using DotNetMcp;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotNetMcp.Tests;

/// <summary>
/// Backward compatibility smoke tests for legacy reference-related MCP tools.
/// 
/// NOTE: Comprehensive tests are in ConsolidatedPackageToolTests.cs (references are part of package management).
/// These tests ensure legacy tools still work for backwards compatibility.
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
    public async Task DotnetReferenceAdd_BackCompatSmokeTest()
    {
        // Smoke test: ensure legacy tool still works
        var result = await _tools.DotnetReferenceAdd(
            project: "MyProject.csproj",
            reference: "MyLibrary.csproj",
            machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet add \"MyProject.csproj\" reference \"MyLibrary.csproj\"");
    }

    [Fact]
    public async Task DotnetReferenceList_BackCompatSmokeTest()
    {
        // Smoke test: ensure legacy tool still works
        var result = await _tools.DotnetReferenceList(machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet list reference");
    }

    [Fact]
    public async Task DotnetReferenceRemove_BackCompatSmokeTest()
    {
        // Smoke test: ensure legacy tool still works
        var result = await _tools.DotnetReferenceRemove(
            project: "MyProject.csproj",
            reference: "MyLibrary.csproj",
            machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet remove \"MyProject.csproj\" reference \"MyLibrary.csproj\"");
    }
}
