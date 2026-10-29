using System.IO.Pipelines;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Diagnostics;
using System.Xml.Linq;
using AgentGovernance.Extensions.ModelContextProtocol;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Extensions.Tasks;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace DotNetMcp.Tests;

public class Sdk20CompatibilityTests
{
    [Theory]
    [InlineData("2026-07-28")]
    [InlineData("2025-11-25")]
    public async Task DiscoveryAndInlineCalls_WorkOnBothRevisions(string protocol)
    {
        await using var host = await TestServer.CreateAsync(protocol);
        Assert.Equal(protocol, host.Client.NegotiatedProtocolVersion);
        var tools = await host.Client.ListToolsAsync(cancellationToken: host.Token);
        Assert.Equal(13, tools.Count);
        foreach (var tool in tools)
        {
            Assert.Equal(JsonValueKind.Object, tool.ProtocolTool.InputSchema.ValueKind);
            if (tool.Name is "dotnet_project" or "dotnet_solution" or "dotnet_package" or "dotnet_server_capabilities")
                Assert.False(tool.ProtocolTool.InputSchema.GetProperty("properties").TryGetProperty("context", out _));
            Assert.False(tool.ProtocolTool.InputSchema.GetProperty("properties").TryGetProperty("cancellationToken", out _));
        }

        var result = await host.Client.CallToolAsync("dotnet_server_info", cancellationToken: host.Token);
        Assert.Contains("stdio", result.GetText());
        Assert.NotEmpty(await host.Client.ListResourcesAsync(cancellationToken: host.Token));
        Assert.NotEmpty(await host.Client.ListPromptsAsync(cancellationToken: host.Token));

        var error = await Assert.ThrowsAsync<McpProtocolException>(
            async () => await host.Client.ReadResourceAsync("dotnet://missing", cancellationToken: host.Token));
#pragma warning disable MCP9005 // Assert the SDK's legacy resource error compatibility.
        Assert.Equal(protocol == "2026-07-28" ? McpErrorCode.InvalidParams : McpErrorCode.ResourceNotFound, error.ErrorCode);
#pragma warning restore MCP9005
    }

    [Fact]
    public void ToolDeserialization_RequiresInputSchema()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Tool>("""{"name":"missing-schema"}"""));
        Assert.NotNull(JsonSerializer.Deserialize<Tool>("""{"name":"valid","inputSchema":{"type":"object"}}"""));
    }

    [Theory]
    [InlineData("2026-07-28")]
    [InlineData("2025-11-25")]
    public async Task CapabilitySnapshot_ReflectsTheConnection(string protocol)
    {
        await using var host = await TestServer.CreateAsync(protocol);
        var result = await host.Client.CallToolAsync("dotnet_server_capabilities", cancellationToken: host.Token);
        var capabilities = Assert.IsType<JsonElement>(result.StructuredContent);
        Assert.Equal(protocol, capabilities.GetProperty("protocolVersion").GetString());
        var supports = capabilities.GetProperty("supports");
        Assert.False(supports.GetProperty("asyncTasks").GetBoolean());
        Assert.False(supports.GetProperty("elicitation").GetBoolean());
        Assert.False(supports.GetProperty("sampling").GetBoolean());
        if (protocol == "2025-11-25")
        {
            await host.Client.PingAsync(cancellationToken: host.Token);
            await using var subscription = await host.Client.SubscribeToResourceAsync(
                "dotnet://templates", (_, _) => ValueTask.CompletedTask, cancellationToken: host.Token);
        }
    }

    [Fact]
#pragma warning disable MCP9005 // Exercise the retained per-request logging feature.
    public async Task CurrentLogging_IsOptedIntoPerRequest()
    {
        await using var host = await TestServer.CreateAsync("2026-07-28");
        var enabled = await host.Client.SendRequestAsync(new()
        {
            Id = new RequestId("logging-opt-in"),
            Method = RequestMethods.ToolsCall,
            Params = new JsonObject { ["name"] = "dotnet_server_capabilities" },
            Context = new() { LogLevel = LoggingLevel.Info }
        }, host.Token);
        Assert.True(enabled.Result!["structuredContent"]!["supports"]!["mcpLogging"]!.GetValue<bool>());
        var disabled = await host.Client.CallToolAsync("dotnet_server_capabilities", cancellationToken: host.Token);
        Assert.False(Assert.IsType<JsonElement>(disabled.StructuredContent).GetProperty("supports").GetProperty("mcpLogging").GetBoolean());
    }
#pragma warning restore MCP9005

    [Theory]
    [InlineData("2026-07-28", "Clean", "accept", true)]
    [InlineData("2026-07-28", "Clean", "accept", false)]
    [InlineData("2026-07-28", "Clean", "decline", true)]
    [InlineData("2026-07-28", "Clean", "cancel", true)]
    [InlineData("2025-11-25", "Clean", "accept", true)]
    [InlineData("2025-11-25", "Clean", "accept", false)]
    [InlineData("2025-11-25", "Clean", "decline", true)]
    [InlineData("2025-11-25", "Clean", "cancel", true)]
    [InlineData("2026-07-28", "Remove", "accept", true)]
    [InlineData("2026-07-28", "Remove", "accept", false)]
    [InlineData("2026-07-28", "Remove", "decline", true)]
    [InlineData("2026-07-28", "Remove", "cancel", true)]
    [InlineData("2025-11-25", "Remove", "accept", true)]
    [InlineData("2025-11-25", "Remove", "accept", false)]
    [InlineData("2025-11-25", "Remove", "decline", true)]
    [InlineData("2025-11-25", "Remove", "cancel", true)]
    public async Task Confirmation_RespectsActionAndBoolean(string protocol, string action, string answer, bool confirmed)
    {
        var confirmations = 0;
        await using var host = await TestServer.CreateAsync(protocol, (_, _) =>
        {
            Interlocked.Increment(ref confirmations);
            return ValueTask.FromResult(Confirmation(answer, confirmed));
        });
        var request = DestructiveRequest(action, host.DirectoryPath);
        var result = await host.Client.CallToolAsync(request, host.Token);
        Assert.Equal(1, confirmations);
        if (answer == "accept" && confirmed)
            Assert.DoesNotContain("operation cancelled", result.GetText());
        else
            Assert.Contains("operation cancelled", result.GetText());
    }

    [Theory]
    [InlineData("Clean")]
    [InlineData("Remove")]
    public async Task Confirmation_RejectsUnboundContinuation(string action)
    {
        await using var host = await TestServer.CreateAsync("2026-07-28", (_, _) =>
            ValueTask.FromResult(Confirmation("accept", true)));
        var request = DestructiveRequest(action, host.DirectoryPath);
        request.RequestState = "not-bound-to-this-operation";
        request.InputResponses = new Dictionary<string, InputResponse>
        {
            ["confirmation"] = InputResponse.FromElicitResult(Confirmation("accept", true))
        };
        var error = await Assert.ThrowsAsync<McpProtocolException>(
            async () => await host.Client.CallToolAsync(request, host.Token));
        Assert.Equal(McpErrorCode.InvalidParams, error.ErrorCode);
    }

    [Theory]
    [InlineData("2026-07-28")]
    [InlineData("2025-11-25")]
    public async Task ConfirmedClean_ExecutesOnlyOnceAfterInput(string protocol)
    {
        string? marker = null;
        var confirmations = 0;
        await using var host = await TestServer.CreateAsync(protocol, (_, _) =>
        {
            Assert.False(File.Exists(marker));
            Interlocked.Increment(ref confirmations);
            return ValueTask.FromResult(Confirmation("accept", true));
        });
        marker = Path.Join(host.DirectoryPath, "clean-count.txt");
        var project = Path.Join(host.DirectoryPath, "clean.csproj");
        await File.WriteAllTextAsync(project, """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
              <Target Name="CountClean" AfterTargets="Clean">
                <WriteLinesToFile File="clean-count.txt" Lines="clean" Overwrite="false" />
              </Target>
            </Project>
            """, host.Token);
        var result = await host.Client.CallToolAsync("dotnet_project",
            new Dictionary<string, object?> { ["action"] = "Clean", ["project"] = project },
            cancellationToken: host.Token);
        Assert.NotEqual(true, result.IsError);
        Assert.Equal(1, confirmations);
        Assert.Single(await File.ReadAllLinesAsync(marker, host.Token));
    }

    [Fact]
    public async Task TaskCapability_IsEnabledForAnOptedInRequest()
    {
        await using var host = await TestServer.CreateAsync("2026-07-28");
        var task = await host.Client.CallToolAsTaskAsync(new() { Name = "dotnet_server_capabilities" }, host.Token);
        Assert.True(task.IsTask);
        var completed = Assert.IsType<CompletedTaskResult>(
            await PollAsync(host, task.TaskCreated!.TaskId, r => r is not WorkingTaskResult));
        var result = completed.Result.Deserialize<CallToolResult>()!;
        Assert.True(Assert.IsType<JsonElement>(result.StructuredContent).GetProperty("supports").GetProperty("asyncTasks").GetBoolean());
    }

    [Fact]
    public async Task TaskProtocolFailure_UsesFailedState()
    {
        await using var host = await TestServer.CreateAsync("2026-07-28");
        var task = await host.Client.CallToolAsTaskAsync(new() { Name = "missing-tool" }, host.Token);
        Assert.True(task.IsTask);
        Assert.IsType<FailedTaskResult>(
            await PollAsync(host, task.TaskCreated!.TaskId, r => r is not WorkingTaskResult));
    }

    [Fact]
    public async Task TaskLifecycle_PollsToCompletionAndRetainsApplicationErrors()
    {
        await using var host = await TestServer.CreateAsync("2026-07-28");
        var task = await host.Client.CallToolAsTaskAsync(new() { Name = "controlled" }, host.Token);
        Assert.True(task.IsTask);
        await host.Entered.Task.WaitAsync(host.Token);
        Assert.IsType<WorkingTaskResult>(await host.Client.GetTaskAsync(task.TaskCreated!.TaskId, host.Token));
        host.Release.TrySetResult();
        var completed = Assert.IsType<CompletedTaskResult>(await PollAsync(host, task.TaskCreated.TaskId, r => r is not WorkingTaskResult));
        var result = completed.Result.Deserialize<CallToolResult>();
        Assert.NotNull(result);
        Assert.True(result.IsError);
        Assert.Equal("controlled application error", result.GetText());
        Assert.Equal(1, host.Invocations);
    }

    [Fact]
    public async Task TaskCancellation_StopsTheRunningHandler()
    {
        await using var host = await TestServer.CreateAsync("2026-07-28");
        var task = await host.Client.CallToolAsTaskAsync(new() { Name = "controlled" }, host.Token);
        Assert.True(task.IsTask);
        await host.Entered.Task.WaitAsync(host.Token);
        await host.Client.CancelTaskAsync(task.TaskCreated!.TaskId, host.Token);
        Assert.IsType<CancelledTaskResult>(await host.Client.GetTaskAsync(task.TaskCreated.TaskId, host.Token));
        await host.Exited.Task.WaitAsync(host.Token);
    }

    [Fact]
    public async Task TaskCancellation_TerminatesTheCliProcessTree()
    {
        await using var host = await TestServer.CreateAsync("2026-07-28");
        var marker = Path.Join(host.DirectoryPath, "child.pid");
        var command = OperatingSystem.IsWindows()
            ? $"powershell.exe -NoProfile -NonInteractive -Command \"[IO.File]::WriteAllText('{marker.Replace("'", "''")}.tmp', [string]$PID); [IO.File]::Move('{marker.Replace("'", "''")}.tmp', '{marker.Replace("'", "''")}'); Start-Sleep -Seconds 60\""
            : $"/bin/sh -c 'echo $$ > \"{marker}.tmp\"; mv \"{marker}.tmp\" \"{marker}\"; exec sleep 60'";
        var project = Path.Join(host.DirectoryPath, "wait.csproj");
        var document = new XDocument(new XElement("Project",
            new XElement("Target", new XAttribute("Name", "Build"),
                new XElement("Exec", new XAttribute("Command", command)))));
        await File.WriteAllTextAsync(project, document.ToString(), host.Token);
        var task = await host.Client.CallToolAsTaskAsync(new()
        {
            Name = "dotnet_project",
            Arguments = new Dictionary<string, JsonElement>
            {
                ["action"] = JsonSerializer.SerializeToElement("Build"),
                ["project"] = JsonSerializer.SerializeToElement(project),
                ["noRestore"] = JsonSerializer.SerializeToElement(true)
            }
        }, host.Token);
        Assert.True(task.IsTask);
        var taskId = task.TaskCreated!.TaskId;
        Process? child = null;
        try
        {
            while (!File.Exists(marker))
            {
                Assert.IsType<WorkingTaskResult>(await host.Client.GetTaskAsync(taskId, host.Token));
                await Task.Delay(TimeSpan.FromMilliseconds(20), host.Token);
            }
            var pidText = await File.ReadAllTextAsync(marker, host.Token);
            child = Process.GetProcessById(int.Parse(pidText.Trim(), System.Globalization.CultureInfo.InvariantCulture));
            await host.Client.CancelTaskAsync(taskId, host.Token);
            await child.WaitForExitAsync(host.Token).WaitAsync(TimeSpan.FromSeconds(10), host.Token);
            Assert.True(child.HasExited);
            Assert.IsType<CancelledTaskResult>(await host.Client.GetTaskAsync(taskId, host.Token));
        }
        finally
        {
            if (child is not null)
            {
                if (!child.HasExited)
                    child.Kill(entireProcessTree: true);
                child.Dispose();
            }
            if (await host.Client.GetTaskAsync(taskId, host.Token) is WorkingTaskResult or InputRequiredTaskResult)
                await host.Client.CancelTaskAsync(taskId, host.Token);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("allow")]
    public async Task TaskConfirmation_UsesInputRequiredAndUpdateWithoutReexecuting(string? governance)
    {
        await using var host = await TestServer.CreateAsync("2026-07-28", (_, _) =>
            throw new InvalidOperationException("Manual task polling must not automatically resolve input."), governance);
        var task = await host.Client.CallToolAsTaskAsync(DestructiveRequest("Clean", host.DirectoryPath), host.Token);
        Assert.True(task.IsTask);
        var input = Assert.IsType<InputRequiredTaskResult>(
            await PollAsync(host, task.TaskCreated!.TaskId, r => r is not WorkingTaskResult));
        Assert.NotNull(input.InputRequests);
        var request = Assert.Single(input.InputRequests);
        Assert.Equal("elicitation/create", request.Value.Method);
        await host.Client.UpdateTaskAsync(new()
        {
            TaskId = task.TaskCreated.TaskId,
            InputResponses = new Dictionary<string, InputResponse>
            {
                [request.Key] = InputResponse.FromElicitResult(Confirmation("decline", false))
            }
        }, host.Token);
        var completed = Assert.IsType<CompletedTaskResult>(
            await PollAsync(host, task.TaskCreated.TaskId, r => r is CompletedTaskResult or FailedTaskResult or CancelledTaskResult));
        Assert.Contains("Clean operation cancelled.", completed.Result.Deserialize<CallToolResult>()!.GetText());
    }

    [Fact]
    public async Task TaskConfirmation_CanBeCancelledWhileAwaitingInput()
    {
        await using var host = await TestServer.CreateAsync("2026-07-28", (_, _) =>
            throw new InvalidOperationException("Manual task polling must not automatically resolve input."));
        var task = await host.Client.CallToolAsTaskAsync(DestructiveRequest("Clean", host.DirectoryPath), host.Token);
        Assert.IsType<InputRequiredTaskResult>(
            await PollAsync(host, task.TaskCreated!.TaskId, r => r is not WorkingTaskResult));
        await host.Client.CancelTaskAsync(task.TaskCreated.TaskId, host.Token);
        Assert.IsType<CancelledTaskResult>(await host.Client.GetTaskAsync(task.TaskCreated.TaskId, host.Token));
    }

    [Fact]
    public async Task RequestCancellation_ReachesExecutorAndRestoresPreviousScope()
    {
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();
        var previous = DotNetCommandExecutor.RequestCancellationToken.Value;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await DotNetCommandExecutor.WithRequestCancellationAsync<string>(cancelled.Token,
                async () => await DotNetCommandExecutor.ExecuteCommandAsync("--info", cancellationToken: TestContext.Current.CancellationToken)));
        Assert.Equal(previous, DotNetCommandExecutor.RequestCancellationToken.Value);
    }

    [Fact]
    public async Task Governance_FullCatalogFailsClosedOnExistingPasswordSchema()
    {
        var error = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await TestServer.CreateAsync("2026-07-28", governance: "allow", fullCatalog: true));
        Assert.Contains("dotnet_dev_certs", error.Message);
        Assert.Contains("\"password\"", error.Message);
    }

    [Fact]
    public async Task Governance_EnforcesPoliciesAcrossMrtrConfirmation()
    {
        var confirmations = 0;
        await using var host = await TestServer.CreateAsync("2026-07-28", (_, _) =>
        {
            Interlocked.Increment(ref confirmations);
            return ValueTask.FromResult(Confirmation("decline", false));
        }, "allow");
        var result = await host.Client.CallToolAsync(DestructiveRequest("Clean", host.DirectoryPath), host.Token);
        Assert.Equal(1, confirmations);
        Assert.Contains("Clean operation cancelled.", result.GetText());
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Governance_EnforcesPoliciesForInlineAndTaskCalls(bool deny, bool asTask)
    {
        await using var host = await TestServer.CreateAsync("2026-07-28", governance: deny ? "deny" : "allow");
        host.Release.TrySetResult();
        CallToolResult result;
        if (asTask)
        {
            var task = await host.Client.CallToolAsTaskAsync(new() { Name = "dotnet_server_info" }, host.Token);
            Assert.True(task.IsTask);
            var completed = Assert.IsType<CompletedTaskResult>(
                await PollAsync(host, task.TaskCreated!.TaskId, r => r is not WorkingTaskResult));
            result = completed.Result.Deserialize<CallToolResult>()!;
        }
        else
        {
            result = await host.Client.CallToolAsync("dotnet_server_info", cancellationToken: host.Token);
        }
        Assert.Equal(deny, result.IsError == true);
        Assert.Equal(deny, result.GetText().Contains("blocked by governance policy", StringComparison.Ordinal));
    }

    private static ElicitResult Confirmation(string action, bool confirmed) => new()
    {
        Action = action,
        Content = new Dictionary<string, JsonElement> { ["confirmed"] = JsonSerializer.SerializeToElement(confirmed) }
    };

    private static CallToolRequestParams DestructiveRequest(string action, string directory) => new()
    {
        Name = action == "Clean" ? "dotnet_project" : "dotnet_solution",
        Arguments = new Dictionary<string, JsonElement>
        {
            ["action"] = JsonSerializer.SerializeToElement(action),
            [action == "Clean" ? "project" : "solution"] = JsonSerializer.SerializeToElement(Path.Join(directory, action == "Clean" ? "missing.csproj" : "missing.sln")),
            ["projects"] = JsonSerializer.SerializeToElement(new[] { "missing.csproj" })
        }
    };

    private static async Task<GetTaskResult> PollAsync(TestServer host, string taskId, Func<GetTaskResult, bool> finished)
    {
        while (true)
        {
            var result = await host.Client.GetTaskAsync(taskId, host.Token);
            if (finished(result))
                return result;
            await Task.Delay(TimeSpan.FromMilliseconds(20), host.Token);
        }
    }

    private sealed class TestServer : IAsyncDisposable
    {
        private readonly CancellationTokenSource _timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        private ServiceProvider? _provider;
        private Task? _serverRun;
        public McpClient Client { get; private set; } = null!;
        public string DirectoryPath { get; } = Path.Join(Path.GetTempPath(), $"dotnet-mcp-sdk20-{Guid.NewGuid():N}");
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Exited { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int Invocations;
        public CancellationToken Token => _timeout.Token;

        public static async Task<TestServer> CreateAsync(
            string protocol,
            Func<ElicitRequestParams?, CancellationToken, ValueTask<ElicitResult>>? elicit = null,
            string? governance = null,
            bool fullCatalog = false)
        {
            var host = new TestServer();
            host._timeout.CancelAfter(TimeSpan.FromSeconds(60));
            Directory.CreateDirectory(host.DirectoryPath);
            try
            {
                Pipe requests = new(), responses = new();
                var services = new ServiceCollection();
                services.AddLogging();
                services.AddSingleton<ConcurrencyManager>();
                services.AddSingleton<ProcessSessionManager>();
                services.AddTransient<DotNetCliTools>();
                var store = new InMemoryMcpTaskStore();
                services.AddSingleton<IMcpTaskStore>(store);
                var builder = services.AddMcpServer(options =>
                    options.Filters.Request.CallToolFilters.Add(next => async (context, ct) =>
                    {
                        var previous = DotNetCommandExecutor.WorkingDirectoryOverride.Value;
                        DotNetCommandExecutor.WorkingDirectoryOverride.Value = host.DirectoryPath;
                        try
                        {
                            return await DotNetCommandExecutor.WithRequestCancellationAsync(ct, () => next(context, ct));
                        }
                        finally
                        {
                            DotNetCommandExecutor.WorkingDirectoryOverride.Value = previous;
                        }
                    }))
                    .WithStreamServerTransport(requests.Reader.AsStream(), responses.Writer.AsStream())
                    .WithTasks(store)
                    .WithTools([McpServerTool.Create(async (CancellationToken ct) =>
                    {
                        Assert.Equal(ct, DotNetCommandExecutor.RequestCancellationToken.Value);
                        Interlocked.Increment(ref host.Invocations);
                        host.Entered.TrySetResult();
                        try
                        {
                            await host.Release.Task.WaitAsync(ct);
                            return new CallToolResult { IsError = true, Content = [new TextContentBlock { Text = "controlled application error" }] };
                        }
                        finally { host.Exited.TrySetResult(); }
                    }, new() { Name = "controlled" })])
                    .WithResources<DotNetResources>()
                    .WithResources<McpAppsResources>()
                    .WithSubscribeToResourcesHandler((_, _) => ValueTask.FromResult(new EmptyResult()))
                    .WithUnsubscribeFromResourcesHandler((_, _) => ValueTask.FromResult(new EmptyResult()))
                    .WithPrompts<DotNetPrompts>();
                if (governance is null || fullCatalog)
                {
                    builder.WithTools<DotNetCliTools>();
                }
                else
                {
                    // Keep the scanner enabled. These production tools avoid the catalog's
                    // existing password field, whose fail-closed rejection is tested separately.
                    builder.WithTools(new[] { nameof(DotNetCliTools.DotnetServerInfo), nameof(DotNetCliTools.DotnetProject) }
                        .Select(name => McpServerTool.Create(
                            typeof(DotNetCliTools).GetMethod(name)!,
                            context => context.Services!.GetRequiredService<DotNetCliTools>())));
                }
                if (governance is not null)
                {
                    var policy = Path.Join(host.DirectoryPath, "policy.yaml");
                    await File.WriteAllTextAsync(policy, $"name: sdk20-test\nversion: '1.0'\ndefault_action: {governance}\nrules: []\n", host.Token);
                    builder.WithGovernance(options =>
                    {
                        options.RequireAuthenticatedAgentId = false;
                        options.PolicyPaths.Add(policy);
                    });
                }
                host._provider = services.BuildServiceProvider();
                host._serverRun = host._provider.GetRequiredService<McpServer>().RunAsync(host.Token);
                host.Client = await McpClient.CreateAsync(
                    new PerRequestLoggingTransport(new StreamClientTransport(requests.Writer.AsStream(), responses.Reader.AsStream())),
                    new()
                    {
                        ProtocolVersion = protocol,
                        // Use the fixture timeout rather than misclassifying a busy modern server as legacy.
                        DiscoverProbeTimeout = Timeout.InfiniteTimeSpan,
                        Handlers = new() { ElicitationHandler = elicit }
                    },
                    cancellationToken: host.Token);
                return host;
            }
            catch
            {
                await host.DisposeAsync();
                throw;
            }
        }

        public async ValueTask DisposeAsync()
        {
            Release.TrySetResult();
            if (Client is not null)
                await Client.DisposeAsync();
            await _timeout.CancelAsync();
            if (_serverRun is not null)
            {
                try { await _serverRun; }
                catch (OperationCanceledException) when (_timeout.IsCancellationRequested) { }
            }
            if (_provider is not null)
                await _provider.DisposeAsync();
            _timeout.Dispose();
            Directory.Delete(DirectoryPath, recursive: true);
        }
    }
}
