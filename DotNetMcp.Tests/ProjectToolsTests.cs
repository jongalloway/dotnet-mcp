using DotNetMcp;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotNetMcp.Tests;

/// <summary>
/// Tests for project-related MCP tools that don't have existing tests
/// </summary>
public class ProjectToolsTests
{
    private readonly DotNetCliTools _tools;
    private readonly ConcurrencyManager _concurrencyManager;

    public ProjectToolsTests()
    {
        _concurrencyManager = new ConcurrencyManager();
        _tools = new DotNetCliTools(NullLogger<DotNetCliTools>.Instance, _concurrencyManager);
    }

    [Fact]
    public async Task DotnetProjectRestore_WithoutParameters_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetProjectRestore(machineReadable: true);

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet restore");
    }

    [Fact]
    public async Task DotnetProjectRestore_WithProject_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetProjectRestore(project: "MyProject.csproj", machineReadable: true);

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet restore \"MyProject.csproj\"");
    }

    [Fact]
    public async Task DotnetProjectRestore_WithMachineReadable_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetProjectRestore(machineReadable: true);

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet restore");
    }

    [Fact]
    public async Task DotnetProjectClean_WithoutParameters_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetProjectClean(machineReadable: true);

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet clean");
    }

    [Fact]
    public async Task DotnetProjectClean_WithProject_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetProjectClean(project: "MyProject.csproj", machineReadable: true);

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet clean \"MyProject.csproj\"");
    }

    [Fact]
    public async Task DotnetProjectClean_WithConfiguration_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetProjectClean(configuration: "Release", machineReadable: true);

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet clean -c Release");
    }

    [Fact]
    public async Task DotnetProjectClean_WithAllParameters_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetProjectClean(
            project: "MyProject.csproj",
            configuration: "Release",
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet clean \"MyProject.csproj\" -c Release");
    }
}
