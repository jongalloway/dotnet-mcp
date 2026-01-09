using System.Reflection;
using System.Text;
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
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "help")]
    [McpMeta("priority", 5.0)]
    public async partial Task<string> DotnetHelp(
        string? command = null,
        bool machineReadable = false)
        => await ExecuteDotNetCommand(command != null ? $"{command} --help" : "--help", machineReadable);

    /// <summary>
    /// Get a machine-readable JSON snapshot of server capabilities, versions, and supported features for agent orchestration and discovery.
    /// </summary>
    [McpServerTool]
    [McpMeta("category", "help")]
    [McpMeta("priority", 8.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("tags", JsonValue = """["capabilities","version","discovery","orchestration","metadata"]""")]
    public async partial Task<string> DotnetServerCapabilities()
    {
        // Get the assembly version
        var assembly = typeof(DotNetCliTools).Assembly;
        var version = assembly.GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? DefaultServerVersion;

        // Parse installed SDKs from dotnet --list-sdks
        var sdksOutput = await ExecuteDotNetCommand("--list-sdks", machineReadable: false);
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
                "efcore"
            },
            Supports = new ServerFeatureSupport
            {
                StructuredErrors = true,
                MachineReadable = true,
                Cancellation = true,
                Telemetry = false  // Future feature
            },
            SdkVersions = new SdkVersionInfo
            {
                Installed = installedSdks,
                Recommended = FrameworkHelper.GetLatestRecommendedFramework(),
                Lts = FrameworkHelper.GetLatestLtsFramework()
            }
        };

        return ErrorResultFactory.ToJson(capabilities);
    }

    /// <summary>
    /// Get detailed human-readable information about .NET MCP Server capabilities, including supported features, concurrency safety, and available resources.
    /// Provides guidance for AI orchestrators on parallel execution.
    /// </summary>
    [McpServerTool]
    [McpMeta("category", "help")]
    [McpMeta("priority", 5.0)]
    public partial Task<string> DotnetServerInfo()
    {
        var result = new StringBuilder();
        result.AppendLine("=== .NET MCP Server Capabilities ===");
        result.AppendLine();
        result.AppendLine("Version: 1.0+");
        result.AppendLine("Protocol: Model Context Protocol (MCP)");
        result.AppendLine("Transport: stdio");
        result.AppendLine();

        result.AppendLine("FEATURES:");
        result.AppendLine("  • 11 Consolidated MCP Tools (8 functional + 3 utility)");
        result.AppendLine("  • 4 MCP Resources (SDK, Runtime, Templates, Frameworks)");
        result.AppendLine("  • Direct .NET SDK integration via NuGet packages");
        result.AppendLine("  • Template Engine integration with caching (5-min TTL)");
        result.AppendLine("  • Framework validation and LTS identification");
        result.AppendLine("  • MSBuild integration for project analysis");
        result.AppendLine("  • Thread-safe caching with metrics tracking");
        result.AppendLine();

        result.AppendLine("CONSOLIDATED TOOLS:");
        result.AppendLine("  • dotnet_project (12 actions): New, Restore, Build, Run, Test, Publish, Clean, Analyze, Dependencies, Validate, Pack, Watch, Format");
        result.AppendLine("  • dotnet_package (8 actions): Add, Remove, Search, Update, List, AddReference, RemoveReference, ListReferences, ClearCache");
        result.AppendLine("  • dotnet_solution (3 actions): Create, Add, List, Remove");
        result.AppendLine("  • dotnet_ef (8 actions): MigrationsAdd, MigrationsList, MigrationsRemove, MigrationsScript, DatabaseUpdate, DatabaseDrop, DbContextList, DbContextInfo, DbContextScaffold");
        result.AppendLine("  • dotnet_workload (5 actions): List, Info, Search, Install, Update, Uninstall");
        result.AppendLine("  • dotnet_tool (7 actions): Install, List, Update, Uninstall, Restore, CreateManifest, Search, Run");
        result.AppendLine("  • dotnet_sdk (9 actions): Version, Info, ListSdks, ListRuntimes, ListTemplates, SearchTemplates, TemplateInfo, ClearTemplateCache, FrameworkInfo, CacheMetrics");
        result.AppendLine("  • dotnet_dev_certs (8 actions): CertificateTrust, CertificateCheck, CertificateClean, CertificateExport, SecretsInit, SecretsSet, SecretsList, SecretsRemove, SecretsClear");
        result.AppendLine();

        result.AppendLine("UTILITY TOOLS:");
        result.AppendLine("  • dotnet_help: Get help for any dotnet command");
        result.AppendLine("  • dotnet_server_capabilities: Machine-readable server capabilities JSON");
        result.AppendLine("  • dotnet_server_info: This detailed information output");
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
        result.AppendLine("  • Use dotnet_cache_metrics for hit/miss statistics");
        result.AppendLine();

        result.AppendLine("RESOURCES (Read-Only Access):");
        result.AppendLine("  • dotnet://sdk-info - Installed SDKs with versions and paths");
        result.AppendLine("  • dotnet://runtime-info - Installed runtimes with metadata");
        result.AppendLine("  • dotnet://templates - Complete template catalog");
        result.AppendLine("  • dotnet://frameworks - Framework information with LTS status");
        result.AppendLine();

        result.AppendLine("DOCUMENTATION:");
        result.AppendLine("  • README: https://github.com/jongalloway/dotnet-mcp");
        result.AppendLine("  • SDK Integration: doc/sdk-integration.md");
        result.AppendLine("  • Advanced Topics: doc/advanced-topics.md");
        result.AppendLine("  • Concurrency Safety: doc/concurrency.md");
        result.AppendLine();

        result.AppendLine("For detailed concurrency guidance and parallel execution patterns,");
        result.AppendLine("see the Concurrency Safety Matrix at: doc/concurrency.md");

        return Task.FromResult(result.ToString());
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
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpMeta("category", "format")]
    [McpMeta("priority", 6.0)]
    [McpMeta("minimumSdkVersion", "6.0")]
    public async Task<string> DotnetFormat(
        string? project = null,
        bool verify = false,
        bool includeGenerated = false,
        string? diagnostics = null,
        string? severity = null,
        bool machineReadable = false)
    {
        var args = new StringBuilder("format");
        if (!string.IsNullOrEmpty(project)) args.Append($" \"{project}\"");
        if (verify) args.Append(" --verify-no-changes");
        if (includeGenerated) args.Append(" --include-generated");
        if (!string.IsNullOrEmpty(diagnostics)) args.Append($" --diagnostics {diagnostics}");
        if (!string.IsNullOrEmpty(severity)) args.Append($" --severity {severity}");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    /// <summary>
    /// Enable telemetry reporting for .NET SDK usage analytics. This feature is planned but not yet implemented.
    /// </summary>
    /// <param name="enable">Whether to enable or disable telemetry (preserved for future implementation)</param>
    /// <param name="machineReadable">Ignored - method always returns JSON (preserved for consistency with other tools)</param>
    /// <returns>JSON error response indicating the feature is not yet available</returns>
    [McpMeta("category", "telemetry")]
    [McpMeta("priority", 2.0)]
    [McpMeta("planned", true)]
    public Task<string> DotnetTelemetry(
        bool enable = true,
        bool machineReadable = false)
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

        return Task.FromResult(ErrorResultFactory.ToJson(error));
    }
}
