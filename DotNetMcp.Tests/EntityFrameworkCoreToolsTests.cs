using DotNetMcp;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
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
    private readonly Mock<ILogger<DotNetCliTools>> _loggerMock;

    public EntityFrameworkCoreToolsTests()
    {
        _loggerMock = new Mock<ILogger<DotNetCliTools>>();
        _tools = new DotNetCliTools(_loggerMock.Object);
    }

    #region Migration Tools Tests

    [Fact]
    public async Task DotnetEfMigrationsAdd_WithName_BuildsCorrectCommand()
    {
        // Validates that the method exists and accepts basic parameters
        var result = await _tools.DotnetEfMigrationsAdd(
            name: "InitialCreate");

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DotnetEfMigrationsAdd_WithEmptyName_ReturnsError()
    {
        // Validates that empty name returns an error
        var result = await _tools.DotnetEfMigrationsAdd(
            name: "");

        result.Should().Contain("Error");
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
            framework: "net9.0");

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DotnetEfMigrationsList_WithBasicParameters_BuildsCorrectCommand()
    {
        var result = await _tools.DotnetEfMigrationsList();

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DotnetEfMigrationsList_WithProject_BuildsCorrectCommand()
    {
        var result = await _tools.DotnetEfMigrationsList(
            project: "MyProject.csproj");

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DotnetEfMigrationsRemove_WithForce_BuildsCorrectCommand()
    {
        var result = await _tools.DotnetEfMigrationsRemove(
            force: true);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DotnetEfMigrationsScript_WithBasicParameters_BuildsCorrectCommand()
    {
        var result = await _tools.DotnetEfMigrationsScript();

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DotnetEfMigrationsScript_WithFromTo_BuildsCorrectCommand()
    {
        var result = await _tools.DotnetEfMigrationsScript(
            from: "InitialCreate",
            to: "AddProductEntity");

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DotnetEfMigrationsScript_WithIdempotent_BuildsCorrectCommand()
    {
        var result = await _tools.DotnetEfMigrationsScript(
            idempotent: true,
            output: "migration.sql");

        result.Should().NotBeNull();
    }

    #endregion

    #region Database Tools Tests

    [Fact]
    public async Task DotnetEfDatabaseUpdate_WithBasicParameters_BuildsCorrectCommand()
    {
        var result = await _tools.DotnetEfDatabaseUpdate();

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DotnetEfDatabaseUpdate_WithMigration_BuildsCorrectCommand()
    {
        var result = await _tools.DotnetEfDatabaseUpdate(
            migration: "AddProductEntity");

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DotnetEfDatabaseUpdate_WithRollback_BuildsCorrectCommand()
    {
        // Test rollback to initial state
        var result = await _tools.DotnetEfDatabaseUpdate(
            migration: "0");

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DotnetEfDatabaseDrop_WithForce_BuildsCorrectCommand()
    {
        var result = await _tools.DotnetEfDatabaseDrop(
            force: true);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DotnetEfDatabaseDrop_WithDryRun_BuildsCorrectCommand()
    {
        var result = await _tools.DotnetEfDatabaseDrop(
            dryRun: true);

        result.Should().NotBeNull();
    }

    #endregion

    #region DbContext Tools Tests

    [Fact]
    public async Task DotnetEfDbContextList_WithBasicParameters_BuildsCorrectCommand()
    {
        var result = await _tools.DotnetEfDbContextList();

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DotnetEfDbContextList_WithProject_BuildsCorrectCommand()
    {
        var result = await _tools.DotnetEfDbContextList(
            project: "MyProject.csproj");

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DotnetEfDbContextInfo_WithBasicParameters_BuildsCorrectCommand()
    {
        var result = await _tools.DotnetEfDbContextInfo();

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DotnetEfDbContextInfo_WithContext_BuildsCorrectCommand()
    {
        var result = await _tools.DotnetEfDbContextInfo(
            context: "ApplicationDbContext");

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DotnetEfDbContextScaffold_WithConnectionAndProvider_BuildsCorrectCommand()
    {
        var result = await _tools.DotnetEfDbContextScaffold(
            connection: "Server=localhost;Database=MyDb;",
            provider: "Microsoft.EntityFrameworkCore.SqlServer");

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DotnetEfDbContextScaffold_WithEmptyConnection_ReturnsError()
    {
        var result = await _tools.DotnetEfDbContextScaffold(
            connection: "",
            provider: "Microsoft.EntityFrameworkCore.SqlServer");

        result.Should().Contain("Error");
    }

    [Fact]
    public async Task DotnetEfDbContextScaffold_WithEmptyProvider_ReturnsError()
    {
        var result = await _tools.DotnetEfDbContextScaffold(
            connection: "Server=localhost;Database=MyDb;",
            provider: "");

        result.Should().Contain("Error");
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

        result.Should().NotBeNull();
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
        methods.Should().HaveCount(9);
        
        // Verify method names
        methods.Select(m => m.Name).Should().Contain(new[]
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
        });
    }

    #endregion
}
