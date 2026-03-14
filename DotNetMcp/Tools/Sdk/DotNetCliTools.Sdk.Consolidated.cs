using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace DotNetMcp;

/// <summary>
/// Consolidated .NET SDK information commands.
/// </summary>
public sealed partial class DotNetCliTools
{
    /// <summary>
    /// Query .NET SDK, runtime, template, and framework information.
    /// Provides a unified interface for all SDK-related queries including version info,
    /// installed SDKs and runtimes, template discovery, framework metadata, and cache metrics.
    /// Also supports creating or updating global.json via the ConfigureGlobalJson action.
    /// </summary>
    /// <param name="action">The SDK information operation to perform</param>
    /// <param name="searchTerm">Search query for template search operations</param>
    /// <param name="templateShortName">Template short name for template info operations (e.g., 'console', 'webapi')</param>
    /// <param name="templatePackage">Template package ID (NuGet) or path (folder or .nupkg) for template install/uninstall operations</param>
    /// <param name="templateVersion">Optional version for template package install (used as &lt;package&gt;@&lt;version&gt;)</param>
    /// <param name="nugetSource">Optional NuGet source to use for template install (e.g., a feed URL)</param>
    /// <param name="interactive">Allows install to prompt for authentication/interaction if required</param>
    /// <param name="force">Allows installing template packages from specified sources even if they override an existing template package</param>
    /// <param name="framework">Specific framework to query for framework info (e.g., 'net10.0', 'net8.0')</param>
    /// <param name="forceReload">If true, bypasses cache and reloads from disk (applies to template operations)</param>
    /// <param name="sdkVersion">SDK version to pin in global.json (e.g., '10.0.100'). Used by ConfigureGlobalJson action.</param>
    /// <param name="rollForward">SDK roll-forward policy for global.json (e.g., 'latestFeature', 'latestMinor', 'latestMajor', 'disable'). Used by ConfigureGlobalJson action.</param>
    /// <param name="testRunner">Test runner to configure in global.json (e.g., 'Microsoft.Testing.Platform'). Used by ConfigureGlobalJson action.</param>
    /// <param name="workingDirectory">Working directory for command execution</param>
    [McpServerTool(Title = ".NET SDK & Templates", Destructive = true, IconSource = "https://raw.githubusercontent.com/microsoft/fluentui-emoji/62ecdc0d7ca5c6df32148c169556bc8d3782fca4/assets/Gear/Flat/gear_flat.svg")]
    [McpMeta("category", "sdk")]
    [McpMeta("priority", 9.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("consolidatedTool", true)]
    [McpMeta("actions", JsonValue = """["Version","Info","ListSdks","ListRuntimes","ListTemplates","SearchTemplates","TemplateInfo","ClearTemplateCache","ListTemplatePacks","InstallTemplatePack","UninstallTemplatePack","FrameworkInfo","CacheMetrics","ConfigureGlobalJson"]""")]
    public async partial Task<CallToolResult> DotnetSdk(
        DotnetSdkAction action,
        string? searchTerm = null,
        string? templateShortName = null,
        string? templatePackage = null,
        string? templateVersion = null,
        string? nugetSource = null,
        bool interactive = false,
        bool force = false,
        string? framework = null,
        bool forceReload = false,
        string? sdkVersion = null,
        string? rollForward = null,
        string? testRunner = null,
        string? workingDirectory = null)
    {
        var textResult = await WithWorkingDirectoryAsync(workingDirectory, async () =>
        {
            // Validate action parameter
            if (!ParameterValidator.ValidateAction<DotnetSdkAction>(action, out var errorMessage))
            {
                return $"Error: {errorMessage}";
            }

            // Route to appropriate handler based on action
            return action switch
            {
                // Use executor directly so workingDirectory is honored without changing helper method signatures
                DotnetSdkAction.Version => await ExecuteDotNetCommand("--version"),
                DotnetSdkAction.Info => await ExecuteDotNetCommand("--info"),
                DotnetSdkAction.ListSdks => await ExecuteDotNetCommand("--list-sdks"),
                DotnetSdkAction.ListRuntimes => await ExecuteDotNetCommand("--list-runtimes"),

                DotnetSdkAction.ListTemplates => await DotnetTemplateList(forceReload),
                DotnetSdkAction.SearchTemplates => await HandleSearchTemplatesAction(searchTerm, forceReload),
                DotnetSdkAction.TemplateInfo => await HandleTemplateInfoAction(templateShortName, forceReload),
                DotnetSdkAction.ClearTemplateCache => await DotnetTemplateClearCache(),
                DotnetSdkAction.ListTemplatePacks => await DotnetTemplatePackList(),
                DotnetSdkAction.InstallTemplatePack => await HandleTemplatePackInstallAction(templatePackage, templateVersion, nugetSource, interactive, force),
                DotnetSdkAction.UninstallTemplatePack => await HandleTemplatePackUninstallAction(templatePackage),
                DotnetSdkAction.FrameworkInfo => await DotnetFrameworkInfo(framework),
                DotnetSdkAction.CacheMetrics => await DotnetCacheMetrics(),
                DotnetSdkAction.ConfigureGlobalJson => await HandleConfigureGlobalJsonAction(workingDirectory, sdkVersion, rollForward, testRunner),
                _ => $"Error: Action '{action}' is not supported."
            };
        });

        // Add structured content for key actions
        object? structured = action switch
        {
            DotnetSdkAction.Version => BuildVersionStructuredContent(textResult),
            DotnetSdkAction.ListSdks => BuildListSdksStructuredContent(textResult),
            DotnetSdkAction.ListRuntimes => BuildListRuntimesStructuredContent(textResult),
            _ => null
        };

        return StructuredContentHelper.ToCallToolResult(textResult, structured);
    }

    private async Task<string> HandleSearchTemplatesAction(string? searchTerm, bool forceReload)
    {
        // Validate required parameters
        if (!ParameterValidator.ValidateRequiredParameter(searchTerm, "searchTerm", out var errorMessage))
        {
            return $"Error: {errorMessage}";
        }

        return await DotnetTemplateSearch(searchTerm!, forceReload);
    }

    private async Task<string> HandleTemplateInfoAction(string? templateShortName, bool forceReload)
    {
        // Validate required parameters
        if (!ParameterValidator.ValidateRequiredParameter(templateShortName, "templateShortName", out var errorMessage))
        {
            return $"Error: {errorMessage}";
        }

        return await DotnetTemplateInfo(templateShortName!, forceReload);
    }

    private async Task<string> HandleTemplatePackInstallAction(
        string? templatePackage,
        string? templateVersion,
        string? nugetSource,
        bool interactive,
        bool force)
    {
        if (!ParameterValidator.ValidateTemplatePackage(templatePackage, out var packageError))
        {
            return $"Error: {packageError}";
        }

        if (!ParameterValidator.ValidateTemplatePackageVersion(templateVersion, out var versionError))
        {
            return $"Error: {versionError}";
        }

        if (!ParameterValidator.ValidateTemplateNugetSource(nugetSource, out var sourceError))
        {
            return $"Error: {sourceError}";
        }

        // Avoid ambiguous syntax: either pass version separately or use <id>@<version> in templatePackage.
        if (!string.IsNullOrWhiteSpace(templateVersion)
            && !string.IsNullOrWhiteSpace(templatePackage)
            && (templatePackage.Contains("@", StringComparison.Ordinal) || templatePackage.Contains("::", StringComparison.Ordinal)))
        {
            var message = "templatePackage already contains '@' or '::'. Provide version either via templatePackage (e.g., 'My.Templates@1.2.3') or via templateVersion, not both.";
            return $"Error: {message}";
        }

        return await DotnetTemplatePackInstall(templatePackage!, templateVersion, nugetSource, interactive, force);
    }

    private async Task<string> HandleTemplatePackUninstallAction(string? templatePackage)
    {
        // For uninstall, templatePackage is optional: if not provided, dotnet will list installed template packages.
        if (!string.IsNullOrWhiteSpace(templatePackage)
            && !ParameterValidator.ValidateTemplatePackage(templatePackage, out var packageError))
        {
            return $"Error: {packageError}";
        }

        return await DotnetTemplatePackUninstall(templatePackage);
    }

    private static readonly string[] ValidRollForwardValues =
    [
        "patch", "feature", "minor", "major",
        "latestPatch", "latestFeature", "latestMinor", "latestMajor",
        "disable"
    ];

    private Task<string> HandleConfigureGlobalJsonAction(
        string? workingDirectory,
        string? sdkVersion,
        string? rollForward,
        string? testRunner)
    {
        // At least one setting must be provided
        if (string.IsNullOrWhiteSpace(sdkVersion)
            && string.IsNullOrWhiteSpace(rollForward)
            && string.IsNullOrWhiteSpace(testRunner))
        {
            return Task.FromResult(
                "Error: At least one of sdkVersion, rollForward, or testRunner must be provided for ConfigureGlobalJson.");
        }

        // Validate rollForward value if provided
        if (!string.IsNullOrWhiteSpace(rollForward)
            && !ValidRollForwardValues.Contains(rollForward, StringComparer.OrdinalIgnoreCase))
        {
            var validList = string.Join(", ", ValidRollForwardValues.Select(v => $"'{v}'"));
            return Task.FromResult(
                $"Error: Invalid rollForward value '{rollForward}'. Valid values are: {validList}.");
        }

        // Resolve the target directory
        var targetDirectory = !string.IsNullOrWhiteSpace(workingDirectory)
            ? workingDirectory
            : DotNetCommandExecutor.WorkingDirectoryOverride.Value ?? Directory.GetCurrentDirectory();

        var globalJsonPath = Path.Join(targetDirectory, "global.json");

        try
        {
            // Read existing global.json or start with an empty object
            JsonObject root;
            if (File.Exists(globalJsonPath))
            {
                var existing = JsonNode.Parse(File.ReadAllText(globalJsonPath));
                root = existing as JsonObject ?? new JsonObject();
            }
            else
            {
                root = new JsonObject();
            }

            // Update sdk section
            if (!string.IsNullOrWhiteSpace(sdkVersion) || !string.IsNullOrWhiteSpace(rollForward))
            {
                var sdkObj = root["sdk"] as JsonObject ?? new JsonObject();
                root["sdk"] = sdkObj;

                if (!string.IsNullOrWhiteSpace(sdkVersion))
                    sdkObj["version"] = JsonValue.Create(sdkVersion);

                if (!string.IsNullOrWhiteSpace(rollForward))
                    sdkObj["rollForward"] = JsonValue.Create(rollForward);
            }

            // Update test section
            if (!string.IsNullOrWhiteSpace(testRunner))
            {
                var testObj = root["test"] as JsonObject ?? new JsonObject();
                root["test"] = testObj;

                testObj["runner"] = JsonValue.Create(testRunner);
            }

            // Write the updated file with indentation
            var jsonOptions = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
            var output = root.ToJsonString(jsonOptions);
            File.WriteAllText(globalJsonPath, output);

            var result = new StringBuilder();
            result.AppendLine($"global.json written to: {globalJsonPath}");
            result.AppendLine();
            result.Append(output);
            return Task.FromResult(result.ToString());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Task.FromResult($"Error: Failed to write global.json: {ex.Message}");
        }
    }

    // ===== Template & SDK helper methods (moved from DotNetCliTools.Template.cs and DotNetCliTools.Sdk.cs) =====
    /// <summary>
    /// List all installed .NET templates with their metadata using the Template Engine. 
    /// Provides structured information about available project templates.
    /// </summary>
    /// <param name="forceReload">If true, bypasses cache and reloads templates from disk</param>
    [McpMeta("category", "template")]
    [McpMeta("usesTemplateEngine", true)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("priority", 10.0)]
    [McpMeta("tags", JsonValue = """["template","list","discovery","project-creation"]""")]
    internal async Task<string> DotnetTemplateList(bool forceReload = false)
            => await TemplateEngineHelper.GetInstalledTemplatesAsync(forceReload, _logger);

    /// <summary>
    /// Search for .NET templates by name or description. Returns matching templates with their details.
    /// </summary>
    /// <param name="searchTerm">Search term to find templates (searches in name, short name, and description)</param>
    /// <param name="forceReload">If true, bypasses cache and reloads templates from disk</param>
    [McpMeta("category", "template")]
    [McpMeta("usesTemplateEngine", true)]
    internal async Task<string> DotnetTemplateSearch(string searchTerm, bool forceReload = false)
        => await TemplateEngineHelper.SearchTemplatesAsync(searchTerm, forceReload, _logger);

    /// <summary>
    /// Get detailed information about a specific template including available parameters and options.
    /// </summary>
    /// <param name="templateShortName">The template short name (e.g., 'console', 'webapi', 'classlib')</param>
    /// <param name="forceReload">If true, bypasses cache and reloads templates from disk</param>
    [McpMeta("category", "template")]
    [McpMeta("usesTemplateEngine", true)]
    internal async Task<string> DotnetTemplateInfo(string templateShortName, bool forceReload = false)
        => await TemplateEngineHelper.GetTemplateDetailsAsync(templateShortName, forceReload, _logger);

    /// <summary>
    /// Clear all caches (templates, SDK, runtime) to force reload from disk. 
    /// Use this after installing or uninstalling templates or SDK versions. Also resets all cache metrics.
    /// </summary>
    [McpMeta("category", "template")]
    [McpMeta("usesTemplateEngine", true)]
    internal async Task<string> DotnetTemplateClearCache()
    {
        await DotNetResources.ClearAllCachesAsync();
        return "All caches (templates, SDK, runtime) and metrics cleared successfully. Next query will reload from disk.";
    }

    /// <summary>
    /// Install a template package/pack using <c>dotnet new install</c>.
    /// </summary>
    /// <param name="templatePackage">NuGet package ID or path to folder/.nupkg</param>
    /// <param name="templateVersion">Optional version to install (used as &lt;id&gt;@&lt;version&gt;)</param>
    /// <param name="nugetSource">Optional NuGet source to use (feed URL or local source)</param>
    /// <param name="interactive">Allow user interaction for authentication</param>
    /// <param name="force">Allow overriding a template pack from another source</param>
    [McpMeta("category", "template")]
    [McpMeta("commonlyUsed", true)]
    internal async Task<string> DotnetTemplatePackInstall(
        string templatePackage,
        string? templateVersion = null,
        string? nugetSource = null,
        bool interactive = false,
        bool force = false)
    {
        var packageExpression = !string.IsNullOrWhiteSpace(templateVersion)
            ? $"{templatePackage}@{templateVersion}"
            : templatePackage;

        var args = new StringBuilder($"new install \"{packageExpression}\"");
        if (!string.IsNullOrWhiteSpace(nugetSource)) args.Append($" --nuget-source \"{nugetSource}\"");
        if (interactive) args.Append(" --interactive");
        if (force) args.Append(" --force");

        var result = await ExecuteDotNetCommand(args.ToString());

        // Installing templates changes the template engine state. Clear internal caches so follow-up template queries refresh.
        await DotNetResources.ClearAllCachesAsync();

        return result;
    }

    /// <summary>
    /// Uninstall a template package/pack using <c>dotnet new uninstall</c>.
    /// If no package is specified, lists all installed template packages.
    /// </summary>
    /// <param name="templatePackage">NuGet package ID (without version) or path to folder to uninstall</param>
    [McpMeta("category", "template")]
    internal async Task<string> DotnetTemplatePackUninstall(
        string? templatePackage = null)
    {
        var args = new StringBuilder("new uninstall");
        if (!string.IsNullOrWhiteSpace(templatePackage)) args.Append($" \"{templatePackage}\"");

        var result = await ExecuteDotNetCommand(args.ToString());

        // Uninstalling templates changes the template engine state. Clear internal caches so follow-up template queries refresh.
        await DotNetResources.ClearAllCachesAsync();

        return result;
    }

    /// <summary>
    /// List installed template packages/packs.
    /// Under the hood this runs <c>dotnet new uninstall</c> with no arguments, which lists installed packs.
    /// </summary>
    [McpMeta("category", "template")]
    internal async Task<string> DotnetTemplatePackList()
    {
        // NOTE: We intentionally do not clear caches here; this is a read-only listing.
        return await ExecuteDotNetCommand("new uninstall");
    }

    /// <summary>
    /// Get cache metrics showing hit/miss statistics for templates, SDK, and runtime information.
    /// </summary>
    [McpMeta("category", "template")]
    [McpMeta("usesTemplateEngine", true)]
    internal Task<string> DotnetCacheMetrics()
    {
        var result = new StringBuilder();
        result.AppendLine("Cache Metrics:");
        result.AppendLine();
        result.AppendLine($"Templates: {TemplateEngineHelper.Metrics}");
        result.AppendLine($"SDK Info: {DotNetResources.GetSdkMetrics()}");
        result.AppendLine($"Runtime Info: {DotNetResources.GetRuntimeMetrics()}");
        return Task.FromResult(result.ToString());
    }

    /// <summary>
    /// Get information about .NET framework versions, including which are LTS releases. 
    /// Useful for understanding framework compatibility.
    /// </summary>
    /// <param name="framework">Optional: specific framework to get info about (e.g., 'net8.0', 'net6.0')</param>
    [McpMeta("category", "framework")]
    [McpMeta("usesFrameworkHelper", true)]
    internal async Task<string> DotnetFrameworkInfo(string? framework = null)
    {
        var result = new StringBuilder();

        if (!string.IsNullOrEmpty(framework))
        {
            result.AppendLine($"Framework: {framework}");
            result.AppendLine($"Description: {FrameworkHelper.GetFrameworkDescription(framework)}");
            result.AppendLine($"Is LTS: {FrameworkHelper.IsLtsFramework(framework)}");
            result.AppendLine($"Is Modern .NET: {FrameworkHelper.IsModernNet(framework)}");
            result.AppendLine($"Is .NET Core: {FrameworkHelper.IsNetCore(framework)}");
            result.AppendLine($"Is .NET Framework: {FrameworkHelper.IsNetFramework(framework)}");
            result.AppendLine($"Is .NET Standard: {FrameworkHelper.IsNetStandard(framework)}");
        }
        else
        {
            var supportedModernFrameworks = FrameworkHelper.GetSupportedModernFrameworks().ToList();

            // Only show preview TFMs when the SDK major version is installed.
            try
            {
                var sdkList = await DotNetCommandExecutor.ExecuteCommandForResourceAsync("--list-sdks", _logger);
                var hasNet11Sdk = sdkList
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Any(line => line.TrimStart().StartsWith("11.", StringComparison.Ordinal));
                if (hasNet11Sdk)
                {
                    supportedModernFrameworks.Insert(0, DotNetSdkConstants.TargetFrameworks.Net110);
                }
            }
            catch (Exception ex)
            {
                // If SDK discovery fails, fall back to stable list.
                _logger.LogDebug(ex, "Failed to discover installed .NET SDKs. Falling back to stable framework list.");
            }

            result.AppendLine("Modern .NET Frameworks (5.0+):");
            foreach (var fw in supportedModernFrameworks)
            {
                var ltsMarker = FrameworkHelper.IsLtsFramework(fw) ? " (LTS)" : string.Empty;
                result.AppendLine($"  {fw}{ltsMarker} - {FrameworkHelper.GetFrameworkDescription(fw)}");
            }

            result.AppendLine();
            result.AppendLine(".NET Core Frameworks:");
            foreach (var fw in FrameworkHelper.GetSupportedNetCoreFrameworks())
            {
                var ltsMarker = FrameworkHelper.IsLtsFramework(fw) ? " (LTS)" : string.Empty;
                result.AppendLine($"  {fw}{ltsMarker} - {FrameworkHelper.GetFrameworkDescription(fw)}");
            }

            result.AppendLine();
            result.AppendLine($"Latest Recommended: {FrameworkHelper.GetLatestRecommendedFramework()}");
            result.AppendLine($"Latest LTS: {FrameworkHelper.GetLatestLtsFramework()}");
        }

        return result.ToString();
    }

    /// <summary>
    /// Get information about installed .NET SDKs and runtimes.
    /// </summary>
    [McpMeta("category", "sdk")]
    [McpMeta("priority", 6.0)]
    internal async Task<string> DotnetSdkInfo()
        => await ExecuteDotNetCommand("--info");

    /// <summary>
    /// Get the version of the .NET SDK.
    /// </summary>
    [McpMeta("category", "sdk")]
    [McpMeta("priority", 6.0)]
    internal async Task<string> DotnetSdkVersion()
        => await ExecuteDotNetCommand("--version");

    /// <summary>
    /// List installed .NET SDKs.
    /// </summary>
    [McpMeta("category", "sdk")]
    [McpMeta("priority", 6.0)]
    internal async Task<string> DotnetSdkList()
        => await ExecuteDotNetCommand("--list-sdks");

    /// <summary>
    /// List installed .NET runtimes.
    /// </summary>
    [McpMeta("category", "sdk")]
    [McpMeta("priority", 6.0)]
    internal async Task<string> DotnetRuntimeList()
        => await ExecuteDotNetCommand("--list-runtimes");

    private static object? BuildVersionStructuredContent(string textResult)
    {
        // Extract version from output like "10.0.100\nExit Code: 0"
        var lines = textResult.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var versionLine = lines.FirstOrDefault(l => !l.StartsWith("Exit Code:", StringComparison.OrdinalIgnoreCase)
            && !l.StartsWith("Error", StringComparison.OrdinalIgnoreCase)
            && !l.StartsWith("Command:", StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrWhiteSpace(versionLine)) return null;
        var version = versionLine.Trim();
        return new { version };
    }

    private static object? BuildListSdksStructuredContent(string textResult)
    {
        var lines = textResult.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var sdks = lines
            .Where(l => !l.StartsWith("Exit Code:", StringComparison.OrdinalIgnoreCase)
                && !l.StartsWith("Error", StringComparison.OrdinalIgnoreCase)
                && l.TrimStart().Length > 0 && char.IsDigit(l.TrimStart()[0]))
            .Select(l =>
            {
                var parts = l.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                var ver = parts.Length > 0 ? parts[0] : l.Trim();
                var path = parts.Length > 1 ? parts[1].Trim('[', ']', ' ') : null;
                return new { version = ver, path };
            })
            .ToArray();
        return new { sdks };
    }

    private static object? BuildListRuntimesStructuredContent(string textResult)
    {
        var lines = textResult.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var runtimes = lines
            .Where(l => !l.StartsWith("Exit Code:", StringComparison.OrdinalIgnoreCase)
                && !l.StartsWith("Error", StringComparison.OrdinalIgnoreCase)
                && !l.StartsWith("Command:", StringComparison.OrdinalIgnoreCase)
                && l.TrimStart().Length > 0 && char.IsAsciiLetter(l.TrimStart()[0]))
            .Select(l =>
            {
                var parts = l.Trim().Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
                var name = parts.Length > 0 ? parts[0] : string.Empty;
                var ver = parts.Length > 1 ? parts[1] : string.Empty;
                var path = parts.Length > 2 ? parts[2].Trim('[', ']', ' ') : null;
                return new { name, version = ver, path };
            })
            .ToArray();
        return new { runtimes };
    }
}
