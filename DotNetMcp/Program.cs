using DotNetMcp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.Diagnostics;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

// Register ConcurrencyManager as a singleton
builder.Services.AddSingleton<ConcurrencyManager>();

// Register ProcessSessionManager as a singleton
builder.Services.AddSingleton<ProcessSessionManager>();

// Register InMemoryMcpTaskStore to enable MCP Task support for long-running operations.
// This allows AI clients to run build/test/publish as async tasks with polling and cancellation.
builder.Services.AddSingleton<IMcpTaskStore, InMemoryMcpTaskStore>();

// Register ToolMetricsAccumulator for in-memory telemetry collection.
// The accumulator is also captured by the telemetry filter added below.
var metricsAccumulator = new ToolMetricsAccumulator();
builder.Services.AddSingleton(metricsAccumulator);

builder.Services.AddMcpServer(options =>
{
    // Configure server implementation with .NET-themed icon
    options.ServerInfo = new Implementation
    {
        Name = "dotnet-mcp",
        Version = "1.0.0",
        Title = ".NET MCP Server",
        Description = "MCP server providing AI assistants with direct access to the .NET SDK through both official NuGet packages and CLI execution",
        WebsiteUrl = "https://github.com/jongalloway/dotnet-mcp",
        Icons =
        [
            new Icon
            {
                Source = "https://raw.githubusercontent.com/microsoft/fluentui-emoji/62ecdc0d7ca5c6df32148c169556bc8d3782fca4/assets/Gear/Flat/gear_flat.svg",
                MimeType = "image/svg+xml",
                Sizes = ["any"],
                Theme = "light"
            },
            new Icon
            {
                Source = "https://raw.githubusercontent.com/microsoft/fluentui-emoji/62ecdc0d7ca5c6df32148c169556bc8d3782fca4/assets/Gear/3D/gear_3d.png",
                MimeType = "image/png",
                Sizes = ["256x256"]
            }
        ]
    };
})
    .WithStdioServerTransport()
    .WithTools<DotNetCliTools>()
    .WithResources<DotNetResources>()
    .WithPrompts<DotNetPrompts>()
    .WithCompleteHandler(async (ctx, ct) =>
    {
        var argument = ctx.Params?.Argument;
        if (argument is null)
            return new CompleteResult { Completion = new Completion { Values = [] } };
        var prefix = argument.Value ?? string.Empty;
        var values = (await CompletionProvider.GetCompletionsAsync(argument.Name, prefix, ct)).ToList();
        return new CompleteResult
        {
            Completion = new Completion { Values = values }
        };
    });

// Register the telemetry filter that intercepts every CallTool request to record
// invocation counts, durations, and success/failure rates — without modifying individual tools.
// The filter captures the shared metricsAccumulator instance via closure.
builder.Services.Configure<McpServerOptions>(options =>
{
    options.Filters.Request.CallToolFilters.Add(next => async (context, ct) =>
    {
        var toolName = context.Params?.Name ?? "unknown";
        var sw = Stopwatch.StartNew();
        bool success = false;
        try
        {
            var result = await next(context, ct);
            success = true;
            return result;
        }
        finally
        {
            sw.Stop();
            metricsAccumulator.RecordInvocation(toolName, sw.ElapsedMilliseconds, success);
        }
    });
});

await builder.Build().RunAsync();
