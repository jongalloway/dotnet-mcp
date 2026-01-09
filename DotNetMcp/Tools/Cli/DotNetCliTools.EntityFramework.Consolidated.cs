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

    // ===== EF helper methods (moved from DotNetCliTools.EntityFramework.cs) =====
    /// <summary>
    /// Create a new Entity Framework Core migration.
    /// Generates migration files for database schema changes. Requires Microsoft.EntityFrameworkCore.Design package and dotnet-ef tool.
    /// </summary>
    /// <param name="name">Name of the migration (e.g., 'InitialCreate', 'AddProductEntity')</param>
    /// <param name="project">Project file containing the DbContext</param>
    /// <param name="startupProject">Startup project file (if different from DbContext project)</param>
    /// <param name="context">The DbContext class to use (if multiple contexts exist)</param>
    /// <param name="outputDir">Output directory for migration files</param>
    /// <param name="framework">Target framework for the project</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpMeta("category", "ef")]
    [McpMeta("priority", 9.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("tags", JsonValue = """["ef","entity-framework","migration","database","schema"]""")]
    public async Task<string> DotnetEfMigrationsAdd(
        string name,
        string? project = null,
        string? startupProject = null,
        string? context = null,
        string? outputDir = null,
        string? framework = null,
        bool machineReadable = false)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            if (machineReadable)
            {
                var error = ErrorResultFactory.CreateValidationError(
                    "name parameter is required.",
                    parameterName: "name",
                    reason: "required");
                return ErrorResultFactory.ToJson(error);
            }
            return "Error: name parameter is required.";
        }

        var args = new StringBuilder($"ef migrations add \"{name}\"");
        if (!string.IsNullOrEmpty(project)) args.Append($" --project \"{project}\"");
        if (!string.IsNullOrEmpty(startupProject)) args.Append($" --startup-project \"{startupProject}\"");
        if (!string.IsNullOrEmpty(context)) args.Append($" --context \"{context}\"");
        if (!string.IsNullOrEmpty(outputDir)) args.Append($" --output-dir \"{outputDir}\"");
        if (!string.IsNullOrEmpty(framework)) args.Append($" --framework {framework}");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    /// <summary>
    /// List all Entity Framework Core migrations.
    /// Shows applied and pending migrations with their status. Useful for understanding migration history.
    /// </summary>
    /// <param name="project">Project file containing the DbContext</param>
    /// <param name="startupProject">Startup project file (if different from DbContext project)</param>
    /// <param name="context">The DbContext class to use (if multiple contexts exist)</param>
    /// <param name="framework">Target framework for the project</param>
    /// <param name="connection">Show connection string used</param>
    /// <param name="noBuild">Do not build the project before listing</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpMeta("category", "ef")]
    [McpMeta("priority", 8.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("tags", JsonValue = """["ef","entity-framework","migration","database","list"]""")]
    public async Task<string> DotnetEfMigrationsList(
        string? project = null,
        string? startupProject = null,
        string? context = null,
        string? framework = null,
        bool connection = false,
        bool noBuild = false,
        bool machineReadable = false)
    {
        var args = new StringBuilder("ef migrations list");
        if (!string.IsNullOrEmpty(project)) args.Append($" --project \"{project}\"");
        if (!string.IsNullOrEmpty(startupProject)) args.Append($" --startup-project \"{startupProject}\"");
        if (!string.IsNullOrEmpty(context)) args.Append($" --context \"{context}\"");
        if (!string.IsNullOrEmpty(framework)) args.Append($" --framework {framework}");
        if (connection) args.Append(" --connection");
        if (noBuild) args.Append(" --no-build");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    /// <summary>
    /// Remove the last Entity Framework Core migration.
    /// Removes the most recent unapplied migration. Useful for cleaning up mistakes before applying to database.
    /// </summary>
    /// <param name="project">Project file containing the DbContext</param>
    /// <param name="startupProject">Startup project file (if different from DbContext project)</param>
    /// <param name="context">The DbContext class to use (if multiple contexts exist)</param>
    /// <param name="framework">Target framework for the project</param>
    /// <param name="force">Force removal (reverts migration if already applied)</param>
    /// <param name="noBuild">Do not build the project before removing</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpMeta("category", "ef")]
    [McpMeta("priority", 7.0)]
    [McpMeta("tags", JsonValue = """["ef","entity-framework","migration","database","remove"]""")]
    public async Task<string> DotnetEfMigrationsRemove(
        string? project = null,
        string? startupProject = null,
        string? context = null,
        string? framework = null,
        bool force = false,
        bool noBuild = false,
        bool machineReadable = false)
    {
        var args = new StringBuilder("ef migrations remove");
        if (!string.IsNullOrEmpty(project)) args.Append($" --project \"{project}\"");
        if (!string.IsNullOrEmpty(startupProject)) args.Append($" --startup-project \"{startupProject}\"");
        if (!string.IsNullOrEmpty(context)) args.Append($" --context \"{context}\"");
        if (!string.IsNullOrEmpty(framework)) args.Append($" --framework {framework}");
        if (force) args.Append(" --force");
        if (noBuild) args.Append(" --no-build");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    /// <summary>
    /// Generate SQL script from Entity Framework Core migrations.
    /// Exports migration changes to SQL file for deployment or review. Useful for production deployments.
    /// </summary>
    /// <param name="from">Starting migration (default: 0 for all migrations)</param>
    /// <param name="to">Target migration (default: last migration)</param>
    /// <param name="output">Output file path for SQL script</param>
    /// <param name="project">Project file containing the DbContext</param>
    /// <param name="startupProject">Startup project file (if different from DbContext project)</param>
    /// <param name="context">The DbContext class to use (if multiple contexts exist)</param>
    /// <param name="framework">Target framework for the project</param>
    /// <param name="idempotent">Generate idempotent script (can be run multiple times)</param>
    /// <param name="noBuild">Do not build the project before scripting</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpMeta("category", "ef")]
    [McpMeta("priority", 7.0)]
    [McpMeta("tags", JsonValue = """["ef","entity-framework","migration","database","sql","script"]""")]
    public async Task<string> DotnetEfMigrationsScript(
        string? from = null,
        string? to = null,
        string? output = null,
        string? project = null,
        string? startupProject = null,
        string? context = null,
        string? framework = null,
        bool idempotent = false,
        bool noBuild = false,
        bool machineReadable = false)
    {
        // Validate parameter order: if 'to' is specified without 'from', use empty string for from
        var args = new StringBuilder("ef migrations script");
        if (!string.IsNullOrEmpty(from))
        {
            args.Append($" \"{from}\"");
            if (!string.IsNullOrEmpty(to))
                args.Append($" \"{to}\"");
        }
        else if (!string.IsNullOrEmpty(to))
        {
            // If only 'to' is specified, EF Core expects 'from' to be empty string (all migrations up to 'to')
            args.Append($" \"\" \"{to}\"");
        }

        if (!string.IsNullOrEmpty(output)) args.Append($" --output \"{output}\"");
        if (!string.IsNullOrEmpty(project)) args.Append($" --project \"{project}\"");
        if (!string.IsNullOrEmpty(startupProject)) args.Append($" --startup-project \"{startupProject}\"");
        if (!string.IsNullOrEmpty(context)) args.Append($" --context \"{context}\"");
        if (!string.IsNullOrEmpty(framework)) args.Append($" --framework {framework}");
        if (idempotent) args.Append(" --idempotent");
        if (noBuild) args.Append(" --no-build");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    /// <summary>
    /// Apply Entity Framework Core migrations to the database.
    /// Updates database schema to the latest or specified migration. Essential for database updates.
    /// </summary>
    /// <param name="migration">Target migration name (default: latest migration). Use '0' to rollback all migrations.</param>
    /// <param name="project">Project file containing the DbContext</param>
    /// <param name="startupProject">Startup project file (if different from DbContext project)</param>
    /// <param name="context">The DbContext class to use (if multiple contexts exist)</param>
    /// <param name="framework">Target framework for the project</param>
    /// <param name="connection">Connection string (overrides configured connection)</param>
    /// <param name="noBuild">Do not build the project before updating</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpMeta("category", "ef")]
    [McpMeta("priority", 9.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("tags", JsonValue = """["ef","entity-framework","database","update","migration","apply"]""")]
    public async Task<string> DotnetEfDatabaseUpdate(
        string? migration = null,
        string? project = null,
        string? startupProject = null,
        string? context = null,
        string? framework = null,
        string? connection = null,
        bool noBuild = false,
        bool machineReadable = false)
    {
        var args = new StringBuilder("ef database update");
        if (!string.IsNullOrEmpty(migration)) args.Append($" \"{migration}\"");
        if (!string.IsNullOrEmpty(project)) args.Append($" --project \"{project}\"");
        if (!string.IsNullOrEmpty(startupProject)) args.Append($" --startup-project \"{startupProject}\"");
        if (!string.IsNullOrEmpty(context)) args.Append($" --context \"{context}\"");
        if (!string.IsNullOrEmpty(framework)) args.Append($" --framework {framework}");
        if (!string.IsNullOrEmpty(connection)) args.Append($" --connection \"{connection}\"");
        if (noBuild) args.Append(" --no-build");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    /// <summary>
    /// Drop the Entity Framework Core database.
    /// WARNING: This permanently deletes the database. Use with extreme caution (typically only for development).
    /// Set force=true to execute without confirmation prompt.
    /// </summary>
    /// <param name="project">Project file containing the DbContext</param>
    /// <param name="startupProject">Startup project file (if different from DbContext project)</param>
    /// <param name="context">The DbContext class to use (if multiple contexts exist)</param>
    /// <param name="framework">Target framework for the project</param>
    /// <param name="force">Force drop without confirmation prompt (set to true to execute)</param>
    /// <param name="dryRun">Perform a dry run without actually dropping</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpMeta("category", "ef")]
    [McpMeta("priority", 5.0)]
    [McpMeta("tags", JsonValue = """["ef","entity-framework","database","drop","delete"]""")]
    public async Task<string> DotnetEfDatabaseDrop(
        string? project = null,
        string? startupProject = null,
        string? context = null,
        string? framework = null,
        bool force = false,
        bool dryRun = false,
        bool machineReadable = false)
    {
        var args = new StringBuilder("ef database drop");
        if (!string.IsNullOrEmpty(project)) args.Append($" --project \"{project}\"");
        if (!string.IsNullOrEmpty(startupProject)) args.Append($" --startup-project \"{startupProject}\"");
        if (!string.IsNullOrEmpty(context)) args.Append($" --context \"{context}\"");
        if (!string.IsNullOrEmpty(framework)) args.Append($" --framework {framework}");
        if (force) args.Append(" --force");
        if (dryRun) args.Append(" --dry-run");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    /// <summary>
    /// List all Entity Framework Core DbContext classes in the project.
    /// Shows available database contexts. Useful for multi-context applications.
    /// </summary>
    /// <param name="project">Project file containing the DbContext classes</param>
    /// <param name="startupProject">Startup project file (if different from DbContext project)</param>
    /// <param name="framework">Target framework for the project</param>
    /// <param name="noBuild">Do not build the project before listing</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpMeta("category", "ef")]
    [McpMeta("priority", 7.0)]
    [McpMeta("tags", JsonValue = """["ef","entity-framework","dbcontext","list"]""")]
    public async Task<string> DotnetEfDbContextList(
        string? project = null,
        string? startupProject = null,
        string? framework = null,
        bool noBuild = false,
        bool machineReadable = false)
    {
        var args = new StringBuilder("ef dbcontext list");
        if (!string.IsNullOrEmpty(project)) args.Append($" --project \"{project}\"");
        if (!string.IsNullOrEmpty(startupProject)) args.Append($" --startup-project \"{startupProject}\"");
        if (!string.IsNullOrEmpty(framework)) args.Append($" --framework {framework}");
        if (noBuild) args.Append(" --no-build");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    /// <summary>
    /// Get Entity Framework Core DbContext information.
    /// Shows connection string and provider details for a specific DbContext.
    /// </summary>
    /// <param name="project">Project file containing the DbContext</param>
    /// <param name="startupProject">Startup project file (if different from DbContext project)</param>
    /// <param name="context">The DbContext class to use (if multiple contexts exist)</param>
    /// <param name="framework">Target framework for the project</param>
    /// <param name="noBuild">Do not build the project before getting info</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpMeta("category", "ef")]
    [McpMeta("priority", 7.0)]
    [McpMeta("tags", JsonValue = """["ef","entity-framework","dbcontext","info","connection-string"]""")]
    public async Task<string> DotnetEfDbContextInfo(
        string? project = null,
        string? startupProject = null,
        string? context = null,
        string? framework = null,
        bool noBuild = false,
        bool machineReadable = false)
    {
        var args = new StringBuilder("ef dbcontext info");
        if (!string.IsNullOrEmpty(project)) args.Append($" --project \"{project}\"");
        if (!string.IsNullOrEmpty(startupProject)) args.Append($" --startup-project \"{startupProject}\"");
        if (!string.IsNullOrEmpty(context)) args.Append($" --context \"{context}\"");
        if (!string.IsNullOrEmpty(framework)) args.Append($" --framework {framework}");
        if (noBuild) args.Append(" --no-build");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    /// <summary>
    /// Reverse engineer (scaffold) Entity Framework Core entities from an existing database.
    /// Generates DbContext and entity classes from database schema. Essential for database-first development.
    /// </summary>
    /// <param name="connection">Database connection string (e.g., 'Server=localhost;Database=MyDb;...')</param>
    /// <param name="provider">Database provider (e.g., 'Microsoft.EntityFrameworkCore.SqlServer', 'Npgsql.EntityFrameworkCore.PostgreSQL')</param>
    /// <param name="project">Project file to add generated files to</param>
    /// <param name="startupProject">Startup project file (if different from DbContext project)</param>
    /// <param name="outputDir">Output directory for generated entity classes (default: project root)</param>
    /// <param name="contextDir">Directory for the generated DbContext class (default: same as outputDir)</param>
    /// <param name="framework">Target framework for the project</param>
    /// <param name="tables">Specific tables to scaffold (comma-separated; default: all tables)</param>
    /// <param name="schemas">Specific schemas to scaffold (comma-separated)</param>
    /// <param name="useDatabaseNames">Use database names directly instead of pluralization</param>
    /// <param name="force">Force overwrite of existing files</param>
    /// <param name="noBuild">Do not build the project before scaffolding</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpMeta("category", "ef")]
    [McpMeta("priority", 8.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("tags", JsonValue = """["ef","entity-framework","dbcontext","scaffold","reverse-engineer","database-first"]""")]
    public async Task<string> DotnetEfDbContextScaffold(
        string connection,
        string provider,
        string? project = null,
        string? startupProject = null,
        string? outputDir = null,
        string? contextDir = null,
        string? framework = null,
        string? tables = null,
        string? schemas = null,
        bool useDatabaseNames = false,
        bool force = false,
        bool noBuild = false,
        bool machineReadable = false)
    {
        if (string.IsNullOrWhiteSpace(connection))
        {
            if (machineReadable)
            {
                var error = ErrorResultFactory.CreateValidationError(
                    "connection parameter is required.",
                    parameterName: "connection",
                    reason: "required");
                return ErrorResultFactory.ToJson(error);
            }
            return "Error: connection parameter is required.";
        }

        if (string.IsNullOrWhiteSpace(provider))
        {
            if (machineReadable)
            {
                var error = ErrorResultFactory.CreateValidationError(
                    "provider parameter is required.",
                    parameterName: "provider",
                    reason: "required");
                return ErrorResultFactory.ToJson(error);
            }
            return "Error: provider parameter is required.";
        }

        var args = new StringBuilder($"ef dbcontext scaffold \"{connection}\" \"{provider}\"");
        if (!string.IsNullOrEmpty(project)) args.Append($" --project \"{project}\"");
        if (!string.IsNullOrEmpty(startupProject)) args.Append($" --startup-project \"{startupProject}\"");
        if (!string.IsNullOrEmpty(outputDir)) args.Append($" --output-dir \"{outputDir}\"");
        if (!string.IsNullOrEmpty(contextDir)) args.Append($" --context-dir \"{contextDir}\"");
        if (!string.IsNullOrEmpty(framework)) args.Append($" --framework {framework}");

        // Handle multiple tables - split by comma and add --table for each
        if (!string.IsNullOrEmpty(tables))
        {
            foreach (var table in tables.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                args.Append($" --table \"{table}\"");
            }
        }

        // Handle multiple schemas - split by comma and add --schema for each
        if (!string.IsNullOrEmpty(schemas))
        {
            foreach (var schema in schemas.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                args.Append($" --schema \"{schema}\"");
            }
        }

        if (useDatabaseNames) args.Append(" --use-database-names");
        if (force) args.Append(" --force");
        if (noBuild) args.Append(" --no-build");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }
}
