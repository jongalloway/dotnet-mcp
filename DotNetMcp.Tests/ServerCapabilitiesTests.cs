using System.Text.Json;
using DotNetMcp;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DotNetMcp.Tests;

public class ServerCapabilitiesTests
{
    private readonly DotNetCliTools _tools;
    private readonly Mock<ILogger<DotNetCliTools>> _loggerMock;
    private readonly ConcurrencyManager _concurrencyManager;

    public ServerCapabilitiesTests()
    {
        _loggerMock = new Mock<ILogger<DotNetCliTools>>();
        _concurrencyManager = new ConcurrencyManager();
        _tools = new DotNetCliTools(_loggerMock.Object, _concurrencyManager);
    }

    [Fact]
    public async Task DotnetServerCapabilities_ReturnsValidJson()
    {
        // Act
        var result = await _tools.DotnetServerCapabilities();

        // Assert
        result.Should().NotBeNullOrEmpty();
        
        // Verify it's valid JSON by parsing it
        var act = () => JsonDocument.Parse(result);
        act.Should().NotThrow();
    }

    [Fact]
    public async Task DotnetServerCapabilities_ContainsRequiredFields()
    {
        // Act
        var result = await _tools.DotnetServerCapabilities();
        var jsonDoc = JsonDocument.Parse(result);
        var root = jsonDoc.RootElement;

        // Assert - Check all required top-level fields exist
        root.TryGetProperty("serverVersion", out _).Should().BeTrue();
        root.TryGetProperty("protocolVersion", out _).Should().BeTrue();
        root.TryGetProperty("supportedCategories", out _).Should().BeTrue();
        root.TryGetProperty("supports", out _).Should().BeTrue();
        root.TryGetProperty("sdkVersions", out _).Should().BeTrue();
    }

    [Fact]
    public async Task DotnetServerCapabilities_ServerVersion_IsNotEmpty()
    {
        // Act
        var result = await _tools.DotnetServerCapabilities();
        var jsonDoc = JsonDocument.Parse(result);
        var serverVersion = jsonDoc.RootElement.GetProperty("serverVersion").GetString();

        // Assert
        serverVersion.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task DotnetServerCapabilities_ProtocolVersion_IsCorrect()
    {
        // Act
        var result = await _tools.DotnetServerCapabilities();
        var jsonDoc = JsonDocument.Parse(result);
        var protocolVersion = jsonDoc.RootElement.GetProperty("protocolVersion").GetString();

        // Assert
        protocolVersion.Should().Be("0.4.0-preview.3");
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
        categories.Should().NotBeEmpty();
        categories.Should().Contain("template");
        categories.Should().Contain("project");
        categories.Should().Contain("package");
        categories.Should().Contain("solution");
        categories.Should().Contain("sdk");
        categories.Should().Contain("security");
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
        structuredErrors.Should().BeTrue();
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
        machineReadable.Should().BeTrue();
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
        cancellation.Should().BeTrue();
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
        telemetry.Should().BeFalse();
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
        installed.Should().NotBeEmpty();
        installed.Should().AllSatisfy(sdk => sdk.Should().NotBeNullOrEmpty());
    }

    [Fact]
    public async Task DotnetServerCapabilities_SdkVersions_Recommended_IsNet90()
    {
        // Act
        var result = await _tools.DotnetServerCapabilities();
        var jsonDoc = JsonDocument.Parse(result);
        var recommended = jsonDoc.RootElement
            .GetProperty("sdkVersions")
            .GetProperty("recommended")
            .GetString();

        // Assert
        recommended.Should().Be("net9.0");
    }

    [Fact]
    public async Task DotnetServerCapabilities_SdkVersions_Lts_IsNet80()
    {
        // Act
        var result = await _tools.DotnetServerCapabilities();
        var jsonDoc = JsonDocument.Parse(result);
        var lts = jsonDoc.RootElement
            .GetProperty("sdkVersions")
            .GetProperty("lts")
            .GetString();

        // Assert
        lts.Should().Be("net8.0");
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
        capabilities.Should().NotBeNull();
        capabilities!.ServerVersion.Should().NotBeNullOrEmpty();
        capabilities.ProtocolVersion.Should().Be("0.4.0-preview.3");
        capabilities.SupportedCategories.Should().NotBeEmpty();
        capabilities.Supports.Should().NotBeNull();
        capabilities.Supports.StructuredErrors.Should().BeTrue();
        capabilities.Supports.MachineReadable.Should().BeTrue();
        capabilities.Supports.Cancellation.Should().BeTrue();
        capabilities.Supports.Telemetry.Should().BeFalse();
        capabilities.SdkVersions.Should().NotBeNull();
        capabilities.SdkVersions.Installed.Should().NotBeEmpty();
        capabilities.SdkVersions.Recommended.Should().Be("net9.0");
        capabilities.SdkVersions.Lts.Should().Be("net8.0");
    }

    [Fact]
    public async Task DotnetServerInfo_ReturnsHumanReadableText()
    {
        // Act
        var result = await _tools.DotnetServerInfo();

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("=== .NET MCP Server Capabilities ===");
        result.Should().Contain("FEATURES:");
        result.Should().Contain("TOOL CATEGORIES:");
        result.Should().Contain("CONCURRENCY SAFETY:");
    }
}
