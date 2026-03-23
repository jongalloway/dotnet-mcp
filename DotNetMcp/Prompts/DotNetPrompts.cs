using System.ComponentModel;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;

namespace DotNetMcp;

/// <summary>
/// Predefined prompt catalog for common .NET development workflows.
/// These prompts provide ready-made conversation starters for AI assistants,
/// guiding them through typical .NET SDK operations step by step.
/// </summary>
[McpServerPromptType]
public sealed class DotNetPrompts
{
    /// <summary>
    /// Guide for creating a new ASP.NET Core Web API project with recommended setup steps.
    /// </summary>
    /// <param name="projectName">Name for the new Web API project</param>
    /// <param name="outputDirectory">Output directory for the new project (optional)</param>
    [McpServerPrompt(Name = "create_new_webapi", Title = "Create New Web API")]
    [Description("Guide for creating a new ASP.NET Core Web API project with recommended setup steps")]
    public static IList<ChatMessage> CreateNewWebApi(
        [Description("Name for the new Web API project")] string projectName,
        [Description("Output directory for the new project (optional, defaults to current directory)")] string? outputDirectory = null)
    {
        var outputArg = outputDirectory != null ? $"\n- output: {outputDirectory}" : string.Empty;
        var outputNote = outputDirectory != null ? $" in '{outputDirectory}'" : string.Empty;

        return
        [
            new ChatMessage(ChatRole.User,
                $"""
                Please create a new ASP.NET Core Web API project called '{projectName}'{outputNote} and set it up with recommended tooling.

                Steps:
                1. Use dotnet_project (action: New, template: webapi, name: {projectName}{outputArg}) to scaffold the project
                2. Use dotnet_project (action: Build) to verify the project compiles successfully
                3. Use dotnet_package (action: List) to show installed packages

                The webapi template includes a minimal API setup. If Swagger/OpenAPI documentation is needed, consider adding Swashbuckle.AspNetCore.
                """)
        ];
    }

    /// <summary>
    /// Guide for adding a NuGet package to a project and restoring dependencies.
    /// </summary>
    /// <param name="packageId">The NuGet package ID to add (e.g., 'Newtonsoft.Json', 'Serilog')</param>
    /// <param name="projectPath">Path to the project file (optional, uses current directory if omitted)</param>
    /// <param name="version">Specific version to install (optional, defaults to latest stable)</param>
    [McpServerPrompt(Name = "add_package_and_restore", Title = "Add Package and Restore")]
    [Description("Guide for adding a NuGet package to a project and restoring dependencies")]
    public static IList<ChatMessage> AddPackageAndRestore(
        [Description("The NuGet package ID to add (e.g., 'Newtonsoft.Json', 'Serilog')")] string packageId,
        [Description("Path to the project file (optional, uses current directory if omitted)")] string? projectPath = null,
        [Description("Specific version to install (optional, defaults to latest stable)")] string? version = null)
    {
        var projectArg = projectPath != null ? $"\n- project: {projectPath}" : string.Empty;
        var versionArg = version != null ? $"\n- version: {version}" : string.Empty;
        var versionNote = version != null ? $" version {version}" : " (latest stable)";
        var projectNote = projectPath != null ? $" to '{projectPath}'" : string.Empty;

        return
        [
            new ChatMessage(ChatRole.User,
                $"""
                Please add the NuGet package '{packageId}'{versionNote}{projectNote} and confirm it restores successfully.

                Steps:
                1. Use dotnet_package (action: Search, searchTerm: {packageId}) to verify the package exists and find the latest version
                2. Use dotnet_package (action: Add, packageId: {packageId}{versionArg}{projectArg}) to add the package reference
                3. Use dotnet_project (action: Restore{(projectPath != null ? $", project: {projectPath}" : string.Empty)}) to restore all dependencies
                4. Use dotnet_project (action: Build{(projectPath != null ? $", project: {projectPath}" : string.Empty)}) to verify the project still compiles

                If the package is not found or there are version conflicts, report the issue and suggest alternatives.
                """)
        ];
    }

    /// <summary>
    /// Guide for running a .NET project's tests and generating a coverage report.
    /// </summary>
    /// <param name="projectPath">Path to the test project file (optional)</param>
    /// <param name="filter">Optional test filter expression (e.g., 'Category=Unit')</param>
    [McpServerPrompt(Name = "run_tests_with_coverage", Title = "Run Tests with Coverage")]
    [Description("Guide for running .NET project tests and generating a code coverage report")]
    public static IList<ChatMessage> RunTestsWithCoverage(
        [Description("Path to the test project file (optional, runs all tests if omitted)")] string? projectPath = null,
        [Description("Optional test filter expression (e.g., 'Category=Unit', 'FullyQualifiedName~MyTests')")] string? filter = null)
    {
        var projectArg = projectPath != null ? $"\n- project: {projectPath}" : string.Empty;
        var filterArg = filter != null ? $"\n- filter: {filter}" : string.Empty;

        return
        [
            new ChatMessage(ChatRole.User,
                $"""
                Please run the .NET tests{(projectPath != null ? $" in '{projectPath}'" : string.Empty)} and generate a code coverage report.

                Steps:
                1. Use dotnet_project (action: Build{projectArg}) to ensure the project compiles
                2. Use dotnet_project (action: Test{projectArg}{filterArg}, collect: XPlat Code Coverage, logger: console;verbosity=normal) to run tests with coverage
                3. Summarize the test results, including pass/fail counts and any failures

                If tests fail, examine the error output and suggest likely causes or fixes.
                If coverage data is generated, note the output location for further analysis.
                """)
        ];
    }

    /// <summary>
    /// Guided exploration of the .NET SDK environment with the interactive dashboard.
    /// </summary>
    [McpServerPrompt(Name = "explore_sdk_environment", Title = "Explore SDK Environment")]
    [Description("Interactive exploration of the .NET SDK environment using the SDK dashboard")]
    public static IList<ChatMessage> ExploreSdkEnvironment()
    {
        return
        [
            new ChatMessage(ChatRole.User,
                """
                Show me my .NET SDK environment using the SDK dashboard.

                Steps:
                1. Use DotnetSdk (action: ListSdks) to show installed SDKs — the dashboard will display them visually
                2. Briefly summarize the SDK versions and suggest next steps like:
                   - Upgrading to the latest SDK if not current
                   - Setting up a global.json to pin a specific version
                   - Exploring available templates for new projects
                
                Keep your text response brief since the dashboard shows the details.
                """)
        ];
    }

    /// <summary>
    /// Project health check workflow with interactive dashboard.
    /// </summary>
    /// <param name="projectPath">Path to the project to analyze (optional)</param>
    [McpServerPrompt(Name = "project_health_check", Title = "Project Health Check")]
    [Description("Run a comprehensive health check on a .NET project with interactive dashboard")]
    public static IList<ChatMessage> ProjectHealthCheck(
        [Description("Path to the project file to check (optional)")] string? projectPath = null)
    {
        var projectArg = projectPath != null ? $", project: {projectPath}" : string.Empty;

        return
        [
            new ChatMessage(ChatRole.User,
                $"""
                Run a health check on my .NET project{(projectPath != null ? $" at '{projectPath}'" : string.Empty)} and show results in the project dashboard.

                Steps:
                1. Use DotnetProject (action: Build{projectArg}) to verify the project compiles
                2. Use DotnetPackage (action: List{projectArg}, outdated: true) to check for outdated packages
                3. Use DotnetProject (action: Test{projectArg}) to run tests if a test project
                4. Summarize the health status: build OK/failed, outdated packages count, test results
                
                The project dashboard will display results visually. Keep your summary brief — focus on action items.
                """)
        ];
    }

    /// <summary>
    /// Guided NuGet package exploration using the package explorer dashboard.
    /// </summary>
    /// <param name="searchTerm">What to search for on NuGet</param>
    [McpServerPrompt(Name = "explore_packages", Title = "Explore NuGet Packages")]
    [Description("Search and explore NuGet packages using the interactive package explorer")]
    public static IList<ChatMessage> ExplorePackages(
        [Description("Package or category to search for (e.g., 'logging', 'json', 'testing')")] string searchTerm)
    {
        return
        [
            new ChatMessage(ChatRole.User,
                $"""
                Help me find a good NuGet package for "{searchTerm}" using the package explorer.

                Steps:
                1. Use DotnetPackage (action: Search, searchTerm: {searchTerm}) to find relevant packages
                2. Recommend the top 2-3 packages, comparing their popularity and features
                3. If I already have related packages installed, use DotnetPackage (action: List) to check compatibility
                4. Suggest which package to add and explain why
                
                The package explorer dashboard will display search results interactively.
                """)
        ];
    }
}
