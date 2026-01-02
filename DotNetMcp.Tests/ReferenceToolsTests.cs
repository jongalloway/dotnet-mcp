using DotNetMcp;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotNetMcp.Tests;

/// <summary>
/// Tests for reference-related MCP tools
/// </summary>
public class ReferenceToolsTests
{
    private readonly DotNetCliTools _tools;
    private readonly ConcurrencyManager _concurrencyManager;

    public ReferenceToolsTests()
    {
        _concurrencyManager = new ConcurrencyManager();
        _tools = new DotNetCliTools(NullLogger<DotNetCliTools>.Instance, _concurrencyManager);
    }

    [Fact]
    public async Task DotnetReferenceAdd_WithRequiredParameters_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetReferenceAdd(
            project: "MyProject.csproj",
            reference: "MyLibrary.csproj",
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet add \"MyProject.csproj\" reference \"MyLibrary.csproj\"");
    }

    [Fact]
    public async Task DotnetReferenceAdd_WithMachineReadable_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetReferenceAdd(
            project: "MyProject.csproj",
            reference: "MyLibrary.csproj",
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet add \"MyProject.csproj\" reference \"MyLibrary.csproj\"");
    }

    [Fact]
    public async Task DotnetReferenceList_WithoutParameters_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetReferenceList(machineReadable: true);

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet list reference");
    }

    [Fact]
    public async Task DotnetReferenceList_WithProject_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetReferenceList(project: "MyProject.csproj", machineReadable: true);

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet list \"MyProject.csproj\" reference");
    }

    [Fact]
    public async Task DotnetReferenceList_WithMachineReadable_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetReferenceList(machineReadable: true);

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet list reference");
    }

    [Fact]
    public async Task DotnetReferenceRemove_WithRequiredParameters_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetReferenceRemove(
            project: "MyProject.csproj",
            reference: "MyLibrary.csproj",
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet remove \"MyProject.csproj\" reference \"MyLibrary.csproj\"");
    }

    [Fact]
    public async Task DotnetReferenceRemove_WithMachineReadable_BuildsCorrectCommand()
    {
        // Act
        var result = await _tools.DotnetReferenceRemove(
            project: "MyProject.csproj",
            reference: "MyLibrary.csproj",
            machineReadable: true);

        // Assert
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet remove \"MyProject.csproj\" reference \"MyLibrary.csproj\"");
    }
}
