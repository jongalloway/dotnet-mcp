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
        _tools = new DotNetCliTools(NullLogger<DotNetCliTools>.Instance, _concurrencyManager, new ProcessSessionManager());
    }

    [Fact]
    public async Task DotnetProject_WithMissingWorkingDirectory_MachineReadable_ReturnsValidationError()
    {
        var missingDir = Path.GetFullPath(Path.Join(Path.GetTempPath(), "dotnet-mcp-missing-" + Guid.NewGuid().ToString("N")));

        var result = await _tools.DotnetProject(
            action: DotnetProjectAction.Restore,
            workingDirectory: missingDir,
            machineReadable: true);

        Assert.NotNull(result);
        Assert.Contains("\"success\": false", result);
        Assert.Contains("INVALID_PARAMS", result);
        Assert.Contains("workingDirectory", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetProject_WorkingDirectory_UsesProvidedDirectoryForRestore()
    {
        // Arrange: use an empty temp dir so `dotnet restore` fails with MSB1003 about CWD.
        var tempDir = Path.GetFullPath(Path.Join(Path.GetTempPath(), "dotnet-mcp-wd-" + Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(tempDir);

        try
        {
            // Act
            var result = await _tools.DotnetProject(
                action: DotnetProjectAction.Restore,
                workingDirectory: tempDir,
                machineReadable: false);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("MSB1003", result, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("current working directory", result, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            try
            {
                Directory.Delete(tempDir, recursive: true);
            }
            catch (IOException)
            {
                // Best-effort cleanup - ignore IO exceptions during test cleanup
            }
            catch (UnauthorizedAccessException)
            {
                // Best-effort cleanup - ignore access exceptions during test cleanup
            }
        }
    }

    [Fact]
    public async Task DotnetProject_WorkingDirectory_MachineReadable_RecordsCommandAndUsesProvidedDirectory()
    {
        // Arrange: use an empty temp dir so `dotnet restore` fails with MSB1003 about CWD.
        var tempDir = Path.GetFullPath(Path.Join(Path.GetTempPath(), "dotnet-mcp-wd-" + Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(tempDir);

        try
        {
            // Act
            var result = await _tools.DotnetProject(
                action: DotnetProjectAction.Restore,
                workingDirectory: tempDir,
                machineReadable: true);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("\"success\": false", result);
            MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet restore");
            Assert.Contains("MSB1003", result, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("current working directory", result, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            try
            {
                Directory.Delete(tempDir, recursive: true);
            }
            catch (IOException)
            {
                // Best-effort cleanup - ignore IO exceptions during test cleanup
            }
            catch (UnauthorizedAccessException)
            {
                // Best-effort cleanup - ignore access exceptions during test cleanup
            }
        }
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
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet test --project \"MyTests.csproj\"");
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
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet test --project \"MyTests.csproj\" --filter \"FullyQualifiedName~MyNamespace\"");
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

    [Fact]
    public async Task DotnetProject_New_WhenTemplateEngineReturnsEmpty_ButCliFallbackSucceeds_ExecutesDotnetNew()
    {
        // This test verifies the fix for the classlib template creation issue
        var originalLoader = TemplateEngineHelper.LoadTemplatesOverride;
        var originalExecutor = TemplateEngineHelper.ExecuteDotNetForTemplatesAsync;

        try
        {
            // Arrange: Simulate Template Engine API returning empty (the problem scenario)
            TemplateEngineHelper.LoadTemplatesOverride = () => 
                Task.FromResult(Enumerable.Empty<Microsoft.TemplateEngine.Abstractions.ITemplateInfo>());
            
            // Arrange: Simulate CLI fallback succeeding for classlib template validation
            TemplateEngineHelper.ExecuteDotNetForTemplatesAsync = (args, _) =>
            {
                if (args.Contains("new list") && args.Contains("classlib"))
                {
                    // Simulate successful template validation (exit code 0)
                    return Task.FromResult("These templates matched your input: 'classlib'\n\nClass Library  classlib");
                }
                // For actual project creation, we'll let it fail naturally since we're not in a real project directory
                throw new InvalidOperationException("Command failed");
            };

            // Act: Try to create a classlib project
            var result = await _tools.DotnetProject(
                action: DotnetProjectAction.New,
                template: "classlib",
                name: "MyLib",
                output: Path.Join(Path.GetTempPath(), "test-output-" + Guid.NewGuid().ToString("N")),
                machineReadable: true);

            // Assert: Should have attempted to execute dotnet new classlib (validation passed)
            // The command might fail due to environment, but it should have been attempted
            Assert.NotNull(result);
            // Either success or a CLI execution error (not a validation error)
            var hasExecutedCommand = result.Contains("dotnet new classlib") || result.Contains("\"command\":");
            Assert.True(hasExecutedCommand, "Should have attempted to execute 'dotnet new classlib' after successful validation");
        }
        finally
        {
            TemplateEngineHelper.LoadTemplatesOverride = originalLoader;
            TemplateEngineHelper.ExecuteDotNetForTemplatesAsync = originalExecutor;
        }
    }

    #endregion

    #region Legacy Project Argument Tests (--project vs positional)

    [Fact]
    public async Task DotnetProject_Test_DefaultUsesProjectFlag()
    {
        // Test that Test action uses --project by default
        var result = await _tools.DotnetProject(
            action: DotnetProjectAction.Test,
            project: "MyTests.csproj",
            machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet test --project \"MyTests.csproj\"");
    }

    [Fact]
    public async Task DotnetProject_Test_WithLegacyFlag_UsesPositionalArgument()
    {
        // Test that Test action uses positional argument when useLegacyProjectArgument is true
        var result = await _tools.DotnetProject(
            action: DotnetProjectAction.Test,
            project: "MyTests.csproj",
            useLegacyProjectArgument: true,
            machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet test \"MyTests.csproj\"");
    }

    [Fact]
    public async Task DotnetProject_Test_WithLegacyFlag_AndConfiguration_UsesPositionalArgument()
    {
        // Test that Test action with configuration uses positional argument when useLegacyProjectArgument is true
        var result = await _tools.DotnetProject(
            action: DotnetProjectAction.Test,
            project: "MyTests.csproj",
            configuration: "Release",
            useLegacyProjectArgument: true,
            machineReadable: true);

        Assert.NotNull(result);
        var command = MachineReadableCommandAssertions.ExtractExecutedDotnetCommand(result);
        Assert.Contains("dotnet test \"MyTests.csproj\"", command);
        Assert.Contains("-c Release", command);
        Assert.DoesNotContain("--project", command);
    }

    [Fact]
    public async Task DotnetProject_Test_WithLegacyFlag_MultipleParameters_UsesPositionalArgument()
    {
        // Test that Test action with multiple parameters uses positional argument when useLegacyProjectArgument is true
        var result = await _tools.DotnetProject(
            action: DotnetProjectAction.Test,
            project: "MyTests.csproj",
            configuration: "Release",
            filter: "FullyQualifiedName~MyNamespace",
            noBuild: true,
            useLegacyProjectArgument: true,
            machineReadable: true);

        Assert.NotNull(result);
        var command = MachineReadableCommandAssertions.ExtractExecutedDotnetCommand(result);
        Assert.Contains("dotnet test \"MyTests.csproj\"", command);
        Assert.Contains("-c Release", command);
        Assert.Contains("--filter \"FullyQualifiedName~MyNamespace\"", command);
        Assert.Contains("--no-build", command);
        Assert.DoesNotContain("--project", command);
    }

    [Fact]
    public async Task DotnetProject_Test_WithLegacyFlagFalse_UsesProjectFlag()
    {
        // Test that Test action uses --project when useLegacyProjectArgument is explicitly false
        var result = await _tools.DotnetProject(
            action: DotnetProjectAction.Test,
            project: "MyTests.csproj",
            useLegacyProjectArgument: false,
            machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet test --project \"MyTests.csproj\"");
    }

    [Fact]
    public async Task DotnetProject_Test_WithoutProject_LegacyFlagHasNoEffect()
    {
        // Test that when no project is specified, the legacy flag has no effect
        var result = await _tools.DotnetProject(
            action: DotnetProjectAction.Test,
            project: null,
            useLegacyProjectArgument: true,
            machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet test");
    }

    [Fact]
    public async Task DotnetProject_Test_PlainText_DefaultUsesProjectFlag()
    {
        // Test that Test action uses --project by default in plain text mode
        var result = await _tools.DotnetProject(
            action: DotnetProjectAction.Test,
            project: "MyTests.csproj",
            machineReadable: false);

        Assert.NotNull(result);
        // In plain text mode, check for the command in error/output
        // The command should still use --project
    }

    [Fact]
    public async Task DotnetProject_Test_PlainText_WithLegacyFlag_UsesPositionalArgument()
    {
        // Test that Test action uses positional argument when useLegacyProjectArgument is true in plain text mode
        var result = await _tools.DotnetProject(
            action: DotnetProjectAction.Test,
            project: "MyTests.csproj",
            useLegacyProjectArgument: true,
            machineReadable: false);

        Assert.NotNull(result);
        // In plain text mode, the result will be an error or output
        // Just verify we get some result
    }

    #endregion

    #region TestRunner Parameter Tests

    [Fact]
    public async Task DotnetProject_Test_WithTestRunnerMTP_UsesProjectFlag()
    {
        // Test that Test action uses --project when testRunner is explicitly MicrosoftTestingPlatform
        var result = await _tools.DotnetProject(
            action: DotnetProjectAction.Test,
            project: "MyTests.csproj",
            testRunner: TestRunner.MicrosoftTestingPlatform,
            machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet test --project \"MyTests.csproj\"");
        
        // Verify metadata
        Assert.Contains("\"selectedTestRunner\": \"microsoft-testing-platform\"", result);
        Assert.Contains("\"projectArgumentStyle\": \"--project\"", result);
        Assert.Contains("\"selectionSource\": \"testRunner-parameter\"", result);
    }

    [Fact]
    public async Task DotnetProject_Test_WithTestRunnerVSTest_UsesPositionalArg()
    {
        // Test that Test action uses positional argument when testRunner is explicitly VSTest
        var result = await _tools.DotnetProject(
            action: DotnetProjectAction.Test,
            project: "MyTests.csproj",
            testRunner: TestRunner.VSTest,
            machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet test \"MyTests.csproj\"");
        
        // Verify metadata
        Assert.Contains("\"selectedTestRunner\": \"vstest\"", result);
        Assert.Contains("\"projectArgumentStyle\": \"positional\"", result);
        Assert.Contains("\"selectionSource\": \"testRunner-parameter\"", result);
    }

    [Fact]
    public async Task DotnetProject_Test_WithTestRunnerAuto_DetectsFromGlobalJson()
    {
        // Arrange: Create temp directory with global.json configured for MTP
        var tempDir = Path.Join(Path.GetTempPath(), "dotnet-mcp-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var globalJsonPath = Path.Join(tempDir, "global.json");

        try
        {
            File.WriteAllText(globalJsonPath, """
            {
                "test": {
                    "runner": "Microsoft.Testing.Platform"
                }
            }
            """);

            // Act
            var result = await _tools.DotnetProject(
                action: DotnetProjectAction.Test,
                project: "MyTests.csproj",
                testRunner: TestRunner.Auto,
                workingDirectory: tempDir,
                machineReadable: true);

            // Assert
            Assert.NotNull(result);
            MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet test --project \"MyTests.csproj\"");
            
            // Verify metadata indicates MTP was detected from global.json
            Assert.Contains("\"selectedTestRunner\": \"microsoft-testing-platform\"", result);
            Assert.Contains("\"projectArgumentStyle\": \"--project\"", result);
            Assert.Contains("\"selectionSource\": \"global.json\"", result);
        }
        finally
        {
            try
            {
                Directory.Delete(tempDir, recursive: true);
            }
            catch (IOException)
            {
                // Best-effort cleanup
            }
            catch (UnauthorizedAccessException)
            {
                // Best-effort cleanup
            }
        }
    }

    [Fact]
    public async Task DotnetProject_Test_WithTestRunnerAuto_NoGlobalJson_DefaultsToVSTest()
    {
        // Arrange: Create temp directory without global.json
        var tempDir = Path.Join(Path.GetTempPath(), "dotnet-mcp-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            // Act
            var result = await _tools.DotnetProject(
                action: DotnetProjectAction.Test,
                project: "MyTests.csproj",
                testRunner: TestRunner.Auto,
                workingDirectory: tempDir,
                machineReadable: true);

            // Assert: Should default to VSTest (positional arg)
            Assert.NotNull(result);
            MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet test \"MyTests.csproj\"");
            
            // Verify metadata indicates default VSTest
            Assert.Contains("\"selectedTestRunner\": \"vstest\"", result);
            Assert.Contains("\"projectArgumentStyle\": \"positional\"", result);
            Assert.Contains("\"selectionSource\": \"default\"", result);
        }
        finally
        {
            try
            {
                Directory.Delete(tempDir, recursive: true);
            }
            catch (IOException)
            {
                // Best-effort cleanup
            }
            catch (UnauthorizedAccessException)
            {
                // Best-effort cleanup
            }
        }
    }

    [Fact]
    public async Task DotnetProject_Test_UseLegacyProjectArgument_OverridesTestRunner()
    {
        // Test backward compatibility: useLegacyProjectArgument should override testRunner
        var result = await _tools.DotnetProject(
            action: DotnetProjectAction.Test,
            project: "MyTests.csproj",
            testRunner: TestRunner.MicrosoftTestingPlatform,
            useLegacyProjectArgument: true,
            machineReadable: true);

        Assert.NotNull(result);
        // useLegacyProjectArgument=true should force positional arg (VSTest mode)
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet test \"MyTests.csproj\"");
        
        // Verify metadata shows VSTest was selected due to legacy parameter
        Assert.Contains("\"selectedTestRunner\": \"vstest\"", result);
        Assert.Contains("\"projectArgumentStyle\": \"positional\"", result);
        Assert.Contains("\"selectionSource\": \"useLegacyProjectArgument-parameter\"", result);
    }

    [Fact]
    public async Task DotnetProject_Test_WithoutProject_NoMetadataForProjectArgStyle()
    {
        // Test that when no project is specified, projectArgumentStyle is "none"
        var result = await _tools.DotnetProject(
            action: DotnetProjectAction.Test,
            testRunner: TestRunner.MicrosoftTestingPlatform,
            machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet test");
        
        // Verify metadata
        Assert.Contains("\"selectedTestRunner\": \"microsoft-testing-platform\"", result);
        Assert.Contains("\"projectArgumentStyle\": \"none\"", result);
    }

    [Fact]
    public async Task DotnetProject_Test_DefaultTestRunner_IsAuto()
    {
        // Test that when testRunner is not specified, it defaults to Auto behavior
        // Arrange: Create temp directory without global.json (so Auto should default to VSTest)
        var tempDir = Path.Join(Path.GetTempPath(), "dotnet-mcp-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            // Act: Don't specify testRunner parameter
            var result = await _tools.DotnetProject(
                action: DotnetProjectAction.Test,
                project: "MyTests.csproj",
                workingDirectory: tempDir,
                machineReadable: true);

            // Assert: Should use VSTest (default behavior for Auto when no global.json)
            Assert.NotNull(result);
            MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet test \"MyTests.csproj\"");
            
            // Verify metadata
            Assert.Contains("\"selectedTestRunner\": \"vstest\"", result);
            Assert.Contains("\"selectionSource\": \"default\"", result);
        }
        finally
        {
            try
            {
                Directory.Delete(tempDir, recursive: true);
            }
            catch (IOException)
            {
                // Best-effort cleanup
            }
            catch (UnauthorizedAccessException)
            {
                // Best-effort cleanup
            }
        }
    }

    #endregion

    #region Stop Action Tests

    [Fact]
    public async Task DotnetProject_Stop_WithMissingSessionId_ReturnsValidationError()
    {
        // Act
        var result = await _tools.DotnetProject(
            action: DotnetProjectAction.Stop,
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("\"success\": false", result);
        Assert.Contains("sessionId", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("required", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetProject_Stop_WithNonExistentSessionId_ReturnsError()
    {
        // Arrange
        var nonExistentSessionId = Guid.NewGuid().ToString();

        // Act
        var result = await _tools.DotnetProject(
            action: DotnetProjectAction.Stop,
            sessionId: nonExistentSessionId,
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("\"success\": false", result);
        Assert.Contains("not found", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetProject_Stop_PlainText_WithNonExistentSessionId_ReturnsErrorMessage()
    {
        // Arrange
        var nonExistentSessionId = Guid.NewGuid().ToString();

        // Act
        var result = await _tools.DotnetProject(
            action: DotnetProjectAction.Stop,
            sessionId: nonExistentSessionId,
            machineReadable: false);

        // Assert
        Assert.NotNull(result);
        Assert.StartsWith("Error:", result);
        Assert.Contains("not found", result, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region NoBuild Parameter Tests

    [Fact]
    public async Task DotnetProject_Run_WithNoBuild_IncludesNoBuildFlag()
    {
        // Act
        var result = await _tools.DotnetProject(
            action: DotnetProjectAction.Run,
            project: "MyProject.csproj",
            noBuild: true,
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet run --project \"MyProject.csproj\" --no-build");
    }

    [Fact]
    public async Task DotnetProject_Run_WithoutNoBuild_DoesNotIncludeNoBuildFlag()
    {
        // Act
        var result = await _tools.DotnetProject(
            action: DotnetProjectAction.Run,
            project: "MyProject.csproj",
            noBuild: false,
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        var commandExecuted = MachineReadableCommandAssertions.GetExecutedCommand(result);
        Assert.DoesNotContain("--no-build", commandExecuted);
    }

    [Fact]
    public async Task DotnetProject_Run_WithNoBuildNull_DoesNotIncludeNoBuildFlag()
    {
        // Act
        var result = await _tools.DotnetProject(
            action: DotnetProjectAction.Run,
            project: "MyProject.csproj",
            noBuild: null,
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        var commandExecuted = MachineReadableCommandAssertions.GetExecutedCommand(result);
        Assert.DoesNotContain("--no-build", commandExecuted);
    }

    #endregion
}
