using DotNetMcp;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.RegularExpressions;
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
        var result = await _tools.DotnetSdk(action: DotNetMcp.Actions.DotnetSdkAction.Info);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
        // SDK info should contain version or runtime information
        Assert.True(result.Contains("SDK", StringComparison.OrdinalIgnoreCase) ||
                    result.Contains("Runtime", StringComparison.OrdinalIgnoreCase) ||
                    result.Contains("Version", StringComparison.OrdinalIgnoreCase),
                    "Result should contain SDK information");
    }

    [Fact]
    public async Task DotnetSdkInfo_WithMachineReadable_ReturnsStructuredOutput()
    {
        // Act
        var result = await _tools.DotnetSdk(action: DotNetMcp.Actions.DotnetSdkAction.Info, machineReadable: true);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
    }

    [Fact]
    public async Task DotnetSdkVersion_ReturnsSDKVersion()
    {
        // Act
        var result = await _tools.DotnetSdk(action: DotNetMcp.Actions.DotnetSdkAction.Version);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
        // Version should contain numeric version information
        Assert.Matches(@"\d+\.\d+", result); // Should match version pattern like "8.0" or "10.0.100"
    }

    [Fact]
    public async Task DotnetSdkVersion_WithMachineReadable_ReturnsStructuredOutput()
    {
        // Act
        var result = await _tools.DotnetSdk(action: DotNetMcp.Actions.DotnetSdkAction.Version, machineReadable: true);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
    }

    [Fact]
    public async Task DotnetSdkList_ReturnsInstalledSDKs()
    {
        // Act
        var result = await _tools.DotnetSdk(action: DotNetMcp.Actions.DotnetSdkAction.ListSdks);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
        // Should list SDK versions
        Assert.True(result.Contains("SDK", StringComparison.OrdinalIgnoreCase) ||
                    result.Contains("Version", StringComparison.OrdinalIgnoreCase) ||
                    Regex.IsMatch(result, @"\d+\.\d+"),
                    "Result should contain SDK version information");
    }

    [Fact]
    public async Task DotnetSdkList_WithMachineReadable_ReturnsStructuredOutput()
    {
        // Act
        var result = await _tools.DotnetSdk(action: DotNetMcp.Actions.DotnetSdkAction.ListSdks, machineReadable: true);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
    }

    [Fact]
    public async Task DotnetRuntimeList_ReturnsInstalledRuntimes()
    {
        // Act
        var result = await _tools.DotnetSdk(action: DotNetMcp.Actions.DotnetSdkAction.ListRuntimes);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
        // Should list runtime information
        Assert.True(result.Contains("Runtime", StringComparison.OrdinalIgnoreCase) ||
                    result.Contains("Microsoft.NETCore.App", StringComparison.OrdinalIgnoreCase) ||
                    Regex.IsMatch(result, @"\d+\.\d+"),
                    "Result should contain runtime information");
    }

    [Fact]
    public async Task DotnetRuntimeList_WithMachineReadable_ReturnsStructuredOutput()
    {
        // Act
        var result = await _tools.DotnetSdk(action: DotNetMcp.Actions.DotnetSdkAction.ListRuntimes, machineReadable: true);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
    }

    [Fact]
    public async Task DotnetSdk_TemplatePackInstall_WithoutTemplatePackage_ReturnsError()
    {
        // Act
        var result = await _tools.DotnetSdk(
            action: DotNetMcp.Actions.DotnetSdkAction.InstallTemplatePack,
            templatePackage: null);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Error:", result);
        Assert.Contains("templatePackage", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetSdk_TemplatePackInstall_WithMachineReadable_BuildsCorrectCommand()
    {
        // Use a local empty directory to avoid NuGet network calls in unit tests.
        var tempDir = Path.Join(Path.GetTempPath(), "dotnet-mcp-template-pack-test", Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(tempDir);
        try
        {
            // Act
            var result = await _tools.DotnetSdk(
                action: DotNetMcp.Actions.DotnetSdkAction.InstallTemplatePack,
                templatePackage: tempDir,
                machineReadable: true);

            // Assert
            Assert.NotNull(result);
            MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, $"dotnet new install \"{tempDir}\"");
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { /* best-effort cleanup */ }
        }
    }

    [Fact]
    public async Task DotnetSdk_TemplatePackUninstall_WithMachineReadable_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetSdk(
            action: DotNetMcp.Actions.DotnetSdkAction.UninstallTemplatePack,
            templatePackage: "Some.Template.Pack",
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet new uninstall \"Some.Template.Pack\"");
    }

    [Fact]
    public async Task DotnetSdk_ListTemplatePacks_WithMachineReadable_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetSdk(
            action: DotNetMcp.Actions.DotnetSdkAction.ListTemplatePacks,
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet new uninstall");
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

        // Assert - Verify consolidated tools are mentioned
        Assert.Contains("dotnet_project", result);
        Assert.Contains("dotnet_package", result);
        Assert.Contains("dotnet_solution", result);
        Assert.Contains("dotnet_sdk", result);
        Assert.Contains("dotnet_ef", result);
        Assert.Contains("dotnet_workload", result);
        Assert.Contains("dotnet_tool", result);
        Assert.Contains("dotnet_dev_certs", result);
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
