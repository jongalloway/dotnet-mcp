using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace DotNetMcp.Tests.Scenarios;

internal sealed class McpScenarioClient : IAsyncDisposable
{
    private readonly ILoggerFactory _loggerFactory;
    private McpClient? _client;

    private McpScenarioClient(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

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
        var configuration = Path.GetFileName(Path.GetDirectoryName(testBinaryDir.TrimEnd(Path.DirectorySeparatorChar))!);
        var targetFramework = Path.GetFileName(testBinaryDir.TrimEnd(Path.DirectorySeparatorChar));

        var serverBinaryDir = Path.GetFullPath(
            Path.Combine(testBinaryDir, "..", "..", "..", "..", "DotNetMcp", "bin", configuration, targetFramework));

        if (!Directory.Exists(serverBinaryDir))
        {
            throw new DirectoryNotFoundException(
                $"Server binary directory not found at: {serverBinaryDir}. Make sure DotNetMcp project is built before running scenario tests.");
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var exePath = Path.Combine(serverBinaryDir, "DotNetMcp.exe");
            if (!File.Exists(exePath))
            {
                throw new FileNotFoundException($"Server executable not found at: {exePath}");
            }

            return (exePath, Array.Empty<string>());
        }

        var dllPath = Path.Combine(serverBinaryDir, "DotNetMcp.dll");
        if (!File.Exists(dllPath))
        {
            throw new FileNotFoundException($"Server assembly not found at: {dllPath}");
        }

        return ("dotnet", new[] { dllPath });
    }
}
