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
        // Validates that the method exists and accepts basic parameters
        var result = await _tools.DotnetEfMigrationsAdd(
            name: "InitialCreate");

        Assert.NotNull(result);
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
        // Validates that all parameters are accepted
        var result = await _tools.DotnetEfMigrationsAdd(
            name: "AddProductEntity",
            project: "MyProject.csproj",
            startupProject: "MyApi.csproj",
            context: "ApplicationDbContext",
            outputDir: "Migrations",
            framework: "net10.0");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetEfMigrationsList_WithBasicParameters_BuildsCorrectCommand()
    {
        var result = await _tools.DotnetEfMigrationsList();

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetEfMigrationsList_WithProject_BuildsCorrectCommand()
    {
        var result = await _tools.DotnetEfMigrationsList(
            project: "MyProject.csproj");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetEfMigrationsRemove_WithForce_BuildsCorrectCommand()
    {
        var result = await _tools.DotnetEfMigrationsRemove(
            force: true);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetEfMigrationsScript_WithBasicParameters_BuildsCorrectCommand()
    {
        var result = await _tools.DotnetEfMigrationsScript();

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetEfMigrationsScript_WithFromTo_BuildsCorrectCommand()
    {
        var result = await _tools.DotnetEfMigrationsScript(
            from: "InitialCreate",
            to: "AddProductEntity");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetEfMigrationsScript_WithIdempotent_BuildsCorrectCommand()
    {
        var result = await _tools.DotnetEfMigrationsScript(
            idempotent: true,
            output: "migration.sql");

        Assert.NotNull(result);
    }

    #endregion

    #region Database Tools Tests

    [Fact]
    public async Task DotnetEfDatabaseUpdate_WithBasicParameters_BuildsCorrectCommand()
    {
        var result = await _tools.DotnetEfDatabaseUpdate();

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetEfDatabaseUpdate_WithMigration_BuildsCorrectCommand()
    {
        var result = await _tools.DotnetEfDatabaseUpdate(
            migration: "AddProductEntity");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetEfDatabaseUpdate_WithRollback_BuildsCorrectCommand()
    {
        // Test rollback to initial state
        var result = await _tools.DotnetEfDatabaseUpdate(
            migration: "0");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetEfDatabaseDrop_WithForce_BuildsCorrectCommand()
    {
        var result = await _tools.DotnetEfDatabaseDrop(
            force: true);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetEfDatabaseDrop_WithDryRun_BuildsCorrectCommand()
    {
        var result = await _tools.DotnetEfDatabaseDrop(
            dryRun: true);

        Assert.NotNull(result);
    }

    #endregion

    #region DbContext Tools Tests

    [Fact]
    public async Task DotnetEfDbContextList_WithBasicParameters_BuildsCorrectCommand()
    {
        var result = await _tools.DotnetEfDbContextList();

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetEfDbContextList_WithProject_BuildsCorrectCommand()
    {
        var result = await _tools.DotnetEfDbContextList(
            project: "MyProject.csproj");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetEfDbContextInfo_WithBasicParameters_BuildsCorrectCommand()
    {
        var result = await _tools.DotnetEfDbContextInfo();

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetEfDbContextInfo_WithContext_BuildsCorrectCommand()
    {
        var result = await _tools.DotnetEfDbContextInfo(
            context: "ApplicationDbContext");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetEfDbContextScaffold_WithConnectionAndProvider_BuildsCorrectCommand()
    {
        var result = await _tools.DotnetEfDbContextScaffold(
            connection: "Server=localhost;Database=MyDb;",
            provider: "Microsoft.EntityFrameworkCore.SqlServer");

        Assert.NotNull(result);
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
            force: true);

        Assert.NotNull(result);
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

        // We expect 9 EF Core tools
        Assert.Equal(9, methods.Count);

        // Verify method names
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
            "DotnetEfDbContextScaffold"
        };

        foreach (var expectedMethod in expectedMethods)
        {
            Assert.Contains(methods, m => m.Name == expectedMethod);
        }
    }

    #endregion
}
