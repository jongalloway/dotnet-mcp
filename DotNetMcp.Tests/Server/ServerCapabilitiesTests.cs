using System.Text.Json;
using DotNetMcp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotNetMcp.Tests;

public class ServerCapabilitiesTests
{
    private readonly DotNetCliTools _tools;
    private readonly ILogger<DotNetCliTools> _logger;
    private readonly ConcurrencyManager _concurrencyManager;

    public ServerCapabilitiesTests()
    {
        _logger = NullLogger<DotNetCliTools>.Instance;
        _concurrencyManager = new ConcurrencyManager();
        _tools = new DotNetCliTools(_logger, _concurrencyManager);
    }

    [Fact]
    public async Task DotnetServerCapabilities_ReturnsValidJson()
    {
        // Act
        var result = await _tools.DotnetServerCapabilities();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        // Verify it's valid JSON by parsing it
        var doc = JsonDocument.Parse(result);
        Assert.NotNull(doc);
    }

    [Fact]
    public async Task DotnetServerCapabilities_ContainsRequiredFields()
    {
        // Act
        var result = await _tools.DotnetServerCapabilities();
        var jsonDoc = JsonDocument.Parse(result);
        var root = jsonDoc.RootElement;

        // Assert - Check all required top-level fields exist
        Assert.True(root.TryGetProperty("serverVersion", out _));
        Assert.True(root.TryGetProperty("protocolVersion", out _));
        Assert.True(root.TryGetProperty("supportedCategories", out _));
        Assert.True(root.TryGetProperty("supports", out _));
        Assert.True(root.TryGetProperty("sdkVersions", out _));
    }

    [Fact]
    public async Task DotnetServerCapabilities_ServerVersion_IsNotEmpty()
    {
        // Act
        var result = await _tools.DotnetServerCapabilities();
        var jsonDoc = JsonDocument.Parse(result);
        var serverVersion = jsonDoc.RootElement.GetProperty("serverVersion").GetString();

        // Assert
        Assert.NotNull(serverVersion);
        Assert.NotEmpty(serverVersion);
    }

    [Fact]
    public async Task DotnetServerCapabilities_ProtocolVersion_IsCorrect()
    {
        // Act
        var result = await _tools.DotnetServerCapabilities();
        var jsonDoc = JsonDocument.Parse(result);
        var protocolVersion = jsonDoc.RootElement.GetProperty("protocolVersion").GetString();

        // Assert
        Assert.Equal("0.5.0-preview.1", protocolVersion);
    }

    [Fact]
    public async Task DotnetServerCapabilities_SupportedCategories_ContainsExpectedCategories()
    {
        // Act
        var result = await _tools.DotnetServerCapabilities();
        var jsonDoc = JsonDocument.Parse(result);
        var categories = jsonDoc.RootElement
            .GetProperty("supportedCategories")
            .EnumerateArray()
            .Select(e => e.GetString())
            .ToArray();

        // Assert
        Assert.NotEmpty(categories);
        Assert.Contains("template", categories);
        Assert.Contains("project", categories);
        Assert.Contains("package", categories);
        Assert.Contains("solution", categories);
        Assert.Contains("sdk", categories);
        Assert.Contains("security", categories);
    }

    [Fact]
    public async Task DotnetServerCapabilities_Supports_StructuredErrors_IsTrue()
    {
        // Act
        var result = await _tools.DotnetServerCapabilities();
        var jsonDoc = JsonDocument.Parse(result);
        var structuredErrors = jsonDoc.RootElement
            .GetProperty("supports")
            .GetProperty("structuredErrors")
            .GetBoolean();

        // Assert
        Assert.True(structuredErrors);
    }

    [Fact]
    public async Task DotnetServerCapabilities_Supports_MachineReadable_IsTrue()
    {
        // Act
        var result = await _tools.DotnetServerCapabilities();
        var jsonDoc = JsonDocument.Parse(result);
        var machineReadable = jsonDoc.RootElement
            .GetProperty("supports")
            .GetProperty("machineReadable")
            .GetBoolean();

        // Assert
        Assert.True(machineReadable);
    }

    [Fact]
    public async Task DotnetServerCapabilities_Supports_Cancellation_IsTrue()
    {
        // Act
        var result = await _tools.DotnetServerCapabilities();
        var jsonDoc = JsonDocument.Parse(result);
        var cancellation = jsonDoc.RootElement
            .GetProperty("supports")
            .GetProperty("cancellation")
            .GetBoolean();

        // Assert
        Assert.True(cancellation);
    }

    [Fact]
    public async Task DotnetServerCapabilities_Supports_Telemetry_IsFalse()
    {
        // Act
        var result = await _tools.DotnetServerCapabilities();
        var jsonDoc = JsonDocument.Parse(result);
        var telemetry = jsonDoc.RootElement
            .GetProperty("supports")
            .GetProperty("telemetry")
            .GetBoolean();

        // Assert - Telemetry is a future feature, should be false initially
        Assert.False(telemetry);
    }

    [Fact]
    public async Task DotnetServerCapabilities_SdkVersions_ContainsInstalledSdks()
    {
        // Act
        var result = await _tools.DotnetServerCapabilities();
        var jsonDoc = JsonDocument.Parse(result);
        var installed = jsonDoc.RootElement
            .GetProperty("sdkVersions")
            .GetProperty("installed")
            .EnumerateArray()
            .Select(e => e.GetString())
            .ToArray();

        // Assert - At least one SDK should be installed (the one we're using to run tests)
        Assert.NotEmpty(installed);
        foreach (var sdk in installed)
        {
            Assert.NotNull(sdk);
            Assert.NotEmpty(sdk);
        }
    }

    [Fact]
    public async Task DotnetServerCapabilities_SdkVersions_Recommended_IsNet100()
    {
        // Act
        var result = await _tools.DotnetServerCapabilities();
        var jsonDoc = JsonDocument.Parse(result);
        var recommended = jsonDoc.RootElement
            .GetProperty("sdkVersions")
            .GetProperty("recommended")
            .GetString();

        // Assert
        Assert.Equal("net10.0", recommended);
    }

    [Fact]
    public async Task DotnetServerCapabilities_SdkVersions_Lts_IsNet100()
    {
        // Act
        var result = await _tools.DotnetServerCapabilities();
        var jsonDoc = JsonDocument.Parse(result);
        var lts = jsonDoc.RootElement
            .GetProperty("sdkVersions")
            .GetProperty("lts")
            .GetString();

        // Assert
        Assert.Equal("net10.0", lts);
    }

    [Fact]
    public async Task DotnetServerCapabilities_JsonSchema_MatchesExpectedStructure()
    {
        // Act
        var result = await _tools.DotnetServerCapabilities();

        // Deserialize to the actual ServerCapabilities object to verify schema
        var capabilities = JsonSerializer.Deserialize<ServerCapabilities>(result, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = false
        });

        // Assert
        Assert.NotNull(capabilities);
        Assert.NotNull(capabilities!.ServerVersion);
        Assert.NotEmpty(capabilities.ServerVersion);
        Assert.Equal("0.5.0-preview.1", capabilities.ProtocolVersion);
        Assert.NotEmpty(capabilities.SupportedCategories);
        Assert.NotNull(capabilities.Supports);
        Assert.True(capabilities.Supports.StructuredErrors);
        Assert.True(capabilities.Supports.MachineReadable);
        Assert.True(capabilities.Supports.Cancellation);
        Assert.False(capabilities.Supports.Telemetry);
        Assert.NotNull(capabilities.SdkVersions);
        Assert.NotEmpty(capabilities.SdkVersions.Installed);
        Assert.Equal("net10.0", capabilities.SdkVersions.Recommended);
        Assert.Equal("net10.0", capabilities.SdkVersions.Lts);
    }

    [Fact]
    public async Task DotnetServerInfo_ReturnsHumanReadableText()
    {
        // Act
        var result = await _tools.DotnetServerInfo();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("=== .NET MCP Server Capabilities ===", result);
        Assert.Contains("FEATURES:", result);
        Assert.Contains("TOOL CATEGORIES:", result);
        Assert.Contains("CONCURRENCY SAFETY:", result);
    }
}
