using DotNetMcp;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotNetMcp.Tests;

/// <summary>
/// Backward compatibility smoke tests for legacy workload-related MCP tools.
/// 
/// NOTE: Comprehensive parameter-matrix and command-building tests are in ConsolidatedWorkloadToolTests.cs.
/// These tests ensure legacy tools still work for backwards compatibility.
/// </summary>
public class WorkloadToolsTests
{
    private readonly DotNetCliTools _tools;
    private readonly ConcurrencyManager _concurrencyManager;

    public WorkloadToolsTests()
    {
        _concurrencyManager = new ConcurrencyManager();
        _tools = new DotNetCliTools(NullLogger<DotNetCliTools>.Instance, _concurrencyManager);
    }

    [Fact]
    public async Task DotnetWorkloadList_BackCompatSmokeTest()
    {
        // Smoke test: ensure legacy tool still works
        var result = await _tools.DotnetWorkloadList(machineReadable: true);

        Assert.NotNull(result);
        // Workload list returns structured data
        Assert.Contains("dotnet workload list", result);
    }

    [Fact]
    public async Task DotnetWorkloadSearch_BackCompatSmokeTest()
    {
        // Smoke test: ensure legacy tool still works
        var result = await _tools.DotnetWorkloadSearch(machineReadable: true);

        Assert.NotNull(result);
        // Search returns workload information
    }

    [Fact]
    public async Task DotnetWorkloadInstall_BackCompatSmokeTest()
    {
        // Smoke test: ensure legacy tool still works with validation
        var result = await _tools.DotnetWorkloadInstall(
            workloadIds: "maui",
            machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet workload install maui");
    }

    [Fact]
    public async Task DotnetWorkloadUpdate_BackCompatSmokeTest()
    {
        // Smoke test: ensure legacy tool still works
        var result = await _tools.DotnetWorkloadUpdate(machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet workload update");
    }

    [Fact]
    public async Task DotnetWorkloadUninstall_BackCompatSmokeTest()
    {
        // Smoke test: ensure legacy tool still works
        var result = await _tools.DotnetWorkloadUninstall(
            workloadIds: "maui",
            machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet workload uninstall maui");
    }
}
