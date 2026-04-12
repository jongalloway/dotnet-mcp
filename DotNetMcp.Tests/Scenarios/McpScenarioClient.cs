using DotNetMcp;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace DotNetMcp.Tests.Scenarios;

/// <summary>
/// Scenario test client that starts and manages the lifecycle of the dotnet-mcp server process
/// over stdio for end-to-end integration tests.
/// </summary>
/// <remarks>
/// Use <see cref="CreateAsync(CancellationToken, ITestOutputHelper?)"/> to create an instance,
/// which will launch the MCP server process and connect an <see cref="McpClient"/> over stdio.
/// Pass an <see cref="ITestOutputHelper"/> to capture diagnostic output (server command, tool
/// calls, and response snippets) tied to the individual test in CI logs.
/// Call <see cref="DisposeAsync"/> when finished to shut down the underlying client and release
/// associated resources, including the logger factory and server process.
/// </remarks>
internal sealed class McpScenarioClient : IAsyncDisposable
{
    private const int MaxResponseLogLength = 500;

    private readonly ILoggerFactory _loggerFactory;
    private readonly ITestOutputHelper? _output;
    private McpClient? _client;

    private McpScenarioClient(ILoggerFactory loggerFactory, ITestOutputHelper? output)
    {
        _loggerFactory = loggerFactory;
        _output = output;
    }

    /// <summary>
    /// Creates and starts a new MCP scenario client connected to the dotnet-mcp server.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <param name="output">
    /// Optional xUnit output helper. When provided, diagnostic information (server start
    /// command, tool calls, and response snippets) is written to the per-test output channel
    /// so that CI logs for failing tests include actionable context. Arg values are never
    /// logged to avoid inadvertently printing secrets.
    /// </param>
    /// <returns>A connected McpScenarioClient instance.</returns>
    public static async Task<McpScenarioClient> CreateAsync(CancellationToken cancellationToken, ITestOutputHelper? output = null)
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole(options =>
            {
                options.LogToStandardErrorThreshold = LogLevel.Warning;
            });
            builder.SetMinimumLevel(LogLevel.Warning);
        });

        var host = new McpScenarioClient(loggerFactory, output);
        await host.StartAsync(cancellationToken);
        return host;
    }

    private async Task StartAsync(CancellationToken cancellationToken)
    {
        var serverPath = GetServerExecutablePath();

        _output?.WriteLine($"[MCP] Server command: {serverPath.command} {string.Join(" ", serverPath.arguments)}");

        var transportOptions = new StdioClientTransportOptions
        {
            Command = serverPath.command,
            Arguments = serverPath.arguments,
            Name = "dotnet-mcp-scenario-test"
        };

        var transport = new StdioClientTransport(transportOptions);

        _client = await McpClient.CreateAsync(
            transport,
            loggerFactory: _loggerFactory,
            cancellationToken: cancellationToken);

        _output?.WriteLine("[MCP] Server connected.");
    }

    /// <summary>
    /// Calls an MCP tool and returns the text content from the response.
    /// </summary>
    /// <param name="toolName">The name of the tool to call.</param>
    /// <param name="args">The arguments to pass to the tool.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The text content from the tool response.</returns>
    public async Task<string> CallToolTextAsync(string toolName, Dictionary<string, object?> args, CancellationToken cancellationToken)
    {
        if (_client is null)
        {
            throw new InvalidOperationException("MCP client not initialized.");
        }

        LogToolCall(toolName, args);

        var result = await _client.CallToolAsync(toolName, args, cancellationToken: cancellationToken);
        var text = result.Content.OfType<TextContentBlock>().FirstOrDefault()?.Text ?? string.Empty;

        LogToolResponse(toolName, text);

        return text;
    }

    /// <summary>
    /// Calls an MCP tool and returns the full <see cref="CallToolResult"/>, including any structured content.
    /// </summary>
    /// <param name="toolName">The name of the tool to call.</param>
    /// <param name="args">The arguments to pass to the tool.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The full <see cref="CallToolResult"/> from the tool response.</returns>
    public async Task<CallToolResult> CallToolAsync(string toolName, Dictionary<string, object?> args, CancellationToken cancellationToken)
    {
        if (_client is null)
        {
            throw new InvalidOperationException("MCP client not initialized.");
        }

        LogToolCall(toolName, args);

        var result = await _client.CallToolAsync(toolName, args, cancellationToken: cancellationToken);

        var text = result.Content.OfType<TextContentBlock>().FirstOrDefault()?.Text ?? string.Empty;
        LogToolResponse(toolName, text);

        return result;
    }

    /// <summary>
    /// Logs the tool call with its name and argument keys (values are omitted to avoid leaking secrets).
    /// </summary>
    private void LogToolCall(string toolName, Dictionary<string, object?> args)
    {
        if (_output is null) return;
        var keys = string.Join(", ", args.Keys);
        _output.WriteLine($"[MCP] → {toolName} (args: {{{keys}}})");
    }

    /// <summary>
    /// Logs a truncated snippet of the tool response.
    /// </summary>
    private void LogToolResponse(string toolName, string text)
    {
        if (_output is null) return;
        var snippet = text.Length <= MaxResponseLogLength
            ? text
            : string.Concat(text.AsSpan(0, MaxResponseLogLength), $"... [{text.Length - MaxResponseLogLength} chars truncated]");
        _output.WriteLine($"[MCP] ← {toolName}: {snippet}");
    }

    /// <summary>
    /// Disposes the MCP client and releases all associated resources.
    /// </summary>
    /// <returns>A task representing the asynchronous dispose operation.</returns>
    public async ValueTask DisposeAsync()
    {
        if (_client != null)
        {
            await _client.DisposeAsync();
        }

        _loggerFactory.Dispose();
    }

    private static (string command, string[] arguments) GetServerExecutablePath()
    {
        var testBinaryDir = AppContext.BaseDirectory;
        var trimmedTestBinaryDir = testBinaryDir.TrimEnd(Path.DirectorySeparatorChar);
        var testBinaryParentDir = Path.GetDirectoryName(trimmedTestBinaryDir);
        if (string.IsNullOrEmpty(testBinaryParentDir))
        {
            throw new InvalidOperationException(
                $"Unexpected test binary directory structure. Could not determine configuration from base directory: {testBinaryDir}");
        }

        var configuration = Path.GetFileName(testBinaryParentDir);
        var targetFramework = Path.GetFileName(trimmedTestBinaryDir);

        var serverBinaryDir = Path.GetFullPath(
            Path.Join(testBinaryDir, "..", "..", "..", "..", "DotNetMcp", "bin", configuration, targetFramework));

        if (!Directory.Exists(serverBinaryDir))
        {
            throw new DirectoryNotFoundException(
                $"Server binary directory not found at: {serverBinaryDir}. Make sure DotNetMcp project is built before running scenario tests.");
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var exePath = Path.Join(serverBinaryDir, "DotNetMcp.exe");
            if (!File.Exists(exePath))
            {
                throw new FileNotFoundException($"Server executable not found at: {exePath}");
            }

            return (exePath, Array.Empty<string>());
        }

        var dllPath = Path.Join(serverBinaryDir, "DotNetMcp.dll");
        if (!File.Exists(dllPath))
        {
            throw new FileNotFoundException($"Server assembly not found at: {dllPath}");
        }

        return ("dotnet", new[] { dllPath });
    }
}
