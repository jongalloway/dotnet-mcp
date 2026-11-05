using System.Diagnostics;
using System.Collections.Concurrent;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Text.Json;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace DotNetMcp.Tests;

public sealed class NativeDistributionTests
{
    [NativeFact]
    public async Task NativeExecutable_HasNativePeAndRuntimeIdentity()
    {
        var executable = TestServerCommand.Get().command;
        using var stream = File.OpenRead(executable);
        using var pe = new PEReader(stream);
        Assert.Null(pe.PEHeaders.CorHeader);
        Assert.Equal(Machine.Amd64, pe.PEHeaders.CoffHeader.Machine);
        using var process = new Process
        {
            StartInfo = new(executable, "--native-info")
            {
                RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false
            }
        };
        Assert.True(process.Start());
        try
        {
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync(TestContext.Current.CancellationToken);
            Assert.Equal(0, process.ExitCode);
            Assert.Empty(await stderr);
            using var identity = JsonDocument.Parse(await stdout);
            Assert.False(identity.RootElement.GetProperty("DynamicCodeSupported").GetBoolean());
            Assert.Equal("X64", identity.RootElement.GetProperty("ProcessArchitecture").GetString());
            Assert.StartsWith(".NET 11.", identity.RootElement.GetProperty("FrameworkDescription").GetString());
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync(CancellationToken.None);
            }
        }
    }

    [NativeFact]
    public async Task NativeDiscovery_MatchesManagedToolsResourcesAndPrompts()
    {
        await using var native = await ConnectAsync(TestServerCommand.Get());
        await using var managed = await ConnectAsync(TestServerCommand.Resolve(
            null, AppContext.BaseDirectory, OperatingSystem.IsWindows()));
        var ct = TestContext.Current.CancellationToken;
        var nativeTools = (await native.ListToolsAsync(cancellationToken: ct)).OrderBy(tool => tool.Name).ToArray();
        var managedTools = (await managed.ListToolsAsync(cancellationToken: ct)).OrderBy(tool => tool.Name).ToArray();
        Assert.Equal(managedTools.Length, nativeTools.Length);
        for (var index = 0; index < nativeTools.Length; index++)
        {
            var expected = JsonSerializer.SerializeToElement(managedTools[index].ProtocolTool, McpJsonUtilities.DefaultOptions);
            var actual = JsonSerializer.SerializeToElement(nativeTools[index].ProtocolTool, McpJsonUtilities.DefaultOptions);
            Assert.True(JsonElement.DeepEquals(expected, actual),
                $"Native tool contract differs for {managedTools[index].Name}.\nManaged: {expected}\nNative: {actual}");
        }
        var nativeResources = await native.ListResourcesAsync(cancellationToken: ct);
        var managedResources = await managed.ListResourcesAsync(cancellationToken: ct);
        Assert.Equal(managedResources.Select(resource => (resource.Uri, resource.Name, resource.Description)).OrderBy(resource => resource.Uri),
            nativeResources.Select(resource => (resource.Uri, resource.Name, resource.Description)).OrderBy(resource => resource.Uri));
        var nativePrompts = await native.ListPromptsAsync(cancellationToken: ct);
        var managedPrompts = await managed.ListPromptsAsync(cancellationToken: ct);
        Assert.Equal(managedPrompts.Select(prompt => (prompt.Name, prompt.Description)).OrderBy(prompt => prompt.Name),
            nativePrompts.Select(prompt => (prompt.Name, prompt.Description)).OrderBy(prompt => prompt.Name));
    }

    [NativeFact]
    public async Task NativeSdkHelper_PreservesAllProjectOperations()
    {
        const string initialProject = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net10.0</TargetFramework><NativeProbe>original</NativeProbe></PropertyGroup>
              <ItemGroup><None Include="probe.txt" /></ItemGroup>
            </Project>
            """;
        var directory = Directory.CreateDirectory(Path.Join(Path.GetTempPath(), $"dotnet-mcp-native-project-{Guid.NewGuid():N}"));
        try
        {
            var project = Path.Join(directory.FullName, "Probe.csproj");
            await using var native = await ConnectAsync(TestServerCommand.Get());
            await using var managed = await ConnectAsync(TestServerCommand.Resolve(
                null, AppContext.BaseDirectory, OperatingSystem.IsWindows()));
            foreach (var action in new[] { "Analyze", "Dependencies", "Validate", "SetProperty", "GetProperty",
                "RemoveProperty", "AddItem", "RemoveItem", "ListItems" })
            {
                var args = new Dictionary<string, object?>
                {
                    ["action"] = action, ["project"] = project, ["projectPath"] = project,
                    ["propertyName"] = "NativeProbe", ["propertyValue"] = "updated",
                    ["itemType"] = "None", ["include"] = "probe.txt"
                };
                await File.WriteAllTextAsync(project, initialProject, TestContext.Current.CancellationToken);
                var expected = await managed.CallToolAsync("dotnet_project", args, cancellationToken: TestContext.Current.CancellationToken);
                await File.WriteAllTextAsync(project, initialProject, TestContext.Current.CancellationToken);
                var actual = await native.CallToolAsync("dotnet_project", args, cancellationToken: TestContext.Current.CancellationToken);
                Assert.True(expected.IsError != true, $"Managed {action}: {JsonSerializer.Serialize(expected, McpJsonUtilities.DefaultOptions)}");
                Assert.True(actual.IsError != true, $"Native {action}: {JsonSerializer.Serialize(actual, McpJsonUtilities.DefaultOptions)}");
                var expectedText = expected.Content.OfType<TextContentBlock>().First().Text;
                var actualText = actual.Content.OfType<TextContentBlock>().First().Text;
                using var expectedJson = JsonDocument.Parse(expectedText);
                using var actualJson = JsonDocument.Parse(actualText);
                Assert.True(JsonElement.DeepEquals(expectedJson.RootElement, actualJson.RootElement),
                    $"{action} changed SDK semantics.\nManaged: {expectedText}\nNative: {actualText}");
                Assert.True(actualJson.RootElement.GetProperty("success").GetBoolean());
            }
        }
        finally { directory.Delete(recursive: true); }
    }

    [NativeFact]
    public async Task NativeSdkHelper_OversizedResponseFailsWithoutWaitingForTimeout()
    {
        var directory = Directory.CreateDirectory(Path.Join(Path.GetTempPath(), $"dotnet-mcp-native-large-response-{Guid.NewGuid():N}"));
        try
        {
            var project = Path.Join(directory.FullName, "Large.proj");
            var payload = new string('x', SdkHelperProtocol.MaximumResponseBytes + 4 * 1024 * 1024);
            await File.WriteAllTextAsync(project, $"""
                <Project>
                  <ItemGroup><NativeProbe Include="probe"><Payload>{payload}</Payload></NativeProbe></ItemGroup>
                </Project>
                """, TestContext.Current.CancellationToken);

            await using var native = await ConnectAsync(TestServerCommand.Get());
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
            deadline.CancelAfter(TimeSpan.FromSeconds(60));
            var result = await native.CallToolAsync("dotnet_project",
                new Dictionary<string, object?> { ["action"] = "ListItems", ["project"] = project, ["itemType"] = "NativeProbe" },
                cancellationToken: deadline.Token);

            Assert.True(result.IsError);
            Assert.Contains($"SDK helper message exceeded its {SdkHelperProtocol.MaximumResponseBytes}-byte limit.",
                JsonSerializer.Serialize(result, McpJsonUtilities.DefaultOptions));
        }
        finally { directory.Delete(recursive: true); }
    }

    [NativeFact]
    public async Task NativeTemplateAndFrameworkResources_PreserveManagedMetadata()
    {
        await using var native = await ConnectAsync(TestServerCommand.Get());
        await using var managed = await ConnectAsync(TestServerCommand.Resolve(
            null, AppContext.BaseDirectory, OperatingSystem.IsWindows()));
        foreach (var uri in new[] { "dotnet://templates", "dotnet://frameworks" })
        {
            var expected = await managed.ReadResourceAsync(uri, cancellationToken: TestContext.Current.CancellationToken);
            var actual = await native.ReadResourceAsync(uri, cancellationToken: TestContext.Current.CancellationToken);
            var expectedText = Assert.IsType<TextResourceContents>(Assert.Single(expected.Contents)).Text;
            var actualText = Assert.IsType<TextResourceContents>(Assert.Single(actual.Contents)).Text;
            using var expectedJson = JsonDocument.Parse(expectedText);
            using var actualJson = JsonDocument.Parse(actualText);
            Assert.False(actualJson.RootElement.TryGetProperty("error", out _), actualText);
            Assert.True(JsonElement.DeepEquals(expectedJson.RootElement, actualJson.RootElement),
                $"{uri} metadata differs.\nManaged: {expectedText}\nNative: {actualText}");
        }
    }

    [NativeFact]
    public async Task NativeSdkHelper_MissingBundleFailsExplicitly()
    {
        var directory = Directory.CreateDirectory(Path.Join(Path.GetTempPath(), $"dotnet-mcp-native-missing-helper-{Guid.NewGuid():N}"));
        try
        {
            var executable = Path.Join(directory.FullName, "DotNetMcp.exe");
            File.Copy(TestServerCommand.Get().command, executable);
            var project = Path.Join(directory.FullName, "Probe.csproj");
            await File.WriteAllTextAsync(project,
                """<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>""",
                TestContext.Current.CancellationToken);
            await using var client = await ConnectAsync((executable, []));
            var result = await client.CallToolAsync("dotnet_project",
                new Dictionary<string, object?> { ["action"] = "Analyze", ["projectPath"] = project },
                cancellationToken: TestContext.Current.CancellationToken);
            Assert.True(result.IsError);
            Assert.Contains("sdk-helper", JsonSerializer.Serialize(result, McpJsonUtilities.DefaultOptions));
        }
        finally { directory.Delete(recursive: true); }
    }

    [NativeFact]
    public async Task NativeGovernance_PreservesFailClosedCatalogValidation()
    {
        var diagnostics = new ConcurrentQueue<string>();
        var transport = new StdioClientTransport(new()
        {
            Command = TestServerCommand.Get().command,
            Name = "native-governance-fail-closed",
            EnvironmentVariables = new Dictionary<string, string?> { ["McpGovernance__Enabled"] = "true" },
            StandardErrorLines = diagnostics.Enqueue
        });
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));
        var error = await Record.ExceptionAsync(async () =>
        {
            await using var client = await McpClient.CreateAsync(transport, cancellationToken: timeout.Token);
        });
        Assert.NotNull(error);
        var stderr = string.Join(Environment.NewLine, diagnostics);
        Assert.Contains("dotnet_dev_certs", stderr);
        Assert.Contains("password", stderr);
    }

    private static Task<McpClient> ConnectAsync((string command, string[] arguments) command) =>
        McpClient.CreateAsync(new StdioClientTransport(new()
        {
            Command = command.command, Arguments = command.arguments, Name = "native-distribution-parity"
        }), cancellationToken: TestContext.Current.CancellationToken);

    private sealed class NativeFactAttribute : FactAttribute
    {
        public NativeFactAttribute(
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0) : base(sourceFilePath, sourceLineNumber)
        {
            if (!OperatingSystem.IsWindows() ||
                Environment.GetEnvironmentVariable(TestServerCommand.ExecutablePathEnvironmentVariable) is null)
                Skip = "Native distribution tests require an explicitly selected Windows native executable.";
        }
    }
}
