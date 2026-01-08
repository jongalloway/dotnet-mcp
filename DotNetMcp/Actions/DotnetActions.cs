namespace DotNetMcp.Actions;

/// <summary>
/// Actions for the consolidated dotnet_project tool.
/// Manages .NET project lifecycle including creation, building, testing, running, and publishing.
/// </summary>
public enum DotnetProjectAction
{
    /// <summary>Create a new project from a template</summary>
    New,
    
    /// <summary>Restore project dependencies</summary>
    Restore,
    
    /// <summary>Build the project</summary>
    Build,
    
    /// <summary>Build and run the project</summary>
    Run,
    
    /// <summary>Run unit tests</summary>
    Test,
    
    /// <summary>Publish the project for deployment</summary>
    Publish,
    
    /// <summary>Clean build outputs</summary>
    Clean,
    
    /// <summary>Analyze project file for metadata</summary>
    Analyze,
    
    /// <summary>Show dependency graph</summary>
    Dependencies,
    
    /// <summary>Validate project health</summary>
    Validate,
    
    /// <summary>Create a NuGet package from the project</summary>
    Pack,
    
    /// <summary>Run with file watching and hot reload</summary>
    Watch,
    
    /// <summary>Format code according to .editorconfig</summary>
    Format
}

/// <summary>
/// Actions for the consolidated dotnet_package tool.
/// Manages NuGet packages and project references.
/// </summary>
public enum DotnetPackageAction
{
    /// <summary>Add a NuGet package to the project</summary>
    Add,
    
    /// <summary>Remove a NuGet package from the project</summary>
    Remove,
    
    /// <summary>Search NuGet.org for packages</summary>
    Search,
    
    /// <summary>Update packages to newer versions</summary>
    Update,
    
    /// <summary>List package references</summary>
    List,
    
    /// <summary>Add project-to-project reference</summary>
    AddReference,
    
    /// <summary>Remove project-to-project reference</summary>
    RemoveReference,
    
    /// <summary>List project references</summary>
    ListReferences,
    
    /// <summary>Clear NuGet local caches</summary>
    ClearCache
}

/// <summary>
/// Actions for the consolidated dotnet_solution tool.
/// Manages solution files and their contents.
/// </summary>
public enum DotnetSolutionAction
{
    /// <summary>Create a new solution file</summary>
    Create,
    
    /// <summary>Add a project to the solution</summary>
    Add,
    
    /// <summary>List projects in the solution</summary>
    List,
    
    /// <summary>Remove a project from the solution</summary>
    Remove
}

/// <summary>
/// Actions for the consolidated dotnet_ef tool.
/// Manages Entity Framework Core operations including migrations, database, and DbContext.
/// </summary>
public enum DotnetEfAction
{
    /// <summary>Add a new migration</summary>
    MigrationsAdd,
    
    /// <summary>List available migrations</summary>
    MigrationsList,
    
    /// <summary>Remove the last migration</summary>
    MigrationsRemove,
    
    /// <summary>Generate SQL script from migrations</summary>
    MigrationsScript,
    
    /// <summary>Update database to a specified migration</summary>
    DatabaseUpdate,
    
    /// <summary>Drop the database</summary>
    DatabaseDrop,
    
    /// <summary>List available DbContext types</summary>
    DbContextList,
    
    /// <summary>Get information about a DbContext type</summary>
    DbContextInfo,
    
    /// <summary>Scaffold a DbContext and entity types from an existing database</summary>
    DbContextScaffold
}

/// <summary>
/// Actions for the consolidated dotnet_workload tool.
/// Manages .NET SDK workloads for cross-platform development.
/// </summary>
public enum DotnetWorkloadAction
{
    /// <summary>List installed workloads</summary>
    List,
    
    /// <summary>Get information about a specific workload</summary>
    Info,
    
    /// <summary>Search for available workloads</summary>
    Search,
    
    /// <summary>Install a workload</summary>
    Install,
    
    /// <summary>Update installed workloads</summary>
    Update,
    
    /// <summary>Uninstall a workload</summary>
    Uninstall
}

/// <summary>
/// Actions for the consolidated dotnet_tool tool.
/// Manages .NET CLI tools (global, local, and tool manifests).
/// </summary>
public enum DotnetToolAction
{
    /// <summary>Install a .NET tool</summary>
    Install,
    
    /// <summary>List installed tools</summary>
    List,
    
    /// <summary>Update a .NET tool</summary>
    Update,
    
    /// <summary>Uninstall a .NET tool</summary>
    Uninstall,
    
    /// <summary>Restore tools specified in the manifest</summary>
    Restore,
    
    /// <summary>Create a tool manifest file</summary>
    CreateManifest,
    
    /// <summary>Search for available tools on NuGet.org</summary>
    Search,
    
    /// <summary>Run a .NET tool</summary>
    Run
}

/// <summary>
/// Actions for the consolidated dotnet_sdk tool.
/// Provides information about the .NET SDK, runtimes, templates, and frameworks.
/// </summary>
public enum DotnetSdkAction
{
    /// <summary>Display the current SDK version</summary>
    Version,
    
    /// <summary>Display detailed SDK information</summary>
    Info,
    
    /// <summary>List installed SDKs</summary>
    ListSdks,
    
    /// <summary>List installed runtimes</summary>
    ListRuntimes,
    
    /// <summary>List available templates</summary>
    ListTemplates,
    
    /// <summary>Search for templates</summary>
    SearchTemplates,
    
    /// <summary>Get template information</summary>
    TemplateInfo,
    
    /// <summary>Clear template cache</summary>
    ClearTemplateCache,
    
    /// <summary>Get framework information</summary>
    FrameworkInfo,
    
    /// <summary>Manage NuGet local caches</summary>
    NuGetLocals
}

/// <summary>
/// Actions for the consolidated dotnet_dev_certs tool.
/// Manages developer certificates and user secrets.
/// </summary>
public enum DotnetDevCertsAction
{
    /// <summary>Trust the HTTPS development certificate</summary>
    CertificateTrust,
    
    /// <summary>Check if the development certificate is trusted</summary>
    CertificateCheck,
    
    /// <summary>Clean up development certificates</summary>
    CertificateClean,
    
    /// <summary>Export the development certificate</summary>
    CertificateExport,
    
    /// <summary>Initialize user secrets for a project</summary>
    SecretsInit,
    
    /// <summary>Set a user secret value</summary>
    SecretsSet,
    
    /// <summary>List user secrets</summary>
    SecretsList,
    
    /// <summary>Remove a user secret</summary>
    SecretsRemove,
    
    /// <summary>Clear all user secrets</summary>
    SecretsClear
}
