using DotNetMcp;
using DotNetMcp.Actions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotNetMcp.Tests.Tools;

/// <summary>
/// Tests for the consolidated dotnet_ef tool.
/// Validates action routing, parameter handling, and safety requirements.
/// </summary>
public class EntityFrameworkConsolidatedToolTests
{
    private readonly DotNetCliTools _tools;
    private readonly ILogger<DotNetCliTools> _logger;
    private readonly ConcurrencyManager _concurrencyManager;

    public EntityFrameworkConsolidatedToolTests()
    {
        _logger = NullLogger<DotNetCliTools>.Instance;
        _concurrencyManager = new ConcurrencyManager();
        _tools = new DotNetCliTools(_logger, _concurrencyManager, new ProcessSessionManager());
    }

    #region MigrationsAdd Action Tests

    [Fact]
    public async Task DotnetEf_MigrationsAdd_WithName_ExecutesCommand()
    {
        var result = (await _tools.DotnetEf(
            action: DotnetEfAction.MigrationsAdd,
            name: "InitialCreate")).GetText();

        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet ef migrations add \"InitialCreate\"");
    }

    [Fact]
    public async Task DotnetEf_MigrationsAdd_WithoutName_ReturnsError()
    {
        var result = (await _tools.DotnetEf(
            action: DotnetEfAction.MigrationsAdd)).GetText();

        Assert.Contains("Error", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("name", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetEf_MigrationsAdd_WithAllParameters_ExecutesCommand()
    {
        var result = (await _tools.DotnetEf(
            action: DotnetEfAction.MigrationsAdd,
            name: "AddProductEntity",
            project: "MyProject.csproj",
            startupProject: "MyApi.csproj",
            context: "ApplicationDbContext",
            outputDir: "Migrations",
            framework: "net10.0")).GetText();

        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(
            result,
            "dotnet ef migrations add \"AddProductEntity\" --project \"MyProject.csproj\" --startup-project \"MyApi.csproj\" --context \"ApplicationDbContext\" --output-dir \"Migrations\" --framework net10.0");
    }

    #endregion

    #region MigrationsList Action Tests

    [Fact]
    public async Task DotnetEf_MigrationsList_WithBasicParameters_ExecutesCommand()
    {
        var result = (await _tools.DotnetEf(
            action: DotnetEfAction.MigrationsList)).GetText();

        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet ef migrations list");
    }

    [Fact]
    public async Task DotnetEf_MigrationsList_WithProject_ExecutesCommand()
    {
        var result = (await _tools.DotnetEf(
            action: DotnetEfAction.MigrationsList,
            project: "MyProject.csproj")).GetText();

        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet ef migrations list --project \"MyProject.csproj\"");
    }

    [Fact]
    public async Task DotnetEf_MigrationsList_WithConnectionDisplay_ExecutesCommand()
    {
        var result = (await _tools.DotnetEf(
            action: DotnetEfAction.MigrationsList,
            connectionDisplay: true)).GetText();

        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet ef migrations list --connection");
    }

    #endregion

    #region MigrationsRemove Action Tests

    [Fact]
    public async Task DotnetEf_MigrationsRemove_WithBasicParameters_ExecutesCommand()
    {
        var result = (await _tools.DotnetEf(
            action: DotnetEfAction.MigrationsRemove)).GetText();

        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet ef migrations remove");
    }

    [Fact]
    public async Task DotnetEf_MigrationsRemove_WithForce_ExecutesCommand()
    {
        var result = (await _tools.DotnetEf(
            action: DotnetEfAction.MigrationsRemove,
            force: true)).GetText();

        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet ef migrations remove --force");
    }

    #endregion

    #region MigrationsScript Action Tests

    [Fact]
    public async Task DotnetEf_MigrationsScript_WithBasicParameters_ExecutesCommand()
    {
        var result = (await _tools.DotnetEf(
            action: DotnetEfAction.MigrationsScript)).GetText();

        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet ef migrations script");
    }

    [Fact]
    public async Task DotnetEf_MigrationsScript_WithFromTo_ExecutesCommand()
    {
        var result = (await _tools.DotnetEf(
            action: DotnetEfAction.MigrationsScript,
            from: "InitialCreate",
            to: "AddProductEntity")).GetText();

        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet ef migrations script \"InitialCreate\" \"AddProductEntity\"");
    }

    [Fact]
    public async Task DotnetEf_MigrationsScript_WithIdempotent_ExecutesCommand()
    {
        var result = (await _tools.DotnetEf(
            action: DotnetEfAction.MigrationsScript,
            idempotent: true,
            output: "migration.sql")).GetText();

        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet ef migrations script --output \"migration.sql\" --idempotent");
    }

    #endregion

    #region DatabaseUpdate Action Tests

    [Fact]
    public async Task DotnetEf_DatabaseUpdate_WithBasicParameters_ExecutesCommand()
    {
        var result = (await _tools.DotnetEf(
            action: DotnetEfAction.DatabaseUpdate)).GetText();

        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet ef database update");
    }

    [Fact]
    public async Task DotnetEf_DatabaseUpdate_WithMigration_ExecutesCommand()
    {
        var result = (await _tools.DotnetEf(
            action: DotnetEfAction.DatabaseUpdate,
            migration: "AddProductEntity")).GetText();

        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet ef database update \"AddProductEntity\"");
    }

    [Fact]
    public async Task DotnetEf_DatabaseUpdate_WithConnection_ExecutesCommand()
    {
        var result = (await _tools.DotnetEf(
            action: DotnetEfAction.DatabaseUpdate,
            connection: "Server=localhost;Database=MyDb")).GetText();

        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet ef database update --connection \"Server=localhost;Database=MyDb\"");
    }

    #endregion

    #region DatabaseDrop Action Tests (Safety Tests)

    [Fact]
    public async Task DotnetEf_DatabaseDrop_WithoutForce_ReturnsError()
    {
        // DatabaseDrop requires force=true for safety
        var result = (await _tools.DotnetEf(
            action: DotnetEfAction.DatabaseDrop)).GetText();

        Assert.Contains("Error", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("force", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetEf_DatabaseDrop_WithForceTrue_ExecutesCommand()
    {
        var result = (await _tools.DotnetEf(
            action: DotnetEfAction.DatabaseDrop,
            force: true)).GetText();

        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet ef database drop --force");
    }

    [Fact]
    public async Task DotnetEf_DatabaseDrop_WithDryRun_ExecutesCommand()
    {
        // Dry run should work without force=true
        var result = (await _tools.DotnetEf(
            action: DotnetEfAction.DatabaseDrop,
            dryRun: true)).GetText();

        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet ef database drop --dry-run");
    }

    [Fact]
    public async Task DotnetEf_DatabaseDrop_WithForceAndDryRun_ExecutesCommand()
    {
        var result = (await _tools.DotnetEf(
            action: DotnetEfAction.DatabaseDrop,
            force: true,
            dryRun: true)).GetText();

        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet ef database drop --force --dry-run");
    }

    [Fact]
    public async Task DotnetEf_DatabaseDrop_WithoutForce_PlainText_ReturnsError()
    {
        // Test plain text error message
        var result = (await _tools.DotnetEf(
            action: DotnetEfAction.DatabaseDrop)).GetText();

        Assert.Contains("Error", result);
        Assert.Contains("force=true", result);
        Assert.Contains("destructive", result, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region DbContextList Action Tests

    [Fact]
    public async Task DotnetEf_DbContextList_WithBasicParameters_ExecutesCommand()
    {
        var result = (await _tools.DotnetEf(
            action: DotnetEfAction.DbContextList)).GetText();

        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet ef dbcontext list");
    }

    [Fact]
    public async Task DotnetEf_DbContextList_WithProject_ExecutesCommand()
    {
        var result = (await _tools.DotnetEf(
            action: DotnetEfAction.DbContextList,
            project: "MyProject.csproj")).GetText();

        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet ef dbcontext list --project \"MyProject.csproj\"");
    }

    #endregion

    #region DbContextInfo Action Tests

    [Fact]
    public async Task DotnetEf_DbContextInfo_WithBasicParameters_ExecutesCommand()
    {
        var result = (await _tools.DotnetEf(
            action: DotnetEfAction.DbContextInfo)).GetText();

        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet ef dbcontext info");
    }

    [Fact]
    public async Task DotnetEf_DbContextInfo_WithContext_ExecutesCommand()
    {
        var result = (await _tools.DotnetEf(
            action: DotnetEfAction.DbContextInfo,
            context: "ApplicationDbContext")).GetText();

        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet ef dbcontext info --context \"ApplicationDbContext\"");
    }

    #endregion

    #region DbContextScaffold Action Tests

    [Fact]
    public async Task DotnetEf_DbContextScaffold_WithRequiredParameters_ExecutesCommand()
    {
        var result = (await _tools.DotnetEf(
            action: DotnetEfAction.DbContextScaffold,
            connection: "Server=localhost;Database=MyDb",
            provider: "Microsoft.EntityFrameworkCore.SqlServer")).GetText();

        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(
            result,
            "dotnet ef dbcontext scaffold \"Server=localhost;Database=MyDb\" \"Microsoft.EntityFrameworkCore.SqlServer\"");
    }

    [Fact]
    public async Task DotnetEf_DbContextScaffold_WithoutConnection_ReturnsError()
    {
        var result = (await _tools.DotnetEf(
            action: DotnetEfAction.DbContextScaffold,
            provider: "Microsoft.EntityFrameworkCore.SqlServer")).GetText();

        Assert.Contains("Error", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("connection", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetEf_DbContextScaffold_WithoutProvider_ReturnsError()
    {
        var result = (await _tools.DotnetEf(
            action: DotnetEfAction.DbContextScaffold,
            connection: "Server=localhost;Database=MyDb")).GetText();

        Assert.Contains("Error", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("provider", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetEf_DbContextScaffold_WithAllParameters_ExecutesCommand()
    {
        var result = (await _tools.DotnetEf(
            action: DotnetEfAction.DbContextScaffold,
            connection: "Server=localhost;Database=MyDb",
            provider: "Microsoft.EntityFrameworkCore.SqlServer",
            project: "MyProject.csproj",
            outputDir: "Models",
            contextDir: "Data",
            tables: "Products,Orders",
            schemas: "dbo,sales",
            useDatabaseNames: true,
            force: true)).GetText();

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    #endregion

    #region Action Routing Tests

    [Fact]
    public async Task DotnetEf_AllActions_RouteCorrectly()
    {
        // Test that all enum values route to correct handlers
        var actions = Enum.GetValues<DotnetEfAction>();
        
        foreach (var action in actions)
        {
            string result;
            
            // Provide required parameters for actions that need them
            switch (action)
            {
                case DotnetEfAction.MigrationsAdd:
                    result = (await _tools.DotnetEf(action, name: "TestMigration")).GetText();
                    break;
                case DotnetEfAction.DatabaseDrop:
                    result = (await _tools.DotnetEf(action, force: true)).GetText();
                    break;
                case DotnetEfAction.DbContextScaffold:
                    result = (await _tools.DotnetEf(
                        action, 
                        connection: "TestConnection", 
                        provider: "TestProvider")).GetText();
                    break;
                default:
                    result = (await _tools.DotnetEf(action)).GetText();
                    break;
            }
            
            // Verify we got a valid response (not an "unsupported action" error)
            Assert.DoesNotContain("Unsupported action", result);
        }
    }

    #endregion

    #region Common Parameter Tests

    [Fact]
    public async Task DotnetEf_WithCommonParameters_ExecutesCorrectly()
    {
        // Test that common parameters work across different actions
        var result = (await _tools.DotnetEf(
            action: DotnetEfAction.MigrationsList,
            project: "MyProject.csproj",
            startupProject: "MyApi.csproj",
            context: "ApplicationDbContext",
            framework: "net10.0",
            noBuild: true)).GetText();

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    #endregion
}
