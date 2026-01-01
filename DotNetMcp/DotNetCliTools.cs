using System.Reflection;
using System.Text;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace DotNetMcp;

/// <summary>
/// Tool methods for .NET CLI operations. This is a partial class split across multiple files.
/// See Tools/DotNetCliTools.Core.cs for class infrastructure.
/// </summary>
public sealed partial class DotNetCliTools
{
    /// <summary>
    /// Create a new .NET project or file from a template. 
    /// Common templates: console, classlib, web, webapi, mvc, blazor, xunit, nunit, mstest.
    /// </summary>
    /// <param name="template">The template to use (e.g., 'console', 'classlib', 'webapi')</param>
    /// <param name="name">The name for the project</param>
    /// <param name="output">The output directory</param>
    /// <param name="framework">The target framework (e.g., 'net10.0', 'net8.0')</param>
    /// <param name="additionalOptions">Additional template-specific options (e.g., '--format slnx', '--use-program-main', '--aot')</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "project")]
    [McpMeta("priority", 10.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("tags", JsonValue = """["project","create","new","template","initialization"]""")]
    public async partial Task<string> DotnetProjectNew(
        string? template = null,
        string? name = null,
        string? output = null,
        string? framework = null,
        string? additionalOptions = null,
        bool machineReadable = false)
    {
        if (string.IsNullOrWhiteSpace(template))
            return "Error: template parameter is required.";

        // Validate additionalOptions to prevent injection attempts
        if (!string.IsNullOrEmpty(additionalOptions) && !IsValidAdditionalOptions(additionalOptions))
            return "Error: additionalOptions contains invalid characters. Only alphanumeric characters, hyphens, underscores, dots, spaces, and equals signs are allowed.";

        var args = new StringBuilder($"new {template}");
        if (!string.IsNullOrEmpty(name)) args.Append($" -n \"{name}\"");
        if (!string.IsNullOrEmpty(output)) args.Append($" -o \"{output}\"");
        if (!string.IsNullOrEmpty(framework)) args.Append($" -f {framework}");
        if (!string.IsNullOrEmpty(additionalOptions)) args.Append($" {additionalOptions}");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    /// <summary>
    /// Restore the dependencies and tools of a .NET project.
    /// </summary>
    /// <param name="project">The project file or solution file to restore</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "project")]
    [McpMeta("priority", 8.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("tags", JsonValue = """["project","restore","dependencies","packages","setup"]""")]
    public async partial Task<string> DotnetProjectRestore(
        string? project = null,
        bool machineReadable = false)
    {
        var args = "restore";
        if (!string.IsNullOrEmpty(project)) args += $" \"{project}\"";
        return await ExecuteDotNetCommand(args, machineReadable);
    }

    /// <summary>
    /// Build a .NET project and its dependencies.
    /// </summary>
    /// <param name="project">The project file or solution file to build</param>
    /// <param name="configuration">The configuration to build (Debug or Release)</param>
    /// <param name="framework">Build for a specific framework</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "project")]
    [McpMeta("priority", 10.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("isLongRunning", true)]
    [McpMeta("tags", JsonValue = """["project","build","compile","compilation"]""")]
    public async partial Task<string> DotnetProjectBuild(
        string? project = null,
        string? configuration = null,
        string? framework = null,
        bool machineReadable = false)
    {
        var args = new StringBuilder("build");
        if (!string.IsNullOrEmpty(project)) args.Append($" \"{project}\"");
        if (!string.IsNullOrEmpty(configuration)) args.Append($" -c {configuration}");
        if (!string.IsNullOrEmpty(framework)) args.Append($" -f {framework}");

        return await ExecuteWithConcurrencyCheck("build", GetOperationTarget(project), args.ToString(), machineReadable);
    }

    /// <summary>
    /// Build and run a .NET project.
    /// </summary>
    /// <param name="project">The project file to run</param>
    /// <param name="configuration">The configuration to use (Debug or Release)</param>
    /// <param name="appArgs">Arguments to pass to the application</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "project")]
    [McpMeta("priority", 9.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("isLongRunning", true)]
    [McpMeta("tags", JsonValue = """["project","run","execute","launch","development"]""")]
    public async partial Task<string> DotnetProjectRun(
        string? project = null,
        string? configuration = null,
        string? appArgs = null,
        bool machineReadable = false)
    {
        var args = new StringBuilder("run");
        if (!string.IsNullOrEmpty(project)) args.Append($" --project \"{project}\"");
        if (!string.IsNullOrEmpty(configuration)) args.Append($" -c {configuration}");
        if (!string.IsNullOrEmpty(appArgs)) args.Append($" -- {appArgs}");

        return await ExecuteWithConcurrencyCheck("run", GetOperationTarget(project), args.ToString(), machineReadable);
    }

    /// <summary>
    /// Run unit tests in a .NET project.
    /// </summary>
    /// <param name="project">The project file or solution file to test</param>
    /// <param name="configuration">The configuration to test (Debug or Release)</param>
    /// <param name="filter">Filter to run specific tests</param>
    /// <param name="collect">The friendly name of the data collector (e.g., 'XPlat Code Coverage')</param>
    /// <param name="resultsDirectory">The directory where test results will be placed</param>
    /// <param name="logger">The logger to use for test results (e.g., 'trx', 'console;verbosity=detailed')</param>
    /// <param name="noBuild">Do not build the project before testing</param>
    /// <param name="noRestore">Do not restore the project before building</param>
    /// <param name="verbosity">Set the MSBuild verbosity level (quiet, minimal, normal, detailed, diagnostic)</param>
    /// <param name="framework">The target framework to test for</param>
    /// <param name="blame">Run tests in blame mode to isolate problematic tests</param>
    /// <param name="listTests">List discovered tests without running them</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "project")]
    [McpMeta("priority", 9.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("isLongRunning", true)]
    [McpMeta("tags", JsonValue = """["project","test","testing","unit-test","validation"]""")]
    public async partial Task<string> DotnetProjectTest(
        string? project = null,
        string? configuration = null,
        string? filter = null,
        string? collect = null,
        string? resultsDirectory = null,
        string? logger = null,
        bool noBuild = false,
        bool noRestore = false,
        string? verbosity = null,
        string? framework = null,
        bool blame = false,
        bool listTests = false,
        bool machineReadable = false)
    {
        var args = new StringBuilder("test");
        if (!string.IsNullOrEmpty(project)) args.Append($" \"{project}\"");
        if (!string.IsNullOrEmpty(configuration)) args.Append($" -c {configuration}");
        if (!string.IsNullOrEmpty(filter)) args.Append($" --filter \"{filter}\"");
        if (!string.IsNullOrEmpty(collect)) args.Append($" --collect \"{collect}\"");
        if (!string.IsNullOrEmpty(resultsDirectory)) args.Append($" --results-directory \"{resultsDirectory}\"");
        if (!string.IsNullOrEmpty(logger)) args.Append($" --logger \"{logger}\"");
        if (noBuild) args.Append(" --no-build");
        if (noRestore) args.Append(" --no-restore");
        if (!string.IsNullOrEmpty(verbosity)) args.Append($" --verbosity {verbosity}");
        if (!string.IsNullOrEmpty(framework)) args.Append($" --framework {framework}");
        if (blame) args.Append(" --blame");
        if (listTests) args.Append(" --list-tests");

        return await ExecuteWithConcurrencyCheck("test", GetOperationTarget(project), args.ToString(), machineReadable);
    }

    /// <summary>
    /// Publish a .NET project for deployment.
    /// </summary>
    /// <param name="project">The project file to publish</param>
    /// <param name="configuration">The configuration to publish (Debug or Release)</param>
    /// <param name="output">The output directory for published files</param>
    /// <param name="runtime">The target runtime identifier (e.g., 'linux-x64', 'win-x64')</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "project")]
    [McpMeta("priority", 7.0)]
    [McpMeta("isLongRunning", true)]
    public async partial Task<string> DotnetProjectPublish(
        string? project = null,
        string? configuration = null,
        string? output = null,
        string? runtime = null,
        bool machineReadable = false)
    {
        var args = new StringBuilder("publish");
        if (!string.IsNullOrEmpty(project)) args.Append($" \"{project}\"");
        if (!string.IsNullOrEmpty(configuration)) args.Append($" -c {configuration}");
        if (!string.IsNullOrEmpty(output)) args.Append($" -o \"{output}\"");
        if (!string.IsNullOrEmpty(runtime)) args.Append($" -r {runtime}");

        return await ExecuteWithConcurrencyCheck("publish", GetOperationTarget(project), args.ToString(), machineReadable);
    }

    /// <summary>
    /// Clean the output of a .NET project.
    /// </summary>
    /// <param name="project">The project file or solution file to clean</param>
    /// <param name="configuration">The configuration to clean (Debug or Release)</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "project")]
    [McpMeta("priority", 6.0)]
    public async partial Task<string> DotnetProjectClean(
        string? project = null,
        string? configuration = null,
        bool machineReadable = false)
    {
        var args = new StringBuilder("clean");
        if (!string.IsNullOrEmpty(project)) args.Append($" \"{project}\"");
        if (!string.IsNullOrEmpty(configuration)) args.Append($" -c {configuration}");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

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
        result.AppendLine("  • 52 MCP Tools across 13 categories");
        result.AppendLine("  • 4 MCP Resources (SDK, Runtime, Templates, Frameworks)");
        result.AppendLine("  • Direct .NET SDK integration via NuGet packages");
        result.AppendLine("  • Template Engine integration with caching (5-min TTL)");
        result.AppendLine("  • Framework validation and LTS identification");
        result.AppendLine("  • MSBuild integration for project analysis");
        result.AppendLine("  • Thread-safe caching with metrics tracking");
        result.AppendLine();

        result.AppendLine("TOOL CATEGORIES:");
        result.AppendLine("  • Template (5 tools): List, search, info, cache management");
        result.AppendLine("  • Project (10 tools): New, build, run, test, publish, clean, restore, analyze, dependencies, validate");
        result.AppendLine("  • Package (6 tools): Add, remove, update, list, search, pack");
        result.AppendLine("  • Solution (4 tools): Create, add, remove, list");
        result.AppendLine("  • Reference (3 tools): Add, remove, list");
        result.AppendLine("  • Tool (7 tools): Install, uninstall, update, list, search, restore, run");
        result.AppendLine("  • Watch (3 tools): Watch run, watch test, watch build");
        result.AppendLine("  • SDK (4 tools): Version, info, list SDKs, list runtimes");
        result.AppendLine("  • Security (4 tools): Certificate trust, check, clean, export");
        result.AppendLine("  • Framework (1 tool): Framework information and LTS status");
        result.AppendLine("  • Format (1 tool): Code formatting");
        result.AppendLine("  • NuGet (1 tool): Cache management");
        result.AppendLine("  • Help (2 tools): Command help, server capabilities");
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
    [McpServerTool]
    [McpMeta("category", "format")]
    [McpMeta("priority", 6.0)]
    [McpMeta("minimumSdkVersion", "6.0")]
    public async partial Task<string> DotnetFormat(
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
    /// Trust the HTTPS development certificate. Installs the certificate to the trusted root store.
    /// May require elevation on Windows/macOS. Essential for local ASP.NET Core HTTPS development.
    /// </summary>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "security")]
    [McpMeta("priority", 7.0)]
    [McpMeta("requiresElevation", true)]
    public async partial Task<string> DotnetCertificateTrust(bool machineReadable = false)
        => await ExecuteDotNetCommand("dev-certs https --trust", machineReadable);

    /// <summary>
    /// Check if the HTTPS development certificate exists and is trusted.
    /// Returns certificate status and validity information.
    /// </summary>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "security")]
    [McpMeta("priority", 7.0)]
    public async partial Task<string> DotnetCertificateCheck(bool machineReadable = false)
        => await ExecuteDotNetCommand("dev-certs https --check", machineReadable);

    /// <summary>
    /// Remove all HTTPS development certificates.
    /// Use this to clean up old or invalid certificates before creating new ones.
    /// </summary>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "security")]
    [McpMeta("priority", 6.0)]
    public async partial Task<string> DotnetCertificateClean(bool machineReadable = false)
        => await ExecuteDotNetCommand("dev-certs https --clean", machineReadable);

    /// <summary>
    /// Export the HTTPS development certificate to a file.
    /// Useful for Docker containers or sharing certificates across environments. Supports PFX and PEM formats with optional password protection.
    /// </summary>
    /// <param name="path">Path to export the certificate file</param>
    /// <param name="password">Certificate password for protection (optional, but recommended for PFX format)</param>
    /// <param name="format">Export format: Pfx or Pem (defaults to Pfx if not specified)</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "security")]
    [McpMeta("priority", 6.0)]
    public async partial Task<string> DotnetCertificateExport(
        string path,
        string? password = null,
        string? format = null,
        bool machineReadable = false)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "Error: path parameter is required.";

        // Validate and normalize format if provided
        string? normalizedFormat = null;
        if (!string.IsNullOrEmpty(format))
        {
            normalizedFormat = format.ToLowerInvariant();
            if (normalizedFormat != "pfx" && normalizedFormat != "pem")
                return "Error: format must be either 'pfx' or 'pem' (case-insensitive).";
        }

        // Security Note: The password must be passed as a command-line argument to dotnet dev-certs,
        // which is the standard .NET CLI behavior. While this stores the password temporarily in memory
        // (CodeQL alert cs/cleartext-storage-of-sensitive-information), this is:
        // 1. Required by the .NET CLI interface - there's no alternative secure input method
        // 2. Mitigated by passing logger: null below, which prevents logging of the password
        // 3. Not persisted to disk or stored long-term
        // 4. Consistent with how developers manually use the dotnet dev-certs command
        var args = new StringBuilder("dev-certs https");
        args.Append($" --export-path \"{path}\"");

        if (!string.IsNullOrEmpty(normalizedFormat))
            args.Append($" --format {normalizedFormat}");

        if (!string.IsNullOrEmpty(password))
            args.Append($" --password \"{password}\"");

        // Pass logger: null to prevent DotNetCommandExecutor from logging the password
        return await DotNetCommandExecutor.ExecuteCommandAsync(args.ToString(), logger: null, machineReadable, unsafeOutput: false);
    }

    /// <summary>
    /// Initialize user secrets for a project.
    /// Creates a unique secrets ID and enables secret storage. This is the first step to using user secrets in your project.
    /// </summary>
    /// <param name="project">Project file to initialize secrets for (optional; uses current directory if not specified)</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "security")]
    [McpMeta("priority", 8.0)]
    public async partial Task<string> DotnetSecretsInit(
        string? project = null,
        bool machineReadable = false)
    {
        var args = new StringBuilder("user-secrets init");
        if (!string.IsNullOrEmpty(project)) args.Append($" --project \"{project}\"");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    /// <summary>
    /// Set a user secret value.
    /// Stores sensitive configuration outside of the project. Supports hierarchical keys (e.g., 'ConnectionStrings:DefaultConnection').
    /// DEVELOPMENT ONLY - not for production deployment.
    /// </summary>
    /// <param name="key">Secret key (supports hierarchical keys like 'ConnectionStrings:DefaultConnection')</param>
    /// <param name="value">Secret value (will not be logged for security)</param>
    /// <param name="project">Project file (optional; uses current directory if not specified)</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "security")]
    [McpMeta("priority", 9.0)]
    [McpMeta("commonlyUsed", true)]
    public async partial Task<string> DotnetSecretsSet(
        string key,
        string value,
        string? project = null,
        bool machineReadable = false)
    {
        if (string.IsNullOrWhiteSpace(key))
            return "Error: key parameter is required.";

        if (string.IsNullOrWhiteSpace(value))
            return "Error: value parameter is required.";

        // Security Note: The secret value must be passed as a command-line argument to dotnet user-secrets,
        // which is the standard .NET CLI behavior. While this stores the value temporarily in memory
        // (similar to dev-certs password handling), this is:
        // 1. Required by the .NET CLI interface - there's no alternative secure input method
        // 2. Mitigated by passing logger: null below, which prevents logging of the secret value
        // 3. Not persisted to disk in logs or command history by our code
        // 4. Consistent with how developers manually use the dotnet user-secrets command
        // 5. User secrets are ONLY for development, never for production deployment
        var args = new StringBuilder("user-secrets set");
        args.Append($" \"{key}\" \"{value}\"");
        if (!string.IsNullOrEmpty(project)) args.Append($" --project \"{project}\"");

        // Pass logger: null to prevent DotNetCommandExecutor from logging the secret value
        return await DotNetCommandExecutor.ExecuteCommandAsync(args.ToString(), logger: null, machineReadable, unsafeOutput: false);
    }

    /// <summary>
    /// List all user secrets for a project. Displays secret keys and values.
    /// Useful for debugging configuration.
    /// </summary>
    /// <param name="project">Project file (optional; uses current directory if not specified)</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "security")]
    [McpMeta("priority", 7.0)]
    public async partial Task<string> DotnetSecretsList(
        string? project = null,
        bool machineReadable = false)
    {
        var args = new StringBuilder("user-secrets list");
        if (!string.IsNullOrEmpty(project)) args.Append($" --project \"{project}\"");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    /// <summary>
    /// Remove a specific user secret by key. Deletes the secret from local storage.
    /// </summary>
    /// <param name="key">Secret key to remove</param>
    /// <param name="project">Project file (optional; uses current directory if not specified)</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "security")]
    [McpMeta("priority", 6.0)]
    public async partial Task<string> DotnetSecretsRemove(
        string key,
        string? project = null,
        bool machineReadable = false)
    {
        if (string.IsNullOrWhiteSpace(key))
            return "Error: key parameter is required.";

        var args = new StringBuilder($"user-secrets remove \"{key}\"");
        if (!string.IsNullOrEmpty(project)) args.Append($" --project \"{project}\"");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    /// <summary>
    /// Clear all user secrets for a project. Removes all stored secrets.
    /// Use this for a fresh start when debugging configuration issues.
    /// </summary>
    /// <param name="project">Project file (optional; uses current directory if not specified)</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "security")]
    [McpMeta("priority", 5.0)]
    public async partial Task<string> DotnetSecretsClear(
        string? project = null,
        bool machineReadable = false)
    {
        var args = new StringBuilder("user-secrets clear");
        if (!string.IsNullOrEmpty(project)) args.Append($" --project \"{project}\"");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    /// <summary>
    /// Install a .NET tool globally or locally to a tool manifest.
    /// Global tools are available system-wide; local tools are project-specific and tracked in .config/dotnet-tools.json.
    /// </summary>
    /// <param name="packageName">Package name of the tool (e.g., 'dotnet-ef', 'dotnet-format')</param>
    /// <param name="global">Install globally (system-wide); otherwise installs locally to tool manifest</param>
    /// <param name="version">Specific version to install</param>
    /// <param name="framework">Target framework to install for</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "tool")]
    [McpMeta("priority", 8.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("tags", JsonValue = """["tool","install","global","local","cli"]""")]
    public async partial Task<string> DotnetToolInstall(
        string packageName,
        bool global = false,
        string? version = null,
        string? framework = null,
        bool machineReadable = false)
    {
        if (string.IsNullOrWhiteSpace(packageName))
            return "Error: packageName parameter is required.";

        var args = new StringBuilder($"tool install \"{packageName}\"");
        if (global) args.Append(" --global");
        if (!string.IsNullOrEmpty(version)) args.Append($" --version {version}");
        if (!string.IsNullOrEmpty(framework)) args.Append($" --framework {framework}");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    /// <summary>
    /// List installed .NET tools.
    /// Shows global tools (system-wide) or local tools (from .config/dotnet-tools.json manifest) with their versions and commands.
    /// </summary>
    /// <param name="global">List global tools (system-wide); otherwise lists local tools from manifest</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "tool")]
    [McpMeta("priority", 7.0)]
    public async partial Task<string> DotnetToolList(
        bool global = false,
        bool machineReadable = false)
    {
        var args = "tool list";
        if (global) args += " --global";
        return await ExecuteDotNetCommand(args, machineReadable);
    }

    /// <summary>
    /// Update a .NET tool to a newer version.
    /// Can update to latest or a specific version.
    /// </summary>
    /// <param name="packageName">Package name of the tool to update</param>
    /// <param name="global">Update global tool (system-wide); otherwise updates local tool</param>
    /// <param name="version">Update to specific version; otherwise updates to latest</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "tool")]
    [McpMeta("priority", 7.0)]
    public async partial Task<string> DotnetToolUpdate(
        string packageName,
        bool global = false,
        string? version = null,
        bool machineReadable = false)
    {
        if (string.IsNullOrWhiteSpace(packageName))
            return "Error: packageName parameter is required.";

        var args = new StringBuilder($"tool update \"{packageName}\"");
        if (global) args.Append(" --global");
        if (!string.IsNullOrEmpty(version)) args.Append($" --version {version}");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    /// <summary>
    /// Uninstall a .NET tool.
    /// Removes a global tool (system-wide) or removes from local tool manifest.
    /// </summary>
    /// <param name="packageName">Package name of the tool to uninstall</param>
    /// <param name="global">Uninstall global tool (system-wide); otherwise uninstalls from local manifest</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "tool")]
    [McpMeta("priority", 6.0)]
    public async partial Task<string> DotnetToolUninstall(
        string packageName,
        bool global = false,
        bool machineReadable = false)
    {
        if (string.IsNullOrWhiteSpace(packageName))
            return "Error: packageName parameter is required.";

        var args = new StringBuilder($"tool uninstall \"{packageName}\"");
        if (global) args.Append(" --global");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    /// <summary>
    /// Restore tools from the tool manifest (.config/dotnet-tools.json).
    /// Installs all tools listed in the manifest; essential for project setup after cloning.
    /// </summary>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "tool")]
    [McpMeta("priority", 7.0)]
    public async partial Task<string> DotnetToolRestore(bool machineReadable = false)
        => await ExecuteDotNetCommand("tool restore", machineReadable);

    /// <summary>
    /// Create a .NET tool manifest file (.config/dotnet-tools.json).
    /// Required before installing local tools. Creates the manifest in the current directory or specified output location.
    /// </summary>
    /// <param name="output">Output directory for the manifest (defaults to current directory)</param>
    /// <param name="force">Force creation even if manifest already exists</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "tool")]
    [McpMeta("priority", 6.0)]
    public async partial Task<string> DotnetToolManifestCreate(
        string? output = null,
        bool force = false,
        bool machineReadable = false)
    {
        var args = new StringBuilder("new tool-manifest");
        if (!string.IsNullOrEmpty(output)) args.Append($" -o \"{output}\"");
        if (force) args.Append(" --force");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    /// <summary>
    /// Search for .NET tools on NuGet.org.
    /// Finds available tools by name or description with download counts and package information.
    /// </summary>
    /// <param name="searchTerm">Search term to find tools</param>
    /// <param name="detail">Show detailed information including description and versions</param>
    /// <param name="take">Maximum number of results to return (1-100)</param>
    /// <param name="skip">Skip the first N results for pagination</param>
    /// <param name="prerelease">Include prerelease tool versions in search</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "tool")]
    [McpMeta("priority", 6.0)]
    public async partial Task<string> DotnetToolSearch(
        string searchTerm,
        bool detail = false,
        int? take = null,
        int? skip = null,
        bool prerelease = false,
        bool machineReadable = false)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return "Error: searchTerm parameter is required.";

        var args = new StringBuilder($"tool search \"{searchTerm}\"");
        if (detail) args.Append(" --detail");
        if (take.HasValue) args.Append($" --take {take.Value}");
        if (skip.HasValue) args.Append($" --skip {skip.Value}");
        if (prerelease) args.Append(" --prerelease");
        return await ExecuteDotNetCommand(args.ToString(), machineReadable);
    }

    /// <summary>
    /// Run a .NET tool by its command name.
    /// Executes an installed local or global tool with optional arguments.
    /// </summary>
    /// <param name="toolName">Tool command name to run (e.g., 'dotnet-ef', 'dotnet-format')</param>
    /// <param name="args">Arguments to pass to the tool (e.g., 'migrations add Initial')</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [McpMeta("category", "tool")]
    [McpMeta("priority", 7.0)]
    public async partial Task<string> DotnetToolRun(
        string toolName,
        string? args = null,
        bool machineReadable = false)
    {
        if (string.IsNullOrWhiteSpace(toolName))
            return "Error: toolName parameter is required.";

        if (!string.IsNullOrEmpty(args) && !IsValidAdditionalOptions(args))
            return "Error: args contains invalid characters. Only alphanumeric characters, hyphens, underscores, dots, spaces, and equals signs are allowed.";

        var commandArgs = new StringBuilder($"tool run \"{toolName}\"");
        if (!string.IsNullOrEmpty(args)) commandArgs.Append($" -- {args}");
        return await ExecuteDotNetCommand(commandArgs.ToString(), machineReadable);
    }

    // Entity Framework Core CLI Tools
    // Note: Requires dotnet-ef tool to be installed (dotnet tool install dotnet-ef --global or locally)

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
    [McpServerTool]
    [McpMeta("category", "ef")]
    [McpMeta("priority", 9.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("tags", JsonValue = """["ef","entity-framework","migration","database","schema"]""")]
    public async partial Task<string> DotnetEfMigrationsAdd(
        string name,
        string? project = null,
        string? startupProject = null,
        string? context = null,
        string? outputDir = null,
        string? framework = null,
        bool machineReadable = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Error: name parameter is required.";

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
    [McpServerTool]
    [McpMeta("category", "ef")]
    [McpMeta("priority", 8.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("tags", JsonValue = """["ef","entity-framework","migration","database","list"]""")]
    public async partial Task<string> DotnetEfMigrationsList(
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
    [McpServerTool]
    [McpMeta("category", "ef")]
    [McpMeta("priority", 7.0)]
    [McpMeta("tags", JsonValue = """["ef","entity-framework","migration","database","remove"]""")]
    public async partial Task<string> DotnetEfMigrationsRemove(
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
    [McpServerTool]
    [McpMeta("category", "ef")]
    [McpMeta("priority", 7.0)]
    [McpMeta("tags", JsonValue = """["ef","entity-framework","migration","database","sql","script"]""")]
    public async partial Task<string> DotnetEfMigrationsScript(
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
    [McpServerTool]
    [McpMeta("category", "ef")]
    [McpMeta("priority", 9.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("tags", JsonValue = """["ef","entity-framework","database","update","migration","apply"]""")]
    public async partial Task<string> DotnetEfDatabaseUpdate(
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
    [McpServerTool]
    [McpMeta("category", "ef")]
    [McpMeta("priority", 5.0)]
    [McpMeta("tags", JsonValue = """["ef","entity-framework","database","drop","delete"]""")]
    public async partial Task<string> DotnetEfDatabaseDrop(
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
    [McpServerTool]
    [McpMeta("category", "ef")]
    [McpMeta("priority", 7.0)]
    [McpMeta("tags", JsonValue = """["ef","entity-framework","dbcontext","list"]""")]
    public async partial Task<string> DotnetEfDbContextList(
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
    [McpServerTool]
    [McpMeta("category", "ef")]
    [McpMeta("priority", 7.0)]
    [McpMeta("tags", JsonValue = """["ef","entity-framework","dbcontext","info","connection-string"]""")]
    public async partial Task<string> DotnetEfDbContextInfo(
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
    [McpServerTool]
    [McpMeta("category", "ef")]
    [McpMeta("priority", 8.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("tags", JsonValue = """["ef","entity-framework","dbcontext","scaffold","reverse-engineer","database-first"]""")]
    public async partial Task<string> DotnetEfDbContextScaffold(
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
            return "Error: connection parameter is required.";

        if (string.IsNullOrWhiteSpace(provider))
            return "Error: provider parameter is required.";

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

    /// <summary>
    /// Analyze a .csproj file to extract comprehensive project information including target frameworks, 
    /// package references, project references, and build properties. Returns structured JSON.
    /// Does not require building the project.
    /// </summary>
    /// <param name="projectPath">Path to the .csproj file to analyze</param>
    [McpServerTool]
    [McpMeta("category", "project")]
    [McpMeta("usesMSBuild", true)]
    [McpMeta("priority", 7.0)]
    [McpMeta("tags", JsonValue = """["project","analyze","introspection","metadata"]""")]
    public async partial Task<string> DotnetProjectAnalyze(string projectPath)
    {
        _logger.LogDebug("Analyzing project file: {ProjectPath}", projectPath);
        return await ProjectAnalysisHelper.AnalyzeProjectAsync(projectPath, _logger);
    }

    /// <summary>
    /// Analyze project dependencies to build a dependency graph showing direct package and project dependencies.
    /// Returns structured JSON with dependency information. For transitive dependencies, use CLI commands.
    /// </summary>
    /// <param name="projectPath">Path to the .csproj file to analyze</param>
    [McpServerTool]
    [McpMeta("category", "project")]
    [McpMeta("usesMSBuild", true)]
    [McpMeta("priority", 6.0)]
    [McpMeta("tags", JsonValue = """["project","dependencies","analyze","packages"]""")]
    public async partial Task<string> DotnetProjectDependencies(string projectPath)
    {
        _logger.LogDebug("Analyzing dependencies for: {ProjectPath}", projectPath);
        return await ProjectAnalysisHelper.AnalyzeDependenciesAsync(projectPath, _logger);
    }

    /// <summary>
    /// Validate a .csproj file for common issues, deprecated packages, and configuration problems.
    /// Returns structured JSON with errors, warnings, and recommendations. Does not require building.
    /// </summary>
    /// <param name="projectPath">Path to the .csproj file to validate</param>
    [McpServerTool]
    [McpMeta("category", "project")]
    [McpMeta("usesMSBuild", true)]
    [McpMeta("priority", 6.0)]
    [McpMeta("tags", JsonValue = """["project","validate","health-check","diagnostics"]""")]
    public async partial Task<string> DotnetProjectValidate(string projectPath)
    {
        _logger.LogDebug("Validating project: {ProjectPath}", projectPath);
        return await ProjectAnalysisHelper.ValidateProjectAsync(projectPath, _logger);
    }
}

