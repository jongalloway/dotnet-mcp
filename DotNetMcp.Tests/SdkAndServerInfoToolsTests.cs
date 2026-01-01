using DotNetMcp;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotNetMcp.Tests;

/// <summary>
/// Tests for SDK and server info MCP tools
/// </summary>
public class SdkAndServerInfoToolsTests
{
    private readonly DotNetCliTools _tools;
    private readonly ConcurrencyManager _concurrencyManager;

    public SdkAndServerInfoToolsTests()
    {
        _concurrencyManager = new ConcurrencyManager();
        _tools = new DotNetCliTools(NullLogger<DotNetCliTools>.Instance, _concurrencyManager);
    }

    // SDK Tools

    [Fact]
    public async Task DotnetSdkInfo_ReturnsSDKInformation()
    {
        // Act
        var result = await _tools.DotnetSdkInfo();

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
    }

    [Fact]
    public async Task DotnetSdkInfo_WithMachineReadable_ReturnsStructuredOutput()
    {
        // Act
        var result = await _tools.DotnetSdkInfo(machineReadable: true);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
    }

    [Fact]
    public async Task DotnetSdkVersion_ReturnsSDKVersion()
    {
        // Act
        var result = await _tools.DotnetSdkVersion();

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
    }

    [Fact]
    public async Task DotnetSdkVersion_WithMachineReadable_ReturnsStructuredOutput()
    {
        // Act
        var result = await _tools.DotnetSdkVersion(machineReadable: true);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
    }

    [Fact]
    public async Task DotnetSdkList_ReturnsInstalledSDKs()
    {
        // Act
        var result = await _tools.DotnetSdkList();

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
    }

    [Fact]
    public async Task DotnetSdkList_WithMachineReadable_ReturnsStructuredOutput()
    {
        // Act
        var result = await _tools.DotnetSdkList(machineReadable: true);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
    }

    [Fact]
    public async Task DotnetRuntimeList_ReturnsInstalledRuntimes()
    {
        // Act
        var result = await _tools.DotnetRuntimeList();

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
    }

    [Fact]
    public async Task DotnetRuntimeList_WithMachineReadable_ReturnsStructuredOutput()
    {
        // Act
        var result = await _tools.DotnetRuntimeList(machineReadable: true);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
    }

    // Server Info Tools

    [Fact]
    public async Task DotnetServerCapabilities_ReturnsCapabilitiesJson()
    {
        // Act
        var result = await _tools.DotnetServerCapabilities();

        // Assert
        Assert.NotNull(result);
        // Should be JSON
        Assert.Contains("{", result);
        Assert.Contains("}", result);
        Assert.Contains("serverVersion", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetServerCapabilities_ContainsSupportedCategories()
    {
        // Act
        var result = await _tools.DotnetServerCapabilities();

        // Assert
        Assert.Contains("template", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("project", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("package", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetServerInfo_ReturnsServerInformation()
    {
        // Act
        var result = await _tools.DotnetServerInfo();

        // Assert
        Assert.NotNull(result);
        Assert.Contains(".NET MCP Server", result);
        Assert.Contains("FEATURES:", result);
        Assert.Contains("CONCURRENCY SAFETY:", result);
    }

    [Fact]
    public async Task DotnetServerInfo_ContainsToolCategories()
    {
        // Act
        var result = await _tools.DotnetServerInfo();

        // Assert
        Assert.Contains("Template", result);
        Assert.Contains("Project", result);
        Assert.Contains("Package", result);
        Assert.Contains("Solution", result);
        Assert.Contains("SDK", result);
    }

    [Fact]
    public async Task DotnetServerInfo_ContainsConcurrencyGuidance()
    {
        // Act
        var result = await _tools.DotnetServerInfo();

        // Assert
        Assert.Contains("Read-only operations", result);
        Assert.Contains("safe for parallel execution", result);
    }

    [Fact]
    public async Task DotnetServerInfo_ContainsCachingInformation()
    {
        // Act
        var result = await _tools.DotnetServerInfo();

        // Assert
        Assert.Contains("CACHING:", result);
        Assert.Contains("Templates:", result);
        Assert.Contains("5-minute TTL", result);
    }

    [Fact]
    public async Task DotnetServerInfo_ContainsResourceInformation()
    {
        // Act
        var result = await _tools.DotnetServerInfo();

        // Assert
        Assert.Contains("RESOURCES", result);
        Assert.Contains("dotnet://", result);
    }

    [Fact]
    public async Task DotnetServerInfo_ContainsDocumentationLinks()
    {
        // Act
        var result = await _tools.DotnetServerInfo();

        // Assert
        Assert.Contains("DOCUMENTATION:", result);
        Assert.Contains("github.com/jongalloway/dotnet-mcp", result);
    }
}
