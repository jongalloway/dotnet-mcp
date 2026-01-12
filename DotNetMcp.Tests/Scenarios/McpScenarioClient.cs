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
/// Use <see cref="CreateAsync(CancellationToken)"/> to create an instance, which will launch
/// the MCP server process and connect an <see cref="McpClient"/> over stdio. Call
/// <see cref="DisposeAsync"/> when finished to shut down the underlying client and release
/// associated resources, including the logger factory and server process.
/// </remarks>
internal sealed class McpScenarioClient : IAsyncDisposable
{
    private readonly ILoggerFactory _loggerFactory;
    private McpClient? _client;

    private McpScenarioClient(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    /// <summary>
    /// Creates and starts a new MCP scenario client connected to the dotnet-mcp server.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A connected McpScenarioClient instance.</returns>
    public static async Task<McpScenarioClient> CreateAsync(CancellationToken cancellationToken)
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole(options =>
            {
                options.LogToStandardErrorThreshold = LogLevel.Warning;
            });
            builder.SetMinimumLevel(LogLevel.Warning);
        });

        var host = new McpScenarioClient(loggerFactory);
        await host.StartAsync(cancellationToken);
        return host;
    }

    private async Task StartAsync(CancellationToken cancellationToken)
    {
        var serverPath = GetServerExecutablePath();

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

        var result = await _client.CallToolAsync(toolName, args, cancellationToken: cancellationToken);
        var text = result.Content.OfType<TextContentBlock>().FirstOrDefault()?.Text;
        return text ?? string.Empty;
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
