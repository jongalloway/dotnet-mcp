using System.Runtime.InteropServices;
using System.Text.Json;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Xunit;

namespace DotNetMcp.Tests;

/// <summary>
/// Verifies the wire format of _meta.ui on the dotnet_sdk tool,
/// ensuring both modern nested and legacy flat key formats are present
/// for MCP Apps compatibility with VS Code.
/// </summary>
public class McpAppsMetaWireFormatTest : IAsyncLifetime
{
    private McpClient? _client;

    public async ValueTask InitializeAsync()
    {
        var serverPath = GetServerExecutablePath();
        var transportOptions = new StdioClientTransportOptions
        {
            Command = serverPath.command,
            Arguments = serverPath.arguments,
            Name = "dotnet-mcp-test",
        };

        _client = await McpClient.CreateAsync(
            new StdioClientTransport(transportOptions));
    }

    public async ValueTask DisposeAsync()
    {
        if (_client != null)
            await _client.DisposeAsync();
    }

    [Fact]
    public async Task DotnetSdk_Meta_Ui_HasModernNestedFormat()
    {
        Assert.NotNull(_client);

        var meta = await GetSdkToolMeta();

        Assert.True(meta.TryGetProperty("ui", out var ui), "_meta should contain 'ui' key");
        Assert.Equal(JsonValueKind.Object, ui.ValueKind);
        Assert.True(ui.TryGetProperty("resourceUri", out var resourceUri),
            "_meta.ui should contain 'resourceUri'");
        Assert.Equal("ui://dotnet-mcp/sdk-dashboard", resourceUri.GetString());
    }

    [Fact]
    public async Task DotnetSdk_Meta_Ui_HasLegacyFlatKey()
    {
        Assert.NotNull(_client);

        var meta = await GetSdkToolMeta();

        // Legacy flat key required for VS Code compatibility
        Assert.True(meta.TryGetProperty("ui/resourceUri", out var legacyUri),
            "_meta should contain legacy 'ui/resourceUri' flat key (required for VS Code)");
        Assert.Equal("ui://dotnet-mcp/sdk-dashboard", legacyUri.GetString());
    }

    private async Task<JsonElement> GetSdkToolMeta()
    {
        var tools = await _client!.ListToolsAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        var sdkTool = tools.FirstOrDefault(t => t.Name == "dotnet_sdk");
        Assert.NotNull(sdkTool);

        var json = JsonSerializer.Serialize(sdkTool.ProtocolTool);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("_meta", out var meta),
            "dotnet_sdk tool should have _meta field");
        return meta.Clone();
    }

    /// <summary>
    /// Finds the pre-built DotNetMcp server binary, matching the same
    /// configuration/framework as the test project to work in CI.
    /// </summary>
    private static (string command, string[] arguments) GetServerExecutablePath()
    {
        var testBinaryDir = AppContext.BaseDirectory;
        var configuration = Path.GetFileName(Path.GetDirectoryName(testBinaryDir.TrimEnd(Path.DirectorySeparatorChar))!);
        var targetFramework = Path.GetFileName(testBinaryDir.TrimEnd(Path.DirectorySeparatorChar));

        var serverBinaryDir = Path.GetFullPath(
            Path.Join(testBinaryDir, "..", "..", "..", "..", "DotNetMcp", "bin", configuration, targetFramework));

        if (!Directory.Exists(serverBinaryDir))
        {
            throw new DirectoryNotFoundException(
                $"Server binary directory not found at: {serverBinaryDir}. " +
                $"Make sure DotNetMcp project is built before running tests.");
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var exePath = Path.Join(serverBinaryDir, "DotNetMcp.exe");
            if (!File.Exists(exePath))
                throw new FileNotFoundException($"Server executable not found at: {exePath}");
            return (exePath, Array.Empty<string>());
        }
        else
        {
            var dllPath = Path.Join(serverBinaryDir, "DotNetMcp.dll");
            if (!File.Exists(dllPath))
                throw new FileNotFoundException($"Server assembly not found at: {dllPath}");
            return ("dotnet", new[] { dllPath });
        }
    }
}