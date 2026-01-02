using System;
using System.IO;
using DotNetMcp;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotNetMcp.Tests;

/// <summary>
/// Tests for solution-related MCP tools
/// </summary>
[Collection("ProcessWideStateTests")]
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
        // Arrange
        var originalDirectory = Environment.CurrentDirectory;
        var tempDirectory = Path.Combine(Path.GetTempPath(), "dotnet-mcp-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        try
        {
            Environment.CurrentDirectory = tempDirectory;

            // Act
            var result = await _tools.DotnetSolutionCreate(
                name: "MySolution",
                machineReadable: true);

            // Assert
            Assert.NotNull(result);
            MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet new sln -n \"MySolution\"");
        }
        finally
        {
            Environment.CurrentDirectory = originalDirectory;
            if (Directory.Exists(tempDirectory))
                Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task DotnetSolutionCreate_WithOutput_BuildsCorrectCommand()
    {
        // Arrange
        var tempDirectory = Path.Combine(Path.GetTempPath(), "dotnet-mcp-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        try
        {
            // Act
            var result = await _tools.DotnetSolutionCreate(
                name: "MySolution",
                output: tempDirectory,
                machineReadable: true);

            // Assert
            Assert.NotNull(result);
            MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, $"dotnet new sln -n \"MySolution\" -o \"{tempDirectory}\"");
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
                Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task DotnetSolutionCreate_WithSlnFormat_BuildsCorrectCommand()
    {
        // Arrange
        var originalDirectory = Environment.CurrentDirectory;
        var tempDirectory = Path.Combine(Path.GetTempPath(), "dotnet-mcp-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        try
        {
            Environment.CurrentDirectory = tempDirectory;

            // Act
            var result = await _tools.DotnetSolutionCreate(
                name: "MySolution",
                format: "sln",
                machineReadable: true);

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
    public async Task DotnetSolutionCreate_WithSlnxFormat_BuildsCorrectCommand()
    {
        // Arrange
        var originalDirectory = Environment.CurrentDirectory;
        var tempDirectory = Path.Combine(Path.GetTempPath(), "dotnet-mcp-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        try
        {
            Environment.CurrentDirectory = tempDirectory;

            // Act
            var result = await _tools.DotnetSolutionCreate(
                name: "MySolution",
                format: "slnx",
                machineReadable: true);

            // Assert
            Assert.NotNull(result);
            MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet new sln -n \"MySolution\" --format slnx");
        }
        finally
        {
            Environment.CurrentDirectory = originalDirectory;
            if (Directory.Exists(tempDirectory))
                Directory.Delete(tempDirectory, recursive: true);
        }
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
        // Arrange
        var tempDirectory = Path.Combine(Path.GetTempPath(), "dotnet-mcp-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        // Act
        var result = await _tools.DotnetSolutionCreate(
            name: "MySolution",
            output: tempDirectory,
            format: "slnx",
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, $"dotnet new sln -n \"MySolution\" -o \"{tempDirectory}\" --format slnx");

        if (Directory.Exists(tempDirectory))
            Directory.Delete(tempDirectory, recursive: true);
    }

    [Fact]
    public async Task DotnetSolutionAdd_WithSingleProject_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetSolutionAdd(
            solution: "MySolution.sln",
            projects: new[] { "MyProject.csproj" },
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet solution \"MySolution.sln\" add \"MyProject.csproj\"");
    }

    [Fact]
    public async Task DotnetSolutionAdd_WithMultipleProjects_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetSolutionAdd(
            solution: "MySolution.sln",
            projects: new[] { "Project1.csproj", "Project2.csproj", "Project3.csproj" },
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(
            result,
            "dotnet solution \"MySolution.sln\" add \"Project1.csproj\" \"Project2.csproj\" \"Project3.csproj\"");
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
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet solution \"MySolution.sln\" add \"MyProject.csproj\"");
    }

    [Fact]
    public async Task DotnetSolutionList_WithSolution_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetSolutionList(
            solution: "MySolution.sln",
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet solution \"MySolution.sln\" list");
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
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet solution \"MySolution.sln\" list");
    }

    [Fact]
    public async Task DotnetSolutionRemove_WithSingleProject_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetSolutionRemove(
            solution: "MySolution.sln",
            projects: new[] { "MyProject.csproj" },
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet solution \"MySolution.sln\" remove \"MyProject.csproj\"");
    }

    [Fact]
    public async Task DotnetSolutionRemove_WithMultipleProjects_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetSolutionRemove(
            solution: "MySolution.sln",
            projects: new[] { "Project1.csproj", "Project2.csproj" },
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(
            result,
            "dotnet solution \"MySolution.sln\" remove \"Project1.csproj\" \"Project2.csproj\"");
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
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet solution \"MySolution.sln\" remove \"MyProject.csproj\"");
    }
}
