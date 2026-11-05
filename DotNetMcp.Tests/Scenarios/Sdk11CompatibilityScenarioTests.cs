using System.Diagnostics;
using System.Text.Json;
using DotNetMcp;
using ModelContextProtocol.Extensions.Tasks;
using ModelContextProtocol.Protocol;

namespace DotNetMcp.Tests.Scenarios;

public sealed class Sdk11CompatibilityScenarioTests(ITestOutputHelper output)
{
    [ScenarioFact]
    public async Task Sdk11_UsesNativeCliHost_AndSupportsManagedOptOut()
    {
        using var root = await CreateFixtureAsync(nameof(Sdk11_UsesNativeCliHost_AndSupportsManagedOptOut));
        foreach (var enabled in new[] { "true", "false" })
        {
            var result = await ScenarioHelpers.RunDotNetAsync("--version", root.Path, TestContext.Current.CancellationToken,
                new Dictionary<string, string?>
                {
                    ["DOTNET_CLI_ENABLEAOT"] = enabled,
                    ["DOTNET_HOST_TRACE"] = "1",
                    ["DOTNET_HOST_TRACEFILE"] = null
                });
            Assert.Equal(0, result.exitCode);
            Assert.StartsWith("11.", result.stdout);
            Assert.Contains("Using AOT-ed SDK=", result.stderr);
            Assert.Equal(enabled == "false",
                System.Text.RegularExpressions.Regex.IsMatch(result.stderr, @"mgd_app='.*[/\\]sdk[/\\].*[/\\]dotnet.dll'"));
        }
    }

    [ScenarioFact]
    public async Task Sdk11_LaunchSettingsNoticeOnStderr_DoesNotMarkSuccessfulRunAsError()
    {
        using var root = await CreateFixtureAsync(nameof(Sdk11_LaunchSettingsNoticeOnStderr_DoesNotMarkSuccessfulRunAsError));
        var project = await WriteConsoleAsync(root.Path, """Console.WriteLine("APPLICATION_OK");""");
        var properties = Directory.CreateDirectory(Path.Join(root.Path, "Properties"));
        await File.WriteAllTextAsync(Path.Join(properties.FullName, "launchSettings.json"),
            """{"profiles":{"Compatibility":{"commandName":"Project"}}}""", TestContext.Current.CancellationToken);
        var direct = await ScenarioHelpers.RunDotNetAsync($"run --project \"{project}\"", root.Path, TestContext.Current.CancellationToken);
        Assert.Equal(0, direct.exitCode);
        Assert.Contains("Using launch settings from", direct.stderr);
        Assert.DoesNotContain("Using launch settings from", direct.stdout);
        await using var client = await McpScenarioClient.CreateAsync(TestContext.Current.CancellationToken, output);
        var result = await client.CallToolAsync("dotnet_project", new()
        {
            ["action"] = "Run", ["project"] = project, ["workingDirectory"] = root.Path
        }, TestContext.Current.CancellationToken);
        Assert.True(result.IsError != true, result.GetText());
        Assert.Contains("APPLICATION_OK", result.GetText());
        Assert.Contains("Using launch settings from", result.GetText());
    }

    [ScenarioFact]
    public async Task Sdk11_CancelledBuild_TerminatesItsActualWorkerProcess()
    {
        using var root = await CreateFixtureAsync(nameof(Sdk11_CancelledBuild_TerminatesItsActualWorkerProcess));
        var pidFile = Path.Join(root.Path, "worker.pid");
        var sleeper = await WriteConsoleAsync(root.Path, """
            File.WriteAllText(args[0] + ".tmp", Environment.ProcessId.ToString());
            File.Move(args[0] + ".tmp", args[0]);
            await Task.Delay(TimeSpan.FromMinutes(2));
            """);
        var build = await ScenarioHelpers.RunDotNetAsync($"build \"{sleeper}\" -v:q", root.Path, TestContext.Current.CancellationToken);
        Assert.True(build.exitCode == 0, build.stdout + build.stderr);
        var worker = Path.Join(root.Path, "bin", "Debug", "net10.0", "App.dll");
        var hold = Path.Join(root.Path, "Hold.csproj");
        var command = System.Security.SecurityElement.Escape($"dotnet \"{worker}\" \"{pidFile}\"");
        await File.WriteAllTextAsync(hold, $"""
            <Project>
              <Target Name="Build">
                <Exec Command="{command}" />
              </Target>
            </Project>
            """, TestContext.Current.CancellationToken);
        Assert.True(File.Exists(worker));
        await using var client = await McpScenarioClient.CreateAsync(TestContext.Current.CancellationToken, output);
        var task = await client.Client.CallToolAsTaskAsync(new()
        {
            Name = "dotnet_project",
            Arguments = new Dictionary<string, JsonElement>
            {
                ["action"] = JsonSerializer.SerializeToElement("Build"),
                ["project"] = JsonSerializer.SerializeToElement(hold),
                ["workingDirectory"] = JsonSerializer.SerializeToElement(root.Path),
                ["noRestore"] = JsonSerializer.SerializeToElement(true)
            }
        }, TestContext.Current.CancellationToken);
        Assert.True(task.IsTask);
        var taskId = task.TaskCreated!.TaskId;
        Process? ownedWorker = null;
        try
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
            deadline.CancelAfter(TimeSpan.FromSeconds(30));
            while (!File.Exists(pidFile))
            {
                Assert.IsType<WorkingTaskResult>(await client.Client.GetTaskAsync(taskId, deadline.Token));
                await Task.Delay(100, deadline.Token);
            }
            var pid = int.Parse(await File.ReadAllTextAsync(pidFile, deadline.Token));
            ownedWorker = Process.GetProcessById(pid);
            await client.Client.CancelTaskAsync(taskId, TestContext.Current.CancellationToken);
            using var cleanupDeadline = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
            cleanupDeadline.CancelAfter(TimeSpan.FromSeconds(10));
            await ownedWorker.WaitForExitAsync(cleanupDeadline.Token);
            Assert.IsType<CancelledTaskResult>(await client.Client.GetTaskAsync(taskId, cleanupDeadline.Token));
        }
        finally
        {
            if (ownedWorker is not null)
            {
                if (!ownedWorker.HasExited)
                {
                    ownedWorker.Kill(entireProcessTree: true);
                    await ownedWorker.WaitForExitAsync(CancellationToken.None);
                }
                ownedWorker.Dispose();
            }
            if (await client.Client.GetTaskAsync(taskId, TestContext.Current.CancellationToken) is WorkingTaskResult or InputRequiredTaskResult)
                await client.Client.CancelTaskAsync(taskId, TestContext.Current.CancellationToken);
        }
    }

    [ScenarioFact]
    public async Task Sdk11_Restore_ChoosesSourceVersion_NotHigherGlobalCacheVersion()
    {
        using var root = await CreateFixtureAsync(nameof(Sdk11_Restore_ChoosesSourceVersion_NotHigherGlobalCacheVersion));
        var feed = Directory.CreateDirectory(Path.Join(root.Path, "feed")).FullName;
        var seed = Directory.CreateDirectory(Path.Join(root.Path, "seed")).FullName;
        var cache = Directory.CreateDirectory(Path.Join(root.Path, "cache")).FullName;
        var packageProject = Path.Join(root.Path, "Package.csproj");
        await File.WriteAllTextAsync(packageProject, """
            <Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework>
            <PackageId>SdkCompatibility.Fixture</PackageId><EnableDefaultCompileItems>false</EnableDefaultCompileItems>
            </PropertyGroup></Project>
            """, TestContext.Current.CancellationToken);
        foreach (var version in new[] { "1.0.0", "1.5.0", "2.0.0" })
        {
            var destination = version == "1.5.0" ? seed : feed;
            var packed = await ScenarioHelpers.RunDotNetAsync(
                $"pack \"{packageProject}\" -p:Version={version} -o \"{destination}\" -v:q", root.Path, TestContext.Current.CancellationToken);
            Assert.True(packed.exitCode == 0, packed.stdout + packed.stderr);
        }
        var consumerDirectory = Directory.CreateDirectory(Path.Join(root.Path, "consumer")).FullName;
        var consumer = Path.Join(consumerDirectory, "Consumer.csproj");
        async Task WriteConsumer(string version) => await File.WriteAllTextAsync(consumer, $"""
            <Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework>
            <RestorePackagesPath>{System.Security.SecurityElement.Escape(cache)}</RestorePackagesPath>
            </PropertyGroup><ItemGroup><PackageReference Include="SdkCompatibility.Fixture" Version="{version}" /></ItemGroup></Project>
            """, TestContext.Current.CancellationToken);
        await WriteConsumer("1.5.0");
        var seeded = await ScenarioHelpers.RunDotNetAsync(
            $"restore \"{consumer}\" --source \"{seed}\" --packages \"{cache}\" -v:q", root.Path, TestContext.Current.CancellationToken);
        Assert.True(seeded.exitCode == 0, seeded.stdout + seeded.stderr);
        var config = Path.Join(root.Path, "NuGet.Config");
        await File.WriteAllTextAsync(config,
            $"<configuration><packageSources><clear /></packageSources><config><add key=\"globalPackagesFolder\" value=\"{System.Security.SecurityElement.Escape(cache)}\" /></config></configuration>",
            TestContext.Current.CancellationToken);
        await WriteConsumer("1.1.0");
        await using var client = await McpScenarioClient.CreateAsync(TestContext.Current.CancellationToken, output);
        var args = new Dictionary<string, object?>
        {
            ["action"] = "Restore", ["project"] = consumer, ["source"] = feed, ["workingDirectory"] = root.Path,
            ["configFile"] = config
        };
        var restored = await client.CallToolAsync("dotnet_project", args, TestContext.Current.CancellationToken);
        Assert.True(restored.IsError != true, restored.GetText());
        Assert.Contains("NU1603", restored.GetText());
        using (var assets = JsonDocument.Parse(await File.ReadAllTextAsync(
            Path.Join(consumerDirectory, "obj", "project.assets.json"), TestContext.Current.CancellationToken)))
        {
            Assert.True(assets.RootElement.GetProperty("libraries").TryGetProperty("SdkCompatibility.Fixture/2.0.0", out _));
            Assert.False(assets.RootElement.GetProperty("libraries").TryGetProperty("SdkCompatibility.Fixture/1.5.0", out _));
        }
        File.Delete(Path.Join(feed, "SdkCompatibility.Fixture.2.0.0.nupkg"));
        File.Delete(Path.Join(consumerDirectory, "obj", "project.assets.json"));
        var unavailable = await client.CallToolAsync("dotnet_project", args, TestContext.Current.CancellationToken);
        Assert.True(unavailable.IsError, unavailable.GetText());
        Assert.Contains("NU1102", unavailable.GetText());
        await WriteConsumer("1.5.0");
        var exactCached = await client.CallToolAsync("dotnet_project", args, TestContext.Current.CancellationToken);
        Assert.True(exactCached.IsError != true, exactCached.GetText());
    }

    [ScenarioFact]
    public async Task Sdk11_Pack_PreservesNu5052AsAWarning()
    {
        using var root = await CreateFixtureAsync(nameof(Sdk11_Pack_PreservesNu5052AsAWarning));
        var project = await WriteConsoleAsync(root.Path, """Console.WriteLine("fixture");""");
        var projectXml = await File.ReadAllTextAsync(project, TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(project, projectXml.Replace("</Project>",
            "<PropertyGroup><PackageId>Compatibility.Fixt&#xFC;re</PackageId><IsPackable>true</IsPackable></PropertyGroup></Project>", StringComparison.Ordinal),
            TestContext.Current.CancellationToken);
        await using var client = await McpScenarioClient.CreateAsync(TestContext.Current.CancellationToken, output);
        var result = await client.CallToolAsync("dotnet_project", new()
        {
            ["action"] = "Pack", ["project"] = project, ["workingDirectory"] = root.Path
        }, TestContext.Current.CancellationToken);
        Assert.True(result.IsError != true, result.GetText());
        Assert.True(result.GetText().Contains("NU5052", StringComparison.Ordinal), result.GetText());
        ScenarioHelpers.AssertSuccess(result.GetText());
        Assert.Single(Directory.GetFiles(Path.Join(root.Path, "bin", "Release"), "*.nupkg"));
    }

    [ScenarioFact]
    public async Task Sdk11_VsTest_UsesProjectLocalNewtonsoftJson()
    {
        using var root = await CreateFixtureAsync(nameof(Sdk11_VsTest_UsesProjectLocalNewtonsoftJson));
        var project = Path.Join(root.Path, "CompatibilityTests.csproj");
        await File.WriteAllTextAsync(project, """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net10.0</TargetFramework><IsTestProject>true</IsTestProject></PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.0.1" />
                <PackageReference Include="xunit" Version="2.9.3" />
                <PackageReference Include="xunit.runner.visualstudio" Version="3.1.1" />
                <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
              </ItemGroup>
            </Project>
            """, TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(Path.Join(root.Path, "Tests.cs"), """
            using System;
            using Newtonsoft.Json;
            using Xunit;
            public class CompatibilityTests
            {
                [Fact]
                public void JsonDependencyIsProvidedByTheProject()
                {
                    Assert.StartsWith(AppContext.BaseDirectory, typeof(JsonConvert).Assembly.Location);
                    Assert.Equal("{\"Value\":42}", JsonConvert.SerializeObject(new { Value = 42 }));
                }
            }
            """, TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(Path.Join(root.Path, "NuGet.Config"), $"""
            <configuration><packageSources><clear />
            <add key="compatibility-public" value="{System.Security.SecurityElement.Escape(ScenarioHelpers.PublicPackageSource)}" />
            </packageSources><disabledPackageSources><clear /></disabledPackageSources></configuration>
            """, TestContext.Current.CancellationToken);
        await using var client = await McpScenarioClient.CreateAsync(TestContext.Current.CancellationToken, output);
        var result = await client.CallToolAsync("dotnet_project", new()
        {
            ["action"] = "Test", ["project"] = project, ["workingDirectory"] = root.Path, ["testRunner"] = "VSTest"
        }, TestContext.Current.CancellationToken);
        Assert.True(result.IsError != true, result.GetText());
        ScenarioHelpers.AssertSuccess(result.GetText());
        Assert.Matches(@"Passed:\s+1\b", result.GetText());
        Assert.True(File.Exists(Path.Join(root.Path, "bin", "Debug", "net10.0", "Newtonsoft.Json.dll")));
    }

    private static async Task<ScenarioHelpers.TempScenarioDirectory> CreateFixtureAsync(string name)
    {
        var root = ScenarioHelpers.CreateTempDirectory(name);
        try
        {
            var sdk = await ScenarioHelpers.RunDotNetAsync("--version", AppContext.BaseDirectory, TestContext.Current.CancellationToken);
            Assert.Equal(0, sdk.exitCode);
            Assert.StartsWith("11.", sdk.stdout);
            await File.WriteAllTextAsync(Path.Join(root.Path, "global.json"),
                JsonSerializer.Serialize(new { sdk = new { version = sdk.stdout.Trim(), rollForward = "disable", allowPrerelease = true } }),
                TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Join(root.Path, "NuGet.Config"),
                "<configuration><packageSources><clear /></packageSources></configuration>", TestContext.Current.CancellationToken);
            return root;
        }
        catch { root.Dispose(); throw; }
    }

    private static async Task<string> WriteConsoleAsync(string directory, string source)
    {
        var project = Path.Join(directory, "App.csproj");
        await File.WriteAllTextAsync(project, """
            <Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework>
            <OutputType>Exe</OutputType><ImplicitUsings>enable</ImplicitUsings>
            </PropertyGroup></Project>
            """, TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(Path.Join(directory, "Program.cs"), source, TestContext.Current.CancellationToken);
        return project;
    }
}
