using DotNetMcp;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotNetMcp.Tests;

/// <summary>
/// Backward compatibility smoke tests for legacy Entity Framework Core MCP tools.
/// 
/// NOTE: Comprehensive parameter-matrix and command-building tests are in EntityFrameworkConsolidatedToolTests.cs.
/// These tests ensure legacy tools still work for backwards compatibility.
/// </summary>
public class EntityFrameworkCoreToolsTests
{
    private readonly DotNetCliTools _tools;
    private readonly ConcurrencyManager _concurrencyManager;

    public EntityFrameworkCoreToolsTests()
    {
        _concurrencyManager = new ConcurrencyManager();
        _tools = new DotNetCliTools(NullLogger<DotNetCliTools>.Instance, _concurrencyManager);
    }

    [Fact]
    public async Task DotnetEfMigrationsAdd_BackCompatSmokeTest()
    {
        // Smoke test: ensure legacy tool still works
        var result = await _tools.DotnetEfMigrationsAdd(
            name: "InitialCreate",
            machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet ef migrations add \"InitialCreate\"");
    }

    [Fact]
    public async Task DotnetEfMigrationsList_BackCompatSmokeTest()
    {
        // Smoke test: ensure legacy tool still works
        var result = await _tools.DotnetEfMigrationsList(machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet ef migrations list");
    }

    [Fact]
    public async Task DotnetEfMigrationsRemove_BackCompatSmokeTest()
    {
        // Smoke test: ensure legacy tool still works
        var result = await _tools.DotnetEfMigrationsRemove(
            force: true,
            machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet ef migrations remove --force");
    }

    [Fact]
    public async Task DotnetEfDatabaseUpdate_BackCompatSmokeTest()
    {
        // Smoke test: ensure legacy tool still works
        var result = await _tools.DotnetEfDatabaseUpdate(machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet ef database update");
    }

    [Fact]
    public async Task DotnetEfDbContextList_BackCompatSmokeTest()
    {
        // Smoke test: ensure legacy tool still works
        var result = await _tools.DotnetEfDbContextList(machineReadable: true);

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet ef dbcontext list");
    }
}
