using DotNetMcp;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotNetMcp.Tests;

/// <summary>
/// Tests for solution-related MCP tools
/// </summary>
public class SolutionToolsTests
{
    private readonly DotNetCliTools _tools;
    private readonly ConcurrencyManager _concurrencyManager;

    public SolutionToolsTests()
    {
        _concurrencyManager = new ConcurrencyManager();
        _tools = new DotNetCliTools(NullLogger<DotNetCliTools>.Instance, _concurrencyManager);
    }

    [Fact]
    public async Task DotnetSolutionCreate_WithName_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetSolutionCreate(name: "MySolution");

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
    }

    [Fact]
    public async Task DotnetSolutionCreate_WithOutput_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetSolutionCreate(
            name: "MySolution",
            output: "./solution");

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
    }

    [Fact]
    public async Task DotnetSolutionCreate_WithSlnFormat_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetSolutionCreate(
            name: "MySolution",
            format: "sln");

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
    }

    [Fact]
    public async Task DotnetSolutionCreate_WithSlnxFormat_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetSolutionCreate(
            name: "MySolution",
            format: "slnx");

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
    }

    [Fact]
    public async Task DotnetSolutionCreate_WithInvalidFormat_ReturnsError()
    {
        // Act
        var result = await _tools.DotnetSolutionCreate(
            name: "MySolution",
            format: "invalid");

        // Assert
        Assert.Contains("Error", result);
        Assert.Contains("format must be either 'sln' or 'slnx'", result);
    }

    [Fact]
    public async Task DotnetSolutionCreate_WithAllParameters_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetSolutionCreate(
            name: "MySolution",
            output: "./solution",
            format: "slnx",
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
    }

    [Fact]
    public async Task DotnetSolutionAdd_WithSingleProject_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetSolutionAdd(
            solution: "MySolution.sln",
            projects: new[] { "MyProject.csproj" });

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
    }

    [Fact]
    public async Task DotnetSolutionAdd_WithMultipleProjects_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetSolutionAdd(
            solution: "MySolution.sln",
            projects: new[] { "Project1.csproj", "Project2.csproj", "Project3.csproj" });

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
    }

    [Fact]
    public async Task DotnetSolutionAdd_WithEmptyProjectArray_ReturnsError()
    {
        // Act
        var result = await _tools.DotnetSolutionAdd(
            solution: "MySolution.sln",
            projects: Array.Empty<string>());

        // Assert
        Assert.Contains("Error", result);
        Assert.Contains("at least one project path is required", result);
    }

    [Fact]
    public async Task DotnetSolutionAdd_WithNullProjects_ReturnsError()
    {
        // Act
        var result = await _tools.DotnetSolutionAdd(
            solution: "MySolution.sln",
            projects: null!);

        // Assert
        Assert.Contains("Error", result);
        Assert.Contains("at least one project path is required", result);
    }

    [Fact]
    public async Task DotnetSolutionAdd_WithMachineReadable_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetSolutionAdd(
            solution: "MySolution.sln",
            projects: new[] { "MyProject.csproj" },
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
    }

    [Fact]
    public async Task DotnetSolutionList_WithSolution_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetSolutionList(solution: "MySolution.sln");

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
    }

    [Fact]
    public async Task DotnetSolutionList_WithMachineReadable_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetSolutionList(
            solution: "MySolution.sln",
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
    }

    [Fact]
    public async Task DotnetSolutionRemove_WithSingleProject_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetSolutionRemove(
            solution: "MySolution.sln",
            projects: new[] { "MyProject.csproj" });

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
    }

    [Fact]
    public async Task DotnetSolutionRemove_WithMultipleProjects_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetSolutionRemove(
            solution: "MySolution.sln",
            projects: new[] { "Project1.csproj", "Project2.csproj" });

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
    }

    [Fact]
    public async Task DotnetSolutionRemove_WithEmptyProjectArray_ReturnsError()
    {
        // Act
        var result = await _tools.DotnetSolutionRemove(
            solution: "MySolution.sln",
            projects: Array.Empty<string>());

        // Assert
        Assert.Contains("Error", result);
        Assert.Contains("at least one project path is required", result);
    }

    [Fact]
    public async Task DotnetSolutionRemove_WithNullProjects_ReturnsError()
    {
        // Act
        var result = await _tools.DotnetSolutionRemove(
            solution: "MySolution.sln",
            projects: null!);

        // Assert
        Assert.Contains("Error", result);
        Assert.Contains("at least one project path is required", result);
    }

    [Fact]
    public async Task DotnetSolutionRemove_WithMachineReadable_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetSolutionRemove(
            solution: "MySolution.sln",
            projects: new[] { "MyProject.csproj" },
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Error:", result);
    }
}
