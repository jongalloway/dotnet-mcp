using DotNetMcp;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotNetMcp.Tests;

/// <summary>
/// Tests for watch, format, NuGet, and other miscellaneous MCP tools
/// </summary>
public class MiscellaneousToolsTests
{
    private readonly DotNetCliTools _tools;
    private readonly ConcurrencyManager _concurrencyManager;

    public MiscellaneousToolsTests()
    {
        _concurrencyManager = new ConcurrencyManager();
        _tools = new DotNetCliTools(NullLogger<DotNetCliTools>.Instance, _concurrencyManager);
    }

    // Watch Tools Tests

    [Fact]
    public async Task DotnetWatchRun_WithoutParameters_ReturnsWarning()
    {
        // Act
        var result = await _tools.DotnetWatchRun();

        // Assert
        Assert.NotNull(result);
        Assert.Contains("long-running command", result);
        Assert.Contains("dotnet watch", result);
    }

    [Fact]
    public async Task DotnetWatchRun_WithProject_ReturnsWarning()
    {
        // Act
        var result = await _tools.DotnetWatchRun(project: "MyProject.csproj");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("long-running command", result);
        Assert.Contains("--project", result);
    }

    [Fact]
    public async Task DotnetWatchRun_WithAppArgs_ReturnsWarning()
    {
        // Act
        var result = await _tools.DotnetWatchRun(appArgs: "--environment Development");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("long-running command", result);
        Assert.Contains("--", result);
    }

    [Fact]
    public async Task DotnetWatchRun_WithNoHotReload_ReturnsWarning()
    {
        // Act
        var result = await _tools.DotnetWatchRun(noHotReload: true);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("long-running command", result);
        Assert.Contains("--no-hot-reload", result);
    }

    [Fact]
    public async Task DotnetWatchTest_WithoutParameters_ReturnsWarning()
    {
        // Act
        var result = await _tools.DotnetWatchTest();

        // Assert
        Assert.NotNull(result);
        Assert.Contains("long-running command", result);
        Assert.Contains("dotnet watch", result);
        Assert.Contains("test", result);
    }

    [Fact]
    public async Task DotnetWatchTest_WithProject_ReturnsWarning()
    {
        // Act
        var result = await _tools.DotnetWatchTest(project: "MyTests.csproj");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("long-running command", result);
        Assert.Contains("--project", result);
    }

    [Fact]
    public async Task DotnetWatchTest_WithFilter_ReturnsWarning()
    {
        // Act
        var result = await _tools.DotnetWatchTest(filter: "FullyQualifiedName~MyTest");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("long-running command", result);
        Assert.Contains("--filter", result);
    }

    [Fact]
    public async Task DotnetWatchBuild_WithoutParameters_ReturnsWarning()
    {
        // Act
        var result = await _tools.DotnetWatchBuild();

        // Assert
        Assert.NotNull(result);
        Assert.Contains("long-running command", result);
        Assert.Contains("dotnet watch", result);
        Assert.Contains("build", result);
    }

    [Fact]
    public async Task DotnetWatchBuild_WithProject_ReturnsWarning()
    {
        // Act
        var result = await _tools.DotnetWatchBuild(project: "MyProject.csproj");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("long-running command", result);
        Assert.Contains("--project", result);
    }

    [Fact]
    public async Task DotnetWatchBuild_WithConfiguration_ReturnsWarning()
    {
        // Act
        var result = await _tools.DotnetWatchBuild(configuration: "Release");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("long-running command", result);
        Assert.Contains("-c Release", result);
    }

    // Format Tool Tests

    [Fact]
    public async Task DotnetFormat_WithoutParameters_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetFormat();

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
    }

    [Fact]
    public async Task DotnetFormat_WithProject_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetFormat(project: "MyProject.csproj");

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
    }

    [Fact]
    public async Task DotnetFormat_WithVerify_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetFormat(verify: true);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
    }

    [Fact]
    public async Task DotnetFormat_WithIncludeGenerated_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetFormat(includeGenerated: true);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
    }

    [Fact]
    public async Task DotnetFormat_WithDiagnostics_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetFormat(diagnostics: "IDE0005,CA1304");

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
    }

    [Fact]
    public async Task DotnetFormat_WithSeverity_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetFormat(severity: "warn");

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
    }

    [Fact]
    public async Task DotnetFormat_WithAllParameters_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetFormat(
            project: "MyProject.csproj",
            verify: true,
            includeGenerated: true,
            diagnostics: "IDE0005",
            severity: "error",
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
    }

    // NuGet Locals Tool Tests

    [Fact]
    public async Task DotnetNugetLocals_WithListAll_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetNugetLocals(
            cacheLocation: "all",
            list: true);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
    }

    [Fact]
    public async Task DotnetNugetLocals_WithClearAll_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetNugetLocals(
            cacheLocation: "all",
            clear: true);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
    }

    [Fact]
    public async Task DotnetNugetLocals_WithListHttpCache_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetNugetLocals(
            cacheLocation: "http-cache",
            list: true);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
    }

    [Fact]
    public async Task DotnetNugetLocals_WithListGlobalPackages_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetNugetLocals(
            cacheLocation: "global-packages",
            list: true);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
    }

    [Fact]
    public async Task DotnetNugetLocals_WithListTemp_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetNugetLocals(
            cacheLocation: "temp",
            list: true);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
    }

    [Fact]
    public async Task DotnetNugetLocals_WithListPluginsCache_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetNugetLocals(
            cacheLocation: "plugins-cache",
            list: true);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
    }

    [Fact]
    public async Task DotnetNugetLocals_WithInvalidCacheLocation_ReturnsError()
    {
        // Act
        var result = await _tools.DotnetNugetLocals(
            cacheLocation: "invalid",
            list: true);

        // Assert
        Assert.Contains("Error", result);
        Assert.Contains("Invalid cache location", result);
    }

    [Fact]
    public async Task DotnetNugetLocals_WithBothListAndClear_ReturnsError()
    {
        // Act
        var result = await _tools.DotnetNugetLocals(
            cacheLocation: "all",
            list: true,
            clear: true);

        // Assert
        Assert.Contains("Error", result);
        Assert.Contains("Cannot specify both 'list' and 'clear'", result);
    }

    [Fact]
    public async Task DotnetNugetLocals_WithNeitherListNorClear_ReturnsError()
    {
        // Act
        var result = await _tools.DotnetNugetLocals(
            cacheLocation: "all",
            list: false,
            clear: false);

        // Assert
        Assert.Contains("Error", result);
        Assert.Contains("Either 'list' or 'clear' must be true", result);
    }

    // Framework Info Tool Tests

    [Fact]
    public async Task DotnetFrameworkInfo_WithoutParameters_ReturnsFrameworkList()
    {
        // Act
        var result = await _tools.DotnetFrameworkInfo();

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Modern .NET Frameworks", result);
        Assert.Contains(".NET Core Frameworks", result);
        Assert.Contains("Latest Recommended", result);
        Assert.Contains("Latest LTS", result);
    }

    [Fact]
    public async Task DotnetFrameworkInfo_WithSpecificFramework_ReturnsFrameworkDetails()
    {
        // Act
        var result = await _tools.DotnetFrameworkInfo(framework: "net8.0");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Framework: net8.0", result);
        Assert.Contains("Description:", result);
        Assert.Contains("Is LTS:", result);
        Assert.Contains("Is Modern .NET:", result);
    }

    [Fact]
    public async Task DotnetFrameworkInfo_WithLegacyFramework_ReturnsFrameworkDetails()
    {
        // Act
        var result = await _tools.DotnetFrameworkInfo(framework: "netcoreapp3.1");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Framework: netcoreapp3.1", result);
        Assert.Contains("Is .NET Core:", result);
    }

    // Help Tool Tests

    [Fact]
    public async Task DotnetHelp_WithoutCommand_ReturnsGeneralHelp()
    {
        // Act
        var result = await _tools.DotnetHelp();

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
        // Should return general dotnet help
    }

    [Fact]
    public async Task DotnetHelp_WithCommand_ReturnsCommandHelp()
    {
        // Act
        var result = await _tools.DotnetHelp(command: "build");

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
        // Should return help for dotnet build command
    }

    [Fact]
    public async Task DotnetHelp_WithMachineReadable_ReturnsCommandHelp()
    {
        // Act
        var result = await _tools.DotnetHelp(
            command: "test",
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
    }
}
