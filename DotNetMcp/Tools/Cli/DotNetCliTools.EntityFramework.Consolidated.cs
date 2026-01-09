using System.Text;
using DotNetMcp.Actions;
using ModelContextProtocol.Server;

namespace DotNetMcp;

/// <summary>
/// Consolidated Entity Framework Core tool for migrations, database, and DbContext operations.
/// </summary>
public sealed partial class DotNetCliTools
{
    /// <summary>
    /// Manage Entity Framework Core operations including migrations, database updates, and DbContext scaffolding.
    /// Provides a unified interface for all EF Core CLI operations.
    /// Note: Requires dotnet-ef tool to be installed (dotnet tool install dotnet-ef --global or locally).
    /// </summary>
    /// <param name="action">The EF Core operation to perform</param>
    /// <param name="name">Migration name (required for MigrationsAdd)</param>
    /// <param name="outputDir">Output directory for migration files (MigrationsAdd) or entity classes (DbContextScaffold)</param>
    /// <param name="from">Starting migration for script generation (MigrationsScript)</param>
    /// <param name="to">Ending migration for script generation (MigrationsScript)</param>
    /// <param name="idempotent">Generate idempotent script that can be run multiple times (MigrationsScript)</param>
    /// <param name="output">Output file path for SQL script (MigrationsScript)</param>
    /// <param name="migration">Target migration name for database update (DatabaseUpdate). Use '0' to rollback all migrations.</param>
    /// <param name="connection">Database connection string (DatabaseUpdate, DbContextScaffold)</param>
    /// <param name="provider">Database provider package name (required for DbContextScaffold, e.g., 'Microsoft.EntityFrameworkCore.SqlServer')</param>
    /// <param name="contextDir">Directory for the generated DbContext class (DbContextScaffold)</param>
    /// <param name="tables">Specific tables to scaffold, comma-separated (DbContextScaffold)</param>
    /// <param name="schemas">Specific schemas to scaffold, comma-separated (DbContextScaffold)</param>
    /// <param name="useDatabaseNames">Use database names directly instead of pluralization (DbContextScaffold)</param>
    /// <param name="project">Project file containing the DbContext</param>
    /// <param name="startupProject">Startup project file (if different from DbContext project)</param>
    /// <param name="context">The DbContext class to use (if multiple contexts exist)</param>
    /// <param name="framework">Target framework for the project</param>
    /// <param name="force">Force operation: for MigrationsRemove reverts if already applied; for DatabaseDrop confirms deletion; for DbContextScaffold overwrites existing files</param>
    /// <param name="noBuild">Do not build the project before executing the command</param>
    /// <param name="dryRun">Perform a dry run without actually executing (DatabaseDrop)</param>
    /// <param name="connectionDisplay">Show connection string used (MigrationsList)</param>
    /// <param name="workingDirectory">Working directory for command execution</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "ef")]
    [McpMeta("priority", 9.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("consolidatedTool", true)]
    [McpMeta("actions", JsonValue = """["MigrationsAdd","MigrationsList","MigrationsRemove","MigrationsScript","DatabaseUpdate","DatabaseDrop","DbContextList","DbContextInfo","DbContextScaffold"]""")]
    [McpMeta("tags", JsonValue = """["ef","entity-framework","consolidated","migration","database","dbcontext"]""")]
    public async partial Task<string> DotnetEf(
        DotnetEfAction action,
        string? name = null,
        string? outputDir = null,
        string? from = null,
        string? to = null,
        bool idempotent = false,
        string? output = null,
        string? migration = null,
        string? connection = null,
        string? provider = null,
        string? contextDir = null,
        string? tables = null,
        string? schemas = null,
        bool useDatabaseNames = false,
        string? project = null,
        string? startupProject = null,
        string? context = null,
        string? framework = null,
        bool force = false,
        bool noBuild = false,
        bool dryRun = false,
        bool connectionDisplay = false,
        string? workingDirectory = null,
        bool machineReadable = false)
    {
        return await WithWorkingDirectoryAsync(workingDirectory, async () =>
        {
            // Validate action enum
            if (!ParameterValidator.ValidateAction<DotnetEfAction>(action, out var actionError))
            {
                if (machineReadable)
                {
                    var validActions = Enum.GetNames(typeof(DotnetEfAction));
                    var error = ErrorResultFactory.CreateActionValidationError(
                        action.ToString(),
                        validActions,
                        toolName: "dotnet_ef");
                    return ErrorResultFactory.ToJson(error);
                }
                return $"Error: {actionError}";
            }

            // Route to appropriate action handler
            return action switch
            {
                DotnetEfAction.MigrationsAdd => await DotnetEfMigrationsAdd(
                    name: name!,
                    project: project,
                    startupProject: startupProject,
                    context: context,
                    outputDir: outputDir,
                    framework: framework,
                    machineReadable: machineReadable),

                DotnetEfAction.MigrationsList => await DotnetEfMigrationsList(
                    project: project,
                    startupProject: startupProject,
                    context: context,
                    framework: framework,
                    connection: connectionDisplay,
                    noBuild: noBuild,
                    machineReadable: machineReadable),

                DotnetEfAction.MigrationsRemove => await DotnetEfMigrationsRemove(
                    project: project,
                    startupProject: startupProject,
                    context: context,
                    framework: framework,
                    force: force,
                    noBuild: noBuild,
                    machineReadable: machineReadable),

                DotnetEfAction.MigrationsScript => await DotnetEfMigrationsScript(
                    from: from,
                    to: to,
                    output: output,
                    project: project,
                    startupProject: startupProject,
                    context: context,
                    framework: framework,
                    idempotent: idempotent,
                    noBuild: noBuild,
                    machineReadable: machineReadable),

                DotnetEfAction.DatabaseUpdate => await DotnetEfDatabaseUpdate(
                    migration: migration,
                    project: project,
                    startupProject: startupProject,
                    context: context,
                    framework: framework,
                    connection: connection,
                    noBuild: noBuild,
                    machineReadable: machineReadable),

                DotnetEfAction.DatabaseDrop => await HandleDatabaseDropAction(
                    project: project,
                    startupProject: startupProject,
                    context: context,
                    framework: framework,
                    force: force,
                    dryRun: dryRun,
                    machineReadable: machineReadable),

                DotnetEfAction.DbContextList => await DotnetEfDbContextList(
                    project: project,
                    startupProject: startupProject,
                    framework: framework,
                    noBuild: noBuild,
                    machineReadable: machineReadable),

                DotnetEfAction.DbContextInfo => await DotnetEfDbContextInfo(
                    project: project,
                    startupProject: startupProject,
                    context: context,
                    framework: framework,
                    noBuild: noBuild,
                    machineReadable: machineReadable),

                DotnetEfAction.DbContextScaffold => await DotnetEfDbContextScaffold(
                    connection: connection!,
                    provider: provider!,
                    project: project,
                    startupProject: startupProject,
                    outputDir: outputDir,
                    contextDir: contextDir,
                    framework: framework,
                    tables: tables,
                    schemas: schemas,
                    useDatabaseNames: useDatabaseNames,
                    force: force,
                    noBuild: noBuild,
                    machineReadable: machineReadable),

                _ => machineReadable
                    ? ErrorResultFactory.ToJson(ErrorResultFactory.CreateActionValidationError(
                        action.ToString(),
                        Enum.GetNames(typeof(DotnetEfAction)),
                        toolName: "dotnet_ef"))
                    : $"Error: Unsupported action '{action}'"
            };
        });
    }

    /// <summary>
    /// Special handler for DatabaseDrop action that enforces force=true requirement for safety.
    /// </summary>
    private async Task<string> HandleDatabaseDropAction(
        string? project,
        string? startupProject,
        string? context,
        string? framework,
        bool force,
        bool dryRun,
        bool machineReadable)
    {
        // Safety check: DatabaseDrop requires force=true to prevent accidental deletions
        if (!force && !dryRun)
        {
            if (machineReadable)
            {
                var error = ErrorResultFactory.CreateValidationError(
                    "DatabaseDrop requires force=true to confirm database deletion. This is a destructive operation that permanently deletes the database.",
                    parameterName: "force",
                    reason: "required for safety");
                return ErrorResultFactory.ToJson(error);
            }
            return "Error: DatabaseDrop requires force=true to confirm database deletion. This is a destructive operation that permanently deletes the database.";
        }

        return await DotnetEfDatabaseDrop(
            project: project,
            startupProject: startupProject,
            context: context,
            framework: framework,
            force: force,
            dryRun: dryRun,
            machineReadable: machineReadable);
    }
}
