using System;
using System.IO;
using DotNetMcp;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotNetMcp.Tests;

/// <summary>
/// Tests for consolidated solution-related MCP tools.
/// Tests the DotnetSolution consolidated tool with various actions.
/// </summary>
[Collection("ProcessWideStateTests")]
public class SolutionToolsTests
{
    private readonly DotNetCliTools _tools;
    private readonly ConcurrencyManager _concurrencyManager;

    public SolutionToolsTests()
    {
        _concurrencyManager = new ConcurrencyManager();
        _tools = new DotNetCliTools(NullLogger<DotNetCliTools>.Instance, _concurrencyManager, new ProcessSessionManager());
    }

    // ===== Consolidated DotnetSolution Tests =====

    [Fact]
    public async Task DotnetSolution_CreateAction_WithName_BuildsCorrectCommand()
    {
        // Arrange
        var originalDirectory = Environment.CurrentDirectory;
        var tempDirectory = Path.Join(Path.GetTempPath(), "dotnet-mcp-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        try
        {
            Environment.CurrentDirectory = tempDirectory;

            // Act
            var result = (await _tools.DotnetSolution(
                action: DotNetMcp.Actions.DotnetSolutionAction.Create,
                name: "MySolution")).GetText();

            // Assert
            Assert.NotNull(result);
            MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet new sln -n \"MySolution\" --format sln");
        }
        finally
        {
            Environment.CurrentDirectory = originalDirectory;
            if (Directory.Exists(tempDirectory))
                Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task DotnetSolution_CreateAction_WithAllParameters_BuildsCorrectCommand()
    {
        // Arrange
        var tempDirectory = Path.Join(Path.GetTempPath(), "dotnet-mcp-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        try
        {
            // Act
            var result = (await _tools.DotnetSolution(
                action: DotNetMcp.Actions.DotnetSolutionAction.Create,
                name: "MySolution",
                output: tempDirectory,
                format: "slnx")).GetText();

            // Assert
            Assert.NotNull(result);
            MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, $"dotnet new sln -n \"MySolution\" -o \"{tempDirectory}\" --format slnx");
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
                Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task DotnetSolution_CreateAction_WithoutName_ReturnsError()
    {
        // Act
        var result = (await _tools.DotnetSolution(
            action: DotNetMcp.Actions.DotnetSolutionAction.Create)).GetText();

        // Assert
        Assert.Contains("Error", result);
        Assert.Contains("name", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetSolution_AddAction_WithSingleProject_BuildsCorrectCommand()
    {
        // Act
        var result = (await _tools.DotnetSolution(
            action: DotNetMcp.Actions.DotnetSolutionAction.Add,
            solution: "MySolution.sln",
            projects: new[] { "MyProject.csproj" })).GetText();

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet solution \"MySolution.sln\" add \"MyProject.csproj\"");
    }

    [Fact]
    public async Task DotnetSolution_AddAction_WithMultipleProjects_BuildsCorrectCommand()
    {
        // Act
        var result = (await _tools.DotnetSolution(
            action: DotNetMcp.Actions.DotnetSolutionAction.Add,
            solution: "MySolution.sln",
            projects: new[] { "Project1.csproj", "Project2.csproj" })).GetText();

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(
            result,
            "dotnet solution \"MySolution.sln\" add \"Project1.csproj\" \"Project2.csproj\"");
    }

    [Fact]
    public async Task DotnetSolution_AddAction_WithoutSolution_ReturnsError()
    {
        // Act
        var result = (await _tools.DotnetSolution(
            action: DotNetMcp.Actions.DotnetSolutionAction.Add,
            projects: new[] { "MyProject.csproj" })).GetText();

        // Assert
        Assert.Contains("Error", result);
        Assert.Contains("solution", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetSolution_AddAction_WithoutProjects_ReturnsError()
    {
        // Act
        var result = (await _tools.DotnetSolution(
            action: DotNetMcp.Actions.DotnetSolutionAction.Add,
            solution: "MySolution.sln")).GetText();

        // Assert
        Assert.Contains("Error", result);
        Assert.Contains("project", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetSolution_AddAction_WithEmptyProjectsArray_ReturnsError()
    {
        // Act
        var result = (await _tools.DotnetSolution(
            action: DotNetMcp.Actions.DotnetSolutionAction.Add,
            solution: "MySolution.sln",
            projects: Array.Empty<string>())).GetText();

        // Assert
        Assert.Contains("Error", result);
        Assert.Contains("project", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetSolution_ListAction_BuildsCorrectCommand()
    {
        // Act
        var result = (await _tools.DotnetSolution(
            action: DotNetMcp.Actions.DotnetSolutionAction.List,
            solution: "MySolution.sln")).GetText();

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet solution \"MySolution.sln\" list");
    }

    [Fact]
    public async Task DotnetSolution_ListAction_WithoutSolution_ReturnsError()
    {
        // Act
        var result = (await _tools.DotnetSolution(
            action: DotNetMcp.Actions.DotnetSolutionAction.List)).GetText();

        // Assert
        Assert.Contains("Error", result);
        Assert.Contains("solution", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetSolution_RemoveAction_WithSingleProject_BuildsCorrectCommand()
    {
        // Act
        var result = (await _tools.DotnetSolution(
            action: DotNetMcp.Actions.DotnetSolutionAction.Remove,
            solution: "MySolution.sln",
            projects: new[] { "MyProject.csproj" })).GetText();

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet solution \"MySolution.sln\" remove \"MyProject.csproj\"");
    }

    [Fact]
    public async Task DotnetSolution_RemoveAction_WithMultipleProjects_BuildsCorrectCommand()
    {
        // Act
        var result = (await _tools.DotnetSolution(
            action: DotNetMcp.Actions.DotnetSolutionAction.Remove,
            solution: "MySolution.sln",
            projects: new[] { "Project1.csproj", "Project2.csproj" })).GetText();

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(
            result,
            "dotnet solution \"MySolution.sln\" remove \"Project1.csproj\" \"Project2.csproj\"");
    }

    [Fact]
    public async Task DotnetSolution_RemoveAction_WithoutSolution_ReturnsError()
    {
        // Act
        var result = (await _tools.DotnetSolution(
            action: DotNetMcp.Actions.DotnetSolutionAction.Remove,
            projects: new[] { "MyProject.csproj" })).GetText();

        // Assert
        Assert.Contains("Error", result);
        Assert.Contains("solution", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetSolution_RemoveAction_WithoutProjects_ReturnsError()
    {
        // Act
        var result = (await _tools.DotnetSolution(
            action: DotNetMcp.Actions.DotnetSolutionAction.Remove,
            solution: "MySolution.sln")).GetText();

        // Assert
        Assert.Contains("Error", result);
        Assert.Contains("project", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetSolution_RemoveAction_WithEmptyProjectsArray_ReturnsError()
    {
        // Act
        var result = (await _tools.DotnetSolution(
            action: DotNetMcp.Actions.DotnetSolutionAction.Remove,
            solution: "MySolution.sln",
            projects: Array.Empty<string>())).GetText();

        // Assert
        Assert.Contains("Error", result);
        Assert.Contains("project", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetSolution_CreateAction_MachineReadableError_ReturnsJson()
    {
        // Act
        var result = (await _tools.DotnetSolution(
            action: DotNetMcp.Actions.DotnetSolutionAction.Create)).GetText();

        // Assert
        Assert.Contains("\"success\": false", result);
        Assert.Contains("INVALID_PARAMS", result);
    }

    [Fact]
    public async Task DotnetSolution_AddAction_MachineReadableError_ReturnsJson()
    {
        // Act
        var result = (await _tools.DotnetSolution(
            action: DotNetMcp.Actions.DotnetSolutionAction.Add,
            solution: "MySolution.sln")).GetText();

        // Assert
        Assert.Contains("\"success\": false", result);
        Assert.Contains("INVALID_PARAMS", result);
    }

    [Fact]
    public async Task DotnetSolution_CreateAction_WithInvalidFormat_ReturnsError()
    {
        // Act
        var result = (await _tools.DotnetSolution(
            action: DotNetMcp.Actions.DotnetSolutionAction.Create,
            name: "MySolution",
            format: "invalid")).GetText();

        // Assert
        Assert.Contains("Error", result);
        Assert.Contains("format", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("sln", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("slnx", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetSolution_CreateAction_WithInvalidFormat_MachineReadable_ReturnsJson()
    {
        // Act
        var result = (await _tools.DotnetSolution(
            action: DotNetMcp.Actions.DotnetSolutionAction.Create,
            name: "MySolution",
            format: "invalid")).GetText();

        // Assert
        Assert.Contains("\"success\": false", result);
        Assert.Contains("INVALID_PARAMS", result);
        Assert.Contains("format", result, StringComparison.OrdinalIgnoreCase);
    }
}
