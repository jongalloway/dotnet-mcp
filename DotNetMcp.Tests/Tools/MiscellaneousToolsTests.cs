using System;
using System.IO;
using DotNetMcp;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotNetMcp.Tests;

/// <summary>
/// Tests for watch, format, NuGet, and other miscellaneous MCP tools
/// </summary>
[Collection("ProcessWideStateTests")]
public class MiscellaneousToolsTests
{
    private readonly DotNetCliTools _tools;
    private readonly ConcurrencyManager _concurrencyManager;

    public MiscellaneousToolsTests()
    {
        _concurrencyManager = new ConcurrencyManager();
        _tools = new DotNetCliTools(NullLogger<DotNetCliTools>.Instance, _concurrencyManager);
    }

    private static async Task<string> ExecuteInTempDirectoryAsync(Func<Task<string>> action)
    {
        var originalDirectory = Environment.CurrentDirectory;
        var tempDirectory = Path.Join(Path.GetTempPath(), "dotnet-mcp-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        try
        {
            Environment.CurrentDirectory = tempDirectory;
            return await action();
        }
        finally
        {
            Environment.CurrentDirectory = originalDirectory;
            if (Directory.Exists(tempDirectory))
                Directory.Delete(tempDirectory, recursive: true);
        }
    }

    // Watch Tools Tests

    [Fact]
    public async Task DotnetWatchRun_WithoutParameters_ReturnsWarning()
    {
        // Act
        var result = await _tools.DotnetProject(
            action: DotNetMcp.Actions.DotnetProjectAction.Watch,
            watchAction: "run");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("long-running command", result);
        Assert.Contains("dotnet watch", result);
    }

    [Fact]
    public async Task DotnetWatchRun_WithProject_ReturnsWarning()
    {
        // Act
        var result = await _tools.DotnetProject(
            action: DotNetMcp.Actions.DotnetProjectAction.Watch,
            watchAction: "run",
            project: "MyProject.csproj");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("long-running command", result);
        Assert.Contains("--project", result);
    }

    [Fact]
    public async Task DotnetWatchRun_WithAppArgs_ReturnsWarning()
    {
        // Act
        var result = await _tools.DotnetProject(
            action: DotNetMcp.Actions.DotnetProjectAction.Watch,
            watchAction: "run",
            appArgs: "--environment Development");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("long-running command", result);
        Assert.Contains("--", result);
    }

    [Fact]
    public async Task DotnetWatchRun_WithNoHotReload_ReturnsWarning()
    {
        // Act
        var result = await _tools.DotnetProject(
            action: DotNetMcp.Actions.DotnetProjectAction.Watch,
            watchAction: "run",
            noHotReload: true);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("long-running command", result);
        Assert.Contains("--no-hot-reload", result);
    }

    [Fact]
    public async Task DotnetWatchTest_WithoutParameters_ReturnsWarning()
    {
        // Act
        var result = await _tools.DotnetProject(
            action: DotNetMcp.Actions.DotnetProjectAction.Watch,
            watchAction: "test");

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
        var result = await _tools.DotnetProject(
            action: DotNetMcp.Actions.DotnetProjectAction.Watch,
            watchAction: "test",
            project: "MyTests.csproj");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("long-running command", result);
        Assert.Contains("--project", result);
    }

    [Fact]
    public async Task DotnetWatchTest_WithFilter_ReturnsWarning()
    {
        // Act
        var result = await _tools.DotnetProject(
            action: DotNetMcp.Actions.DotnetProjectAction.Watch,
            watchAction: "test",
            filter: "FullyQualifiedName~MyTest");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("long-running command", result);
        Assert.Contains("--filter", result);
    }

    [Fact]
    public async Task DotnetWatchBuild_WithoutParameters_ReturnsWarning()
    {
        // Act
        var result = await _tools.DotnetProject(
            action: DotNetMcp.Actions.DotnetProjectAction.Watch,
            watchAction: "build");

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
        var result = await _tools.DotnetProject(
            action: DotNetMcp.Actions.DotnetProjectAction.Watch,
            watchAction: "build",
            project: "MyProject.csproj");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("long-running command", result);
        Assert.Contains("--project", result);
    }

    [Fact]
    public async Task DotnetWatchBuild_WithConfiguration_ReturnsWarning()
    {
        // Act
        var result = await _tools.DotnetProject(
            action: DotNetMcp.Actions.DotnetProjectAction.Watch,
            watchAction: "build",
            configuration: "Release");

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
        var result = await ExecuteInTempDirectoryAsync(() => _tools.DotnetProject(
            action: DotNetMcp.Actions.DotnetProjectAction.Format,
            machineReadable: true));

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet format");
    }

    [Fact]
    public async Task DotnetFormat_WithProject_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetProject(
            action: DotNetMcp.Actions.DotnetProjectAction.Format,
            project: "MyProject.csproj",
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet format \"MyProject.csproj\"");
    }

    [Fact]
    public async Task DotnetFormat_WithVerify_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetProject(
            action: DotNetMcp.Actions.DotnetProjectAction.Format,
            verify: true,
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet format --verify-no-changes");
    }

    [Fact]
    public async Task DotnetFormat_WithIncludeGenerated_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetProject(
            action: DotNetMcp.Actions.DotnetProjectAction.Format,
            includeGenerated: true,
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet format --include-generated");
    }

    [Fact]
    public async Task DotnetFormat_WithDiagnostics_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetProject(
            action: DotNetMcp.Actions.DotnetProjectAction.Format,
            diagnostics: "IDE0005,CA1304",
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet format --diagnostics IDE0005,CA1304");
    }

    [Fact]
    public async Task DotnetFormat_WithSeverity_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetProject(
            action: DotNetMcp.Actions.DotnetProjectAction.Format,
            severity: "warn",
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet format --severity warn");
    }

    [Fact]
    public async Task DotnetFormat_WithAllParameters_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetProject(
            action: DotNetMcp.Actions.DotnetProjectAction.Format,
            project: "MyProject.csproj",
            verify: true,
            includeGenerated: true,
            diagnostics: "IDE0005",
            severity: "error",
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(
            result,
            "dotnet format \"MyProject.csproj\" --verify-no-changes --include-generated --diagnostics IDE0005 --severity error");
    }

    // NuGet Locals Tool Tests

    [Fact]
    public async Task DotnetNugetLocals_WithListAll_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetNugetLocals(
            cacheLocation: "all",
            list: true,
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet nuget locals all --list");
    }

    [InteractiveFact]
    public async Task DotnetNugetLocals_WithClearAll_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetNugetLocals(
            cacheLocation: "all",
            clear: true,
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet nuget locals all --clear");
    }

    [Fact]
    public async Task DotnetNugetLocals_WithListHttpCache_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetNugetLocals(
            cacheLocation: "http-cache",
            list: true,
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet nuget locals http-cache --list");
    }

    [Fact]
    public async Task DotnetNugetLocals_WithListGlobalPackages_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetNugetLocals(
            cacheLocation: "global-packages",
            list: true,
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet nuget locals global-packages --list");
    }

    [Fact]
    public async Task DotnetNugetLocals_WithListTemp_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetNugetLocals(
            cacheLocation: "temp",
            list: true,
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet nuget locals temp --list");
    }

    [Fact]
    public async Task DotnetNugetLocals_WithListPluginsCache_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetNugetLocals(
            cacheLocation: "plugins-cache",
            list: true,
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet nuget locals plugins-cache --list");
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
        var result = await _tools.DotnetSdk(action: DotNetMcp.Actions.DotnetSdkAction.FrameworkInfo);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Modern .NET Frameworks", result);
        Assert.Contains(".NET Core Frameworks", result);
        Assert.Contains("Latest Recommended", result);
        Assert.Contains("Latest LTS", result);
        // Should contain at least one framework version
        Assert.Matches(@"net\d+\.\d+", result);
    }

    [Fact]
    public async Task DotnetFrameworkInfo_WithSpecificFramework_ReturnsFrameworkDetails()
    {
        // Act
        var result = await _tools.DotnetSdk(action: DotNetMcp.Actions.DotnetSdkAction.FrameworkInfo, framework: "net8.0");

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
        var result = await _tools.DotnetSdk(action: DotNetMcp.Actions.DotnetSdkAction.FrameworkInfo, framework: "netcoreapp3.1");

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
        // Help output should mention dotnet and commands
        Assert.Contains("dotnet", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetHelp_WithCommand_ReturnsCommandHelp()
    {
        // Act
        var result = await _tools.DotnetHelp(command: "build");

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
        // Help for build command should mention build
        Assert.Contains("build", result, StringComparison.OrdinalIgnoreCase);
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
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet test --help");
    }
}
