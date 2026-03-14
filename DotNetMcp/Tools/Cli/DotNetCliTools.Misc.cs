using System.Reflection;
using System.Text;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace DotNetMcp;

/// <summary>
/// Miscellaneous tools for help, server information, and code formatting.
/// </summary>
public sealed partial class DotNetCliTools
{
    /// <summary>
    /// Get help for a specific dotnet command. Use this to discover available options for any dotnet command.
    /// </summary>
    /// <param name="command">The dotnet command to get help for (e.g., 'build', 'new', 'run'). If not specified, shows general dotnet help.</param>
    [McpServerTool(Title = ".NET CLI Help", ReadOnly = true, Idempotent = true, IconSource = "https://raw.githubusercontent.com/microsoft/fluentui-emoji/62ecdc0d7ca5c6df32148c169556bc8d3782fca4/assets/Light%20Bulb/Flat/light_bulb_flat.svg")]
    [McpMeta("category", "help")]
    [McpMeta("priority", 5.0)]
    public async partial Task<CallToolResult> DotnetHelp(
        string? command = null)
        => StructuredContentHelper.ToCallToolResult(await ExecuteDotNetCommand(command != null ? $"{command} --help" : "--help"));

    /// <summary>
    /// Get a machine-readable JSON snapshot of server capabilities, versions, and supported features for agent orchestration and discovery.
    /// </summary>
    [McpServerTool(Title = "Server Capabilities", ReadOnly = true, Idempotent = true, IconSource = "https://raw.githubusercontent.com/microsoft/fluentui-emoji/62ecdc0d7ca5c6df32148c169556bc8d3782fca4/assets/Bar%20Chart/Flat/bar_chart_flat.svg")]
    [McpMeta("category", "help")]
    [McpMeta("priority", 8.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("tags", JsonValue = """["capabilities","version","discovery","orchestration","metadata"]""")]
    public async partial Task<CallToolResult> DotnetServerCapabilities()
    {
        // Get the assembly version
        var assembly = typeof(DotNetCliTools).Assembly;
        var version = assembly.GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? DefaultServerVersion;

        // Parse installed SDKs from dotnet --list-sdks
        var sdksOutput = await ExecuteDotNetCommand("--list-sdks");
        var installedSdks = ParseInstalledSdks(sdksOutput);

        // Create the capabilities snapshot
        var capabilities = new ServerCapabilities
        {
            ServerVersion = version,
            ProtocolVersion = ProtocolVersion,
            SupportedCategories = new[]
            {
                "template",
                "project",
                "package",
                "solution",
                "reference",
                "tool",
                "watch",
                "sdk",
                "security",
                "framework",
                "format",
                "nuget",
                "help",
                "efcore",
                "telemetry"
            },
            Supports = new ServerFeatureSupport
            {
                StructuredErrors = true,
                MachineReadable = true,
                Cancellation = true,
                Telemetry = true,  // SDK v0.6+ supports request duration logging and OpenTelemetry semantic conventions
                Metrics = true,    // In-memory per-tool metrics via MCP message filter (dotnet_server_metrics tool)
                AsyncTasks = true,  // MCP Task support enabled: long-running operations (build, test, publish) can run as async tasks
                Prompts = true,     // Predefined prompt catalog: create_new_webapi, add_package_and_restore, run_tests_with_coverage
                Elicitation = true,  // Elicitation for confirmation before destructive ops (Clean, solution Remove)
                Sampling = true,     // Sampling for AI-assisted build/test error interpretation (when client supports it)
                ProgressNotifications = true // Real-time progress updates for build, test, publish, and other long-running operations
            },
            SdkVersions = new SdkVersionInfo
            {
                Installed = installedSdks,
                Recommended = FrameworkHelper.GetLatestRecommendedFramework(),
                Lts = FrameworkHelper.GetLatestLtsFramework()
            }
        };

        var json = ErrorResultFactory.ToJson(capabilities);
        return StructuredContentHelper.ToCallToolResult(json, capabilities);
    }

    /// <summary>
    /// Get detailed human-readable information about .NET MCP Server capabilities, including supported features, concurrency safety, and available resources.
    /// Provides guidance for AI orchestrators on parallel execution.
    /// </summary>
    [McpServerTool(Title = "Server Information", ReadOnly = true, Idempotent = true, IconSource = "https://raw.githubusercontent.com/microsoft/fluentui-emoji/62ecdc0d7ca5c6df32148c169556bc8d3782fca4/assets/Information/Flat/information_flat.svg")]
    [McpMeta("category", "help")]
    [McpMeta("priority", 5.0)]
    public partial Task<CallToolResult> DotnetServerInfo()
    {
        var result = new StringBuilder();
        result.AppendLine("=== .NET MCP Server Capabilities ===");
        result.AppendLine();
        result.AppendLine("Version: 1.0+");
        result.AppendLine("Protocol: Model Context Protocol (MCP)");
        result.AppendLine("Transport: stdio");
        result.AppendLine();

        result.AppendLine("FEATURES:");
        result.AppendLine("  • 12 Consolidated MCP Tools (8 functional + 4 utility)");
        result.AppendLine("  • 4 MCP Resources (SDK, Runtime, Templates, Frameworks)");
        result.AppendLine("  • 3 Predefined Prompts (create_new_webapi, add_package_and_restore, run_tests_with_coverage)");
        result.AppendLine("  • Elicitation support: confirmation dialogs for destructive operations (Clean, solution Remove)");
        result.AppendLine("  • Telemetry: in-memory metrics collected via MCP message filter (dotnet_server_metrics)");
        result.AppendLine("  • Direct .NET SDK integration via NuGet packages");
        result.AppendLine("  • Template Engine integration with caching (5-min TTL)");
        result.AppendLine("  • Framework validation and LTS identification");
        result.AppendLine("  • MSBuild integration for project analysis");
        result.AppendLine("  • Thread-safe caching with metrics tracking");
        result.AppendLine();

        result.AppendLine("CONSOLIDATED TOOLS:");
        result.AppendLine("  • dotnet_project (13 actions): New, Restore, Build, Run, Test, Publish, Clean, Analyze, Dependencies, Validate, Pack, Watch, Format");
        result.AppendLine("  • dotnet_package (9 actions): Add, Remove, Search, Update, List, AddReference, RemoveReference, ListReferences, ClearCache");
        result.AppendLine("  • dotnet_solution (4 actions): Create, Add, List, Remove");
        result.AppendLine("  • dotnet_ef (9 actions): MigrationsAdd, MigrationsList, MigrationsRemove, MigrationsScript, DatabaseUpdate, DatabaseDrop, DbContextList, DbContextInfo, DbContextScaffold");
        result.AppendLine("  • dotnet_workload (6 actions): List, Info, Search, Install, Update, Uninstall");
        result.AppendLine("  • dotnet_tool (8 actions): Install, List, Update, Uninstall, Restore, CreateManifest, Search, Run");
        result.AppendLine("  • dotnet_sdk (10 actions): Version, Info, ListSdks, ListRuntimes, ListTemplates, SearchTemplates, TemplateInfo, ClearTemplateCache, FrameworkInfo, CacheMetrics");
        result.AppendLine("  • dotnet_dev_certs (9 actions): CertificateTrust, CertificateCheck, CertificateClean, CertificateExport, SecretsInit, SecretsSet, SecretsList, SecretsRemove, SecretsClear");
        result.AppendLine();

        result.AppendLine("UTILITY TOOLS:");
        result.AppendLine("  • dotnet_help: Get help for any dotnet command");
        result.AppendLine("  • dotnet_server_capabilities: Machine-readable server capabilities JSON");
        result.AppendLine("  • dotnet_server_info: This detailed information output");
        result.AppendLine("  • dotnet_server_metrics: In-memory telemetry metrics (Get/Reset) collected via message filter");
        result.AppendLine();

        result.AppendLine("CONCURRENCY SAFETY:");
        result.AppendLine("  ✅ Read-only operations: Always safe for parallel execution");
        result.AppendLine("     (Info, List, Search, Check, Help, Metrics tools)");
        result.AppendLine("  ⚠️  Mutating operations: Safe on different targets only");
        result.AppendLine("     (Build, Add, Remove operations on different projects)");
        result.AppendLine("  ❌ Global/Long-running: Never run in parallel");
        result.AppendLine("     (Watch commands, Run, Certificate operations, Cache clearing)");
        result.AppendLine();
        result.AppendLine("  📖 See documentation: doc/concurrency.md");
        result.AppendLine("     Full concurrency safety matrix with detailed guidance");
        result.AppendLine();

        result.AppendLine("CACHING:");
        result.AppendLine("  • Templates: 5-minute TTL, thread-safe with metrics");
        result.AppendLine("  • SDK Info: 5-minute TTL, thread-safe with metrics");
        result.AppendLine("  • Runtime Info: 5-minute TTL, thread-safe with metrics");
        result.AppendLine("  • Force reload available on template tools");
        result.AppendLine("  • Use dotnet_sdk (action: CacheMetrics) for hit/miss statistics");
        result.AppendLine();

        result.AppendLine("RESOURCES (Read-Only Access):");
        result.AppendLine("  • dotnet://sdk-info - Installed SDKs with versions and paths");
        result.AppendLine("  • dotnet://runtime-info - Installed runtimes with metadata");
        result.AppendLine("  • dotnet://templates - Complete template catalog");
        result.AppendLine("  • dotnet://frameworks - Framework information with LTS status");
        result.AppendLine();

        result.AppendLine("PROMPTS (Predefined Workflow Guides):");
        result.AppendLine("  • create_new_webapi: Guide for creating a new ASP.NET Core Web API project");
        result.AppendLine("  • add_package_and_restore: Guide for adding a NuGet package and restoring dependencies");
        result.AppendLine("  • run_tests_with_coverage: Guide for running tests and generating a coverage report");
        result.AppendLine();

        result.AppendLine("ELICITATION (Confirmation Dialogs):");
        result.AppendLine("  When the MCP client supports elicitation, the server requests user confirmation before:");
        result.AppendLine("  • dotnet_project (action: Clean) - confirms before deleting build artifacts");
        result.AppendLine("  • dotnet_solution (action: Remove) - confirms before removing projects from solution");
        result.AppendLine("  Clients that do not support elicitation proceed without a confirmation prompt.");
        result.AppendLine();

        result.AppendLine("SAMPLING (AI-Assisted Error Analysis):");
        result.AppendLine("  When the MCP client supports sampling, the server requests LLM completions to interpret:");
        result.AppendLine("  • dotnet_project (action: Build) - summarizes build errors and suggests fixes on failure");
        result.AppendLine("  • dotnet_project (action: Test) - analyzes test failures and suggests which tests need attention");
        result.AppendLine("  Clients that do not support sampling receive raw command output only.");
        result.AppendLine();

        result.AppendLine("DOCUMENTATION:");
        result.AppendLine("  • README: https://github.com/jongalloway/dotnet-mcp");
        result.AppendLine("  • SDK Integration: doc/sdk-integration.md");
        result.AppendLine("  • Advanced Topics: doc/advanced-topics.md");
        result.AppendLine("  • Concurrency Safety: doc/concurrency.md");
        result.AppendLine();

        result.AppendLine("For detailed concurrency guidance and parallel execution patterns,");
        result.AppendLine("see the Concurrency Safety Matrix at: doc/concurrency.md");

        return Task.FromResult(StructuredContentHelper.ToCallToolResult(result.ToString()));
    }

    /// <summary>
    /// Format code according to .editorconfig and style rules. Available since .NET 6 SDK.
    /// Useful for enforcing consistent code style across projects.
    /// </summary>
    /// <param name="project">The project or solution file to format</param>
    /// <param name="verify">Verify formatting without making changes</param>
    /// <param name="includeGenerated">Include generated code files</param>
    /// <param name="diagnostics">Comma-separated list of diagnostic IDs to fix</param>
    /// <param name="severity">Severity level to fix (info, warn, error)</param>
    internal async Task<string> DotnetFormat(
        string? project = null,
        bool verify = false,
        bool includeGenerated = false,
        string? diagnostics = null,
        string? severity = null)
    {
        var args = new StringBuilder("format");
        if (!string.IsNullOrEmpty(project)) args.Append($" \"{project}\"");
        if (verify) args.Append(" --verify-no-changes");
        if (includeGenerated) args.Append(" --include-generated");
        if (!string.IsNullOrEmpty(diagnostics)) args.Append($" --diagnostics {diagnostics}");
        if (!string.IsNullOrEmpty(severity)) args.Append($" --severity {severity}");
        return await ExecuteDotNetCommand(args.ToString());
    }

    /// <summary>
    /// Enable telemetry reporting for .NET SDK usage analytics. This feature is planned but not yet implemented.
    /// </summary>
    /// <param name="enable">Whether to enable or disable telemetry (preserved for future implementation)</param>
    /// <returns>JSON error response indicating the feature is not yet available</returns>
    [McpMeta("category", "telemetry")]
    [McpMeta("priority", 2.0)]
    [McpMeta("planned", true)]
    public Task<CallToolResult> DotnetTelemetry(
        bool enable = true)
    {
        // This feature is not yet implemented
        // Parameters are preserved for future implementation and API consistency
        var alternatives = new List<string>
        {
            "Use dotnet_server_capabilities to check current feature support",
            "Monitor SDK usage manually through build logs",
            "Use external telemetry tools like Application Insights"
        };

        var error = ErrorResultFactory.ReturnCapabilityNotAvailable(
            "telemetry reporting",
            "Not yet implemented - planned for future release",
            alternatives);

        return Task.FromResult(StructuredContentHelper.ToCallToolResult(ErrorResultFactory.ToJson(error)));
    }
}
