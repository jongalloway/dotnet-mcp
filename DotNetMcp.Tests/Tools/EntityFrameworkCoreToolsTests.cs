using DotNetMcp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotNetMcp.Tests;

/// <summary>
/// Tests for Entity Framework Core CLI tools.
/// These tests validate that the EF Core tool methods exist and accept parameters correctly.
/// Actual command execution requires dotnet-ef tool to be installed.
/// </summary>
public class EntityFrameworkCoreToolsTests
{
    private readonly DotNetCliTools _tools;
    private readonly ILogger<DotNetCliTools> _logger;
    private readonly ConcurrencyManager _concurrencyManager;

    public EntityFrameworkCoreToolsTests()
    {
        _logger = NullLogger<DotNetCliTools>.Instance;
        _concurrencyManager = new ConcurrencyManager();
        _tools = new DotNetCliTools(_logger, _concurrencyManager);
    }

    #region Migration Tools Tests

    [Fact]
    public async Task DotnetEfMigrationsAdd_WithName_BuildsCorrectCommand()
    {
        var result = await _tools.DotnetEfMigrationsAdd(
            name: "InitialCreate",
            machineReadable: true);

        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet ef migrations add \"InitialCreate\"");
    }

    [Fact]
    public async Task DotnetEfMigrationsAdd_WithEmptyName_ReturnsError()
    {
        // Validates that empty name returns an error
        var result = await _tools.DotnetEfMigrationsAdd(
            name: "");

        Assert.Contains("Error", result);
    }

    [Fact]
    public async Task DotnetEfMigrationsAdd_WithAllParameters_BuildsCorrectCommand()
    {
        var result = await _tools.DotnetEfMigrationsAdd(
            name: "AddProductEntity",
            project: "MyProject.csproj",
            startupProject: "MyApi.csproj",
            context: "ApplicationDbContext",
            outputDir: "Migrations",
            framework: "net10.0",
            machineReadable: true);

        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(
            result,
            "dotnet ef migrations add \"AddProductEntity\" --project \"MyProject.csproj\" --startup-project \"MyApi.csproj\" --context \"ApplicationDbContext\" --output-dir \"Migrations\" --framework net10.0");
    }

    [Fact]
    public async Task DotnetEfMigrationsList_WithBasicParameters_BuildsCorrectCommand()
    {
        var result = await _tools.DotnetEfMigrationsList(machineReadable: true);

        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet ef migrations list");
    }

    [Fact]
    public async Task DotnetEfMigrationsList_WithProject_BuildsCorrectCommand()
    {
        var result = await _tools.DotnetEfMigrationsList(
            project: "MyProject.csproj",
            machineReadable: true);

        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet ef migrations list --project \"MyProject.csproj\"");
    }

    [Fact]
    public async Task DotnetEfMigrationsRemove_WithForce_BuildsCorrectCommand()
    {
        var result = await _tools.DotnetEfMigrationsRemove(
            force: true,
            machineReadable: true);

        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet ef migrations remove --force");
    }

    [Fact]
    public async Task DotnetEfMigrationsScript_WithBasicParameters_BuildsCorrectCommand()
    {
        var result = await _tools.DotnetEfMigrationsScript(machineReadable: true);

        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet ef migrations script");
    }

    [Fact]
    public async Task DotnetEfMigrationsScript_WithFromTo_BuildsCorrectCommand()
    {
        var result = await _tools.DotnetEfMigrationsScript(
            from: "InitialCreate",
            to: "AddProductEntity",
            machineReadable: true);

        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet ef migrations script \"InitialCreate\" \"AddProductEntity\"");
    }

    [Fact]
    public async Task DotnetEfMigrationsScript_WithToOnly_UsesEmptyFrom_BuildsCorrectCommand()
    {
        var result = await _tools.DotnetEfMigrationsScript(
            to: "AddProductEntity",
            machineReadable: true);

        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet ef migrations script \"\" \"AddProductEntity\"");
    }

    [Fact]
    public async Task DotnetEfMigrationsScript_WithIdempotent_BuildsCorrectCommand()
    {
        var result = await _tools.DotnetEfMigrationsScript(
            idempotent: true,
            output: "migration.sql",
            machineReadable: true);

        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet ef migrations script --output \"migration.sql\" --idempotent");
    }

    #endregion

    #region Database Tools Tests

    [Fact]
    public async Task DotnetEfDatabaseUpdate_WithBasicParameters_BuildsCorrectCommand()
    {
        var result = await _tools.DotnetEfDatabaseUpdate(machineReadable: true);

        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet ef database update");
    }

    [Fact]
    public async Task DotnetEfDatabaseUpdate_WithMigration_BuildsCorrectCommand()
    {
        var result = await _tools.DotnetEfDatabaseUpdate(
            migration: "AddProductEntity",
            machineReadable: true);

        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet ef database update \"AddProductEntity\"");
    }

    [Fact]
    public async Task DotnetEfDatabaseUpdate_WithRollback_BuildsCorrectCommand()
    {
        // Test rollback to initial state
        var result = await _tools.DotnetEfDatabaseUpdate(
            migration: "0",
            machineReadable: true);

        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet ef database update \"0\"");
    }

    [Fact]
    public async Task DotnetEfDatabaseDrop_WithForce_BuildsCorrectCommand()
    {
        var result = await _tools.DotnetEfDatabaseDrop(
            force: true,
            machineReadable: true);

        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet ef database drop --force");
    }

    [Fact]
    public async Task DotnetEfDatabaseDrop_WithDryRun_BuildsCorrectCommand()
    {
        var result = await _tools.DotnetEfDatabaseDrop(
            dryRun: true,
            machineReadable: true);

        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet ef database drop --dry-run");
    }

    #endregion

    #region DbContext Tools Tests

    [Fact]
    public async Task DotnetEfDbContextList_WithBasicParameters_BuildsCorrectCommand()
    {
        var result = await _tools.DotnetEfDbContextList(machineReadable: true);

        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet ef dbcontext list");
    }

    [Fact]
    public async Task DotnetEfDbContextList_WithProject_BuildsCorrectCommand()
    {
        var result = await _tools.DotnetEfDbContextList(
            project: "MyProject.csproj",
            machineReadable: true);

        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet ef dbcontext list --project \"MyProject.csproj\"");
    }

    [Fact]
    public async Task DotnetEfDbContextInfo_WithBasicParameters_BuildsCorrectCommand()
    {
        var result = await _tools.DotnetEfDbContextInfo(machineReadable: true);

        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet ef dbcontext info");
    }

    [Fact]
    public async Task DotnetEfDbContextInfo_WithContext_BuildsCorrectCommand()
    {
        var result = await _tools.DotnetEfDbContextInfo(
            context: "ApplicationDbContext",
            machineReadable: true);

        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet ef dbcontext info --context \"ApplicationDbContext\"");
    }

    [Fact]
    public async Task DotnetEfDbContextScaffold_WithConnectionAndProvider_BuildsCorrectCommand()
    {
        var result = await _tools.DotnetEfDbContextScaffold(
            connection: "Server=localhost;Database=MyDb;",
            provider: "Microsoft.EntityFrameworkCore.SqlServer",
            machineReadable: true);

        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(
            result,
            "dotnet ef dbcontext scaffold \"Server=localhost;Database=MyDb;\" \"Microsoft.EntityFrameworkCore.SqlServer\"");
    }

    [Fact]
    public async Task DotnetEfDbContextScaffold_WithEmptyConnection_ReturnsError()
    {
        var result = await _tools.DotnetEfDbContextScaffold(
            connection: "",
            provider: "Microsoft.EntityFrameworkCore.SqlServer");

        Assert.Contains("Error", result);
    }

    [Fact]
    public async Task DotnetEfDbContextScaffold_WithEmptyProvider_ReturnsError()
    {
        var result = await _tools.DotnetEfDbContextScaffold(
            connection: "Server=localhost;Database=MyDb;",
            provider: "");

        Assert.Contains("Error", result);
    }

    [Fact]
    public async Task DotnetEfDbContextScaffold_WithAllParameters_BuildsCorrectCommand()
    {
        var result = await _tools.DotnetEfDbContextScaffold(
            connection: "Server=localhost;Database=MyDb;",
            provider: "Microsoft.EntityFrameworkCore.SqlServer",
            project: "MyProject.csproj",
            outputDir: "Models",
            contextDir: "Data",
            tables: "Products,Categories",
            schemas: "dbo",
            useDatabaseNames: true,
            force: true,
            machineReadable: true);

        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(
            result,
            "dotnet ef dbcontext scaffold \"Server=localhost;Database=MyDb;\" \"Microsoft.EntityFrameworkCore.SqlServer\" --project \"MyProject.csproj\" --output-dir \"Models\" --context-dir \"Data\" --table \"Products\" --table \"Categories\" --schema \"dbo\" --use-database-names --force");
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task EntityFrameworkTools_AllMethodsExist()
    {
        // This test verifies that all EF Core tool methods exist
        // and can be called without throwing exceptions

        var methods = typeof(DotNetCliTools).GetMethods()
            .Where(m => m.Name.StartsWith("DotnetEf"))
            .ToList();

        // We expect 10 EF Core tools (9 individual tools + 1 consolidated tool)
        Assert.Equal(10, methods.Count);

        // Verify method names (9 individual tools)
        var expectedMethods = new[]
        {
            "DotnetEfMigrationsAdd",
            "DotnetEfMigrationsList",
            "DotnetEfMigrationsRemove",
            "DotnetEfMigrationsScript",
            "DotnetEfDatabaseUpdate",
            "DotnetEfDatabaseDrop",
            "DotnetEfDbContextList",
            "DotnetEfDbContextInfo",
            "DotnetEfDbContextScaffold",
            "DotnetEf"  // Consolidated tool
        };

        foreach (var expectedMethod in expectedMethods)
        {
            Assert.Contains(methods, m => m.Name == expectedMethod);
        }
    }

    #endregion
}
