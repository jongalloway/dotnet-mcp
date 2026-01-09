using DotNetMcp;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotNetMcp.Tests;

/// <summary>
/// Backward compatibility smoke tests for legacy project-related MCP tools.
/// 
/// NOTE: Comprehensive parameter-matrix and command-building tests are in ConsolidatedProjectToolTests.cs.
/// These tests ensure legacy tools still work for backwards compatibility.
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
    public async Task DotnetProjectRestore_BackCompatSmokeTest()
    {
        // Smoke test: ensure legacy tool still works
        var result = await _tools.DotnetProjectRestore(machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet restore");
    }

    [Fact]
    public async Task DotnetProjectClean_BackCompatSmokeTest()
    {
        // Smoke test: ensure legacy tool still works
        var result = await _tools.DotnetProjectClean(machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet clean");
    }
}
