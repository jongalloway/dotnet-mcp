using System;
using DotNetMcp;
using DotNetMcp.Actions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotNetMcp.Tests.Tools;

/// <summary>
/// Tests for the consolidated dotnet_project command.
/// </summary>
public class ConsolidatedProjectToolTests
{
    private readonly DotNetCliTools _tools;
    private readonly ConcurrencyManager _concurrencyManager;

    public ConsolidatedProjectToolTests()
    {
        _concurrencyManager = new ConcurrencyManager();
        _tools = new DotNetCliTools(NullLogger<DotNetCliTools>.Instance, _concurrencyManager);
    }

    #region Action Routing Tests

    [Fact]
    public async Task DotnetProject_New_RoutesToDotnetProjectNew()
    {
        // Test that New action routes correctly
        var result = await _tools.DotnetProject(
            action: DotnetProjectAction.New,
            template: "console",
            name: "MyApp",
            machineReadable: true);

        Assert.NotNull(result);
        // Should contain error about template validation since we're not actually creating a project
        Assert.True(result.Contains("\"success\"") || result.Contains("Error"));
    }

    [Fact]
    public async Task DotnetProject_Restore_RoutesToDotnetProjectRestore()
    {
        // Test that Restore action routes correctly
        var result = await _tools.DotnetProject(
            action: DotnetProjectAction.Restore,
            machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet restore");
    }

    [Fact]
    public async Task DotnetProject_Build_RoutesToDotnetProjectBuild()
    {
        // Test that Build action routes correctly
        var result = await _tools.DotnetProject(
            action: DotnetProjectAction.Build,
            project: "MyProject.csproj",
            configuration: "Release",
            machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet build \"MyProject.csproj\" -c Release");
    }

    [Fact]
    public async Task DotnetProject_Run_RoutesToDotnetProjectRun()
    {
        // Test that Run action routes correctly
        var result = await _tools.DotnetProject(
            action: DotnetProjectAction.Run,
            project: "MyProject.csproj",
            machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet run --project \"MyProject.csproj\"");
    }

    [Fact]
    public async Task DotnetProject_Test_RoutesToDotnetProjectTest()
    {
        // Test that Test action routes correctly
        var result = await _tools.DotnetProject(
            action: DotnetProjectAction.Test,
            project: "MyTests.csproj",
            machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet test \"MyTests.csproj\"");
    }

    [Fact]
    public async Task DotnetProject_Publish_RoutesToDotnetProjectPublish()
    {
        // Test that Publish action routes correctly
        var result = await _tools.DotnetProject(
            action: DotnetProjectAction.Publish,
            project: "MyProject.csproj",
            configuration: "Release",
            machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet publish \"MyProject.csproj\" -c Release");
    }

    [Fact]
    public async Task DotnetProject_Clean_RoutesToDotnetProjectClean()
    {
        // Test that Clean action routes correctly
        var result = await _tools.DotnetProject(
            action: DotnetProjectAction.Clean,
            project: "MyProject.csproj",
            machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet clean \"MyProject.csproj\"");
    }

    [Fact]
    public async Task DotnetProject_Pack_RoutesToDotnetPackCreate()
    {
        // Test that Pack action routes correctly
        var result = await _tools.DotnetProject(
            action: DotnetProjectAction.Pack,
            project: "MyLibrary.csproj",
            configuration: "Release",
            machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet pack \"MyLibrary.csproj\" -c Release");
    }

    [Fact]
    public async Task DotnetProject_Format_RoutesToDotnetFormat()
    {
        // Test that Format action routes correctly
        var result = await _tools.DotnetProject(
            action: DotnetProjectAction.Format,
            project: "MyProject.csproj",
            machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet format \"MyProject.csproj\"");
    }

    #endregion

    #region Required Parameter Validation Tests

    [Fact]
    public async Task DotnetProject_Analyze_WithoutProjectPath_ReturnsError()
    {
        // Test that Analyze action requires projectPath
        var result = await _tools.DotnetProject(
            action: DotnetProjectAction.Analyze,
            projectPath: null,
            machineReadable: false);

        Assert.Contains("Error", result);
        Assert.Contains("projectPath", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetProject_Analyze_WithoutProjectPath_MachineReadable_ReturnsError()
    {
        // Test that Analyze action requires projectPath in machine-readable format
        var result = await _tools.DotnetProject(
            action: DotnetProjectAction.Analyze,
            projectPath: null,
            machineReadable: true);

        Assert.NotNull(result);
        Assert.Contains("\"success\": false", result);
        Assert.Contains("projectPath", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("required", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetProject_Dependencies_WithoutProjectPath_ReturnsError()
    {
        // Test that Dependencies action requires projectPath
        var result = await _tools.DotnetProject(
            action: DotnetProjectAction.Dependencies,
            projectPath: null,
            machineReadable: false);

        Assert.Contains("Error", result);
        Assert.Contains("projectPath", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetProject_Dependencies_WithoutProjectPath_MachineReadable_ReturnsError()
    {
        // Test that Dependencies action requires projectPath in machine-readable format
        var result = await _tools.DotnetProject(
            action: DotnetProjectAction.Dependencies,
            projectPath: null,
            machineReadable: true);

        Assert.NotNull(result);
        Assert.Contains("\"success\": false", result);
        Assert.Contains("projectPath", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("required", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetProject_Validate_WithoutProjectPath_ReturnsError()
    {
        // Test that Validate action requires projectPath
        var result = await _tools.DotnetProject(
            action: DotnetProjectAction.Validate,
            projectPath: null,
            machineReadable: false);

        Assert.Contains("Error", result);
        Assert.Contains("projectPath", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetProject_Validate_WithoutProjectPath_MachineReadable_ReturnsError()
    {
        // Test that Validate action requires projectPath in machine-readable format
        var result = await _tools.DotnetProject(
            action: DotnetProjectAction.Validate,
            projectPath: null,
            machineReadable: true);

        Assert.NotNull(result);
        Assert.Contains("\"success\": false", result);
        Assert.Contains("projectPath", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("required", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetProject_Watch_WithoutWatchAction_ReturnsError()
    {
        // Test that Watch action requires watchAction parameter
        var result = await _tools.DotnetProject(
            action: DotnetProjectAction.Watch,
            watchAction: null,
            machineReadable: false);

        Assert.Contains("Error", result);
        Assert.Contains("watchAction", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("required", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetProject_Watch_WithoutWatchAction_MachineReadable_ReturnsError()
    {
        // Test that Watch action requires watchAction in machine-readable format
        var result = await _tools.DotnetProject(
            action: DotnetProjectAction.Watch,
            watchAction: null,
            machineReadable: true);

        Assert.NotNull(result);
        Assert.Contains("\"success\": false", result);
        Assert.Contains("watchAction", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("required", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetProject_Watch_WithInvalidWatchAction_ReturnsError()
    {
        // Test that Watch action validates watchAction value
        var result = await _tools.DotnetProject(
            action: DotnetProjectAction.Watch,
            watchAction: "invalid",
            machineReadable: false);

        Assert.Contains("Error", result);
        Assert.Contains("watchAction", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("invalid", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetProject_Watch_WithInvalidWatchAction_MachineReadable_ReturnsError()
    {
        // Test that Watch action validates watchAction in machine-readable format
        var result = await _tools.DotnetProject(
            action: DotnetProjectAction.Watch,
            watchAction: "invalid",
            machineReadable: true);

        Assert.NotNull(result);
        Assert.Contains("\"success\": false", result);
        Assert.Contains("watchAction", result, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Watch Action Tests

    [Fact]
    public async Task DotnetProject_Watch_Run_RoutesToDotnetWatchRun()
    {
        // Test that Watch action with run routes correctly
        var result = await _tools.DotnetProject(
            action: DotnetProjectAction.Watch,
            watchAction: "run",
            project: "MyProject.csproj");

        Assert.NotNull(result);
        Assert.Contains("dotnet watch", result);
        Assert.Contains("run", result);
    }

    [Fact]
    public async Task DotnetProject_Watch_Test_RoutesToDotnetWatchTest()
    {
        // Test that Watch action with test routes correctly
        var result = await _tools.DotnetProject(
            action: DotnetProjectAction.Watch,
            watchAction: "test",
            project: "MyTests.csproj");

        Assert.NotNull(result);
        Assert.Contains("dotnet watch", result);
        Assert.Contains("test", result);
    }

    [Fact]
    public async Task DotnetProject_Watch_Build_RoutesToDotnetWatchBuild()
    {
        // Test that Watch action with build routes correctly
        var result = await _tools.DotnetProject(
            action: DotnetProjectAction.Watch,
            watchAction: "build",
            project: "MyProject.csproj");

        Assert.NotNull(result);
        Assert.Contains("dotnet watch", result);
        Assert.Contains("build", result);
    }

    [Fact]
    public async Task DotnetProject_Watch_Run_CaseInsensitive()
    {
        // Test that watchAction is case-insensitive
        var result = await _tools.DotnetProject(
            action: DotnetProjectAction.Watch,
            watchAction: "RUN");

        Assert.NotNull(result);
        Assert.Contains("dotnet watch", result);
    }

    #endregion

    #region Action-Specific Parameter Tests

    [Fact]
    public async Task DotnetProject_New_WithAllParameters_ExecutesCorrectly()
    {
        // Test New action with all parameters
        var result = await _tools.DotnetProject(
            action: DotnetProjectAction.New,
            template: "console",
            name: "MyApp",
            output: "src/MyApp",
            framework: "net8.0",
            machineReadable: true);

        Assert.NotNull(result);
        // Will contain validation error or command execution
        Assert.True(result.Contains("\"success\"") || result.Contains("Error"));
    }

    [Fact]
    public async Task DotnetProject_Build_WithFramework_ExecutesCorrectly()
    {
        // Test Build action with framework parameter
        var result = await _tools.DotnetProject(
            action: DotnetProjectAction.Build,
            project: "MyProject.csproj",
            framework: "net8.0",
            machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet build \"MyProject.csproj\" -f net8.0");
    }

    [Fact]
    public async Task DotnetProject_Test_WithFilter_ExecutesCorrectly()
    {
        // Test Test action with filter parameter
        var result = await _tools.DotnetProject(
            action: DotnetProjectAction.Test,
            project: "MyTests.csproj",
            filter: "FullyQualifiedName~MyNamespace",
            machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet test \"MyTests.csproj\" --filter \"FullyQualifiedName~MyNamespace\"");
    }

    [Fact]
    public async Task DotnetProject_Test_WithMultipleParameters_ExecutesCorrectly()
    {
        // Test Test action with multiple parameters
        var result = await _tools.DotnetProject(
            action: DotnetProjectAction.Test,
            project: "MyTests.csproj",
            configuration: "Release",
            noBuild: true,
            verbosity: "detailed",
            machineReadable: true);

        Assert.NotNull(result);
        var commandResult = result;
        Assert.Contains("dotnet test", commandResult);
        Assert.Contains("MyTests.csproj", commandResult);
        Assert.Contains("Release", commandResult);
        Assert.Contains("--no-build", commandResult);
        Assert.Contains("detailed", commandResult);
    }

    [Fact]
    public async Task DotnetProject_Publish_WithRuntime_ExecutesCorrectly()
    {
        // Test Publish action with runtime parameter
        var result = await _tools.DotnetProject(
            action: DotnetProjectAction.Publish,
            project: "MyProject.csproj",
            runtime: "linux-x64",
            machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet publish \"MyProject.csproj\" -r linux-x64");
    }

    [Fact]
    public async Task DotnetProject_Pack_WithSymbols_ExecutesCorrectly()
    {
        // Test Pack action with includeSymbols parameter
        var result = await _tools.DotnetProject(
            action: DotnetProjectAction.Pack,
            project: "MyLibrary.csproj",
            includeSymbols: true,
            machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet pack \"MyLibrary.csproj\" --include-symbols");
    }

    [Fact]
    public async Task DotnetProject_Format_WithVerify_ExecutesCorrectly()
    {
        // Test Format action with verify parameter
        var result = await _tools.DotnetProject(
            action: DotnetProjectAction.Format,
            project: "MyProject.csproj",
            verify: true,
            machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet format \"MyProject.csproj\" --verify-no-changes");
    }

    #endregion

    #region Invalid Action Tests

    [Fact]
    public async Task DotnetProject_InvalidAction_ReturnsError()
    {
        // Test that an invalid action (outside enum range) is handled
        // This tests the default case in the switch expression
        var invalidAction = (DotnetProjectAction)999;
        var result = await _tools.DotnetProject(
            action: invalidAction,
            machineReadable: false);

        Assert.Contains("Error", result);
        Assert.Contains("not supported", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetProject_InvalidAction_MachineReadable_ReturnsError()
    {
        // Test that an invalid action returns machine-readable error
        var invalidAction = (DotnetProjectAction)999;
        var result = await _tools.DotnetProject(
            action: invalidAction,
            machineReadable: true);

        Assert.NotNull(result);
        Assert.Contains("\"success\": false", result);
        Assert.Contains("not supported", result, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task DotnetProject_Restore_WithProject_ExecutesCorrectly()
    {
        // Integration test for Restore with project parameter
        var result = await _tools.DotnetProject(
            action: DotnetProjectAction.Restore,
            project: "MyProject.csproj",
            machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet restore \"MyProject.csproj\"");
    }

    [Fact]
    public async Task DotnetProject_Clean_WithConfiguration_ExecutesCorrectly()
    {
        // Integration test for Clean with configuration parameter
        var result = await _tools.DotnetProject(
            action: DotnetProjectAction.Clean,
            project: "MyProject.csproj",
            configuration: "Debug",
            machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet clean \"MyProject.csproj\" -c Debug");
    }

    [Fact]
    public async Task DotnetProject_Run_WithAppArgs_ExecutesCorrectly()
    {
        // Integration test for Run with application arguments
        var result = await _tools.DotnetProject(
            action: DotnetProjectAction.Run,
            project: "MyProject.csproj",
            appArgs: "--verbose --log-level debug",
            machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet run --project \"MyProject.csproj\" -- --verbose --log-level debug");
    }

    #endregion
}
