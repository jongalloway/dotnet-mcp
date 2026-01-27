using System.Text;
using Microsoft.Extensions.Logging;
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
    /// </summary>
    /// <param name="action">The SDK information operation to perform</param>
    /// <param name="searchTerm">Search query for template search operations</param>
    /// <param name="templateShortName">Template short name for template info operations (e.g., 'console', 'webapi')</param>
    /// <param name="templatePackage">Template package ID (NuGet) or path (folder or .nupkg) for template install/uninstall operations</param>
    /// <param name="templateVersion">Optional version for template package install (used as &lt;package&gt;::&lt;version&gt;)</param>
    /// <param name="nugetSource">Optional NuGet source to use for template install (e.g., a feed URL)</param>
    /// <param name="interactive">Allows install to prompt for authentication/interaction if required</param>
    /// <param name="force">Allows installing template packages from specified sources even if they override an existing template package</param>
    /// <param name="framework">Specific framework to query for framework info (e.g., 'net10.0', 'net8.0')</param>
    /// <param name="forceReload">If true, bypasses cache and reloads from disk (applies to template operations)</param>
    /// <param name="workingDirectory">Working directory for command execution</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool(IconSource = "https://raw.githubusercontent.com/microsoft/fluentui-emoji/62ecdc0d7ca5c6df32148c169556bc8d3782fca4/assets/Gear/Flat/gear_flat.svg")]
    [McpMeta("category", "sdk")]
    [McpMeta("priority", 9.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("consolidatedTool", true)]
    [McpMeta("actions", JsonValue = """["Version","Info","ListSdks","ListRuntimes","ListTemplates","SearchTemplates","TemplateInfo","ClearTemplateCache","ListTemplatePacks","InstallTemplatePack","UninstallTemplatePack","FrameworkInfo","CacheMetrics"]""")]
    public async partial Task<string> DotnetSdk(
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
        string? workingDirectory = null,
        bool machineReadable = false)
    {
        return await WithWorkingDirectoryAsync(workingDirectory, async () =>
        {
            // Validate action parameter
            if (!ParameterValidator.ValidateAction<DotnetSdkAction>(action, out var errorMessage))
            {
                if (machineReadable)
                {
                    var validActions = Enum.GetNames(typeof(DotnetSdkAction));
                    var error = ErrorResultFactory.CreateActionValidationError(
                        action.ToString(),
                        validActions,
                        toolName: "dotnet_sdk");
                    return ErrorResultFactory.ToJson(error);
                }
                return $"Error: {errorMessage}";
            }

            // Route to appropriate handler based on action
            return action switch
            {
                // Use executor directly so workingDirectory is honored without changing helper method signatures
                DotnetSdkAction.Version => await ExecuteDotNetCommand("--version", machineReadable),
                DotnetSdkAction.Info => await ExecuteDotNetCommand("--info", machineReadable),
                DotnetSdkAction.ListSdks => await ExecuteDotNetCommand("--list-sdks", machineReadable),
                DotnetSdkAction.ListRuntimes => await ExecuteDotNetCommand("--list-runtimes", machineReadable),

                DotnetSdkAction.ListTemplates => await DotnetTemplateList(forceReload, machineReadable),
                DotnetSdkAction.SearchTemplates => await HandleSearchTemplatesAction(searchTerm, forceReload, machineReadable),
                DotnetSdkAction.TemplateInfo => await HandleTemplateInfoAction(templateShortName, forceReload, machineReadable),
                DotnetSdkAction.ClearTemplateCache => await DotnetTemplateClearCache(machineReadable),
                DotnetSdkAction.ListTemplatePacks => await DotnetTemplatePackList(machineReadable),
                DotnetSdkAction.InstallTemplatePack => await HandleTemplatePackInstallAction(templatePackage, templateVersion, nugetSource, interactive, force, machineReadable),
                DotnetSdkAction.UninstallTemplatePack => await HandleTemplatePackUninstallAction(templatePackage, machineReadable),
                DotnetSdkAction.FrameworkInfo => await DotnetFrameworkInfo(framework, machineReadable),
                DotnetSdkAction.CacheMetrics => await DotnetCacheMetrics(machineReadable),
                _ => machineReadable
                    ? ErrorResultFactory.ToJson(ErrorResultFactory.CreateValidationError(
                        $"Action '{action}' is not supported.",
                        parameterName: "action",
                        reason: "not supported"))
                    : $"Error: Action '{action}' is not supported."
            };
        });
    }

    private async Task<string> HandleSearchTemplatesAction(string? searchTerm, bool forceReload, bool machineReadable)
    {
        // Validate required parameters
        if (!ParameterValidator.ValidateRequiredParameter(searchTerm, "searchTerm", out var errorMessage))
        {
            if (machineReadable)
            {
                var error = ErrorResultFactory.CreateValidationError(
                    errorMessage!,
                    parameterName: "searchTerm",
                    reason: "required");
                return ErrorResultFactory.ToJson(error);
            }
            return $"Error: {errorMessage}";
        }

        return await DotnetTemplateSearch(searchTerm!, forceReload, machineReadable);
    }

    private async Task<string> HandleTemplateInfoAction(string? templateShortName, bool forceReload, bool machineReadable)
    {
        // Validate required parameters
        if (!ParameterValidator.ValidateRequiredParameter(templateShortName, "templateShortName", out var errorMessage))
        {
            if (machineReadable)
            {
                var error = ErrorResultFactory.CreateValidationError(
                    errorMessage!,
                    parameterName: "templateShortName",
                    reason: "required");
                return ErrorResultFactory.ToJson(error);
            }
            return $"Error: {errorMessage}";
        }

        return await DotnetTemplateInfo(templateShortName!, forceReload, machineReadable);
    }

    private async Task<string> HandleTemplatePackInstallAction(
        string? templatePackage,
        string? templateVersion,
        string? nugetSource,
        bool interactive,
        bool force,
        bool machineReadable)
    {
        if (!ParameterValidator.ValidateTemplatePackage(templatePackage, out var packageError))
        {
            if (machineReadable)
            {
                var error = ErrorResultFactory.CreateValidationError(
                    packageError!,
                    parameterName: "templatePackage",
                    reason: "required");
                return ErrorResultFactory.ToJson(error);
            }
            return $"Error: {packageError}";
        }

        if (!ParameterValidator.ValidateTemplatePackageVersion(templateVersion, out var versionError))
        {
            if (machineReadable)
            {
                var error = ErrorResultFactory.CreateValidationError(
                    versionError!,
                    parameterName: "templateVersion",
                    reason: "invalid format");
                return ErrorResultFactory.ToJson(error);
            }
            return $"Error: {versionError}";
        }

        if (!ParameterValidator.ValidateTemplateNugetSource(nugetSource, out var sourceError))
        {
            if (machineReadable)
            {
                var error = ErrorResultFactory.CreateValidationError(
                    sourceError!,
                    parameterName: "nugetSource",
                    reason: "invalid format");
                return ErrorResultFactory.ToJson(error);
            }
            return $"Error: {sourceError}";
        }

        // Avoid ambiguous syntax: either pass version separately or use <id>::<version> in templatePackage.
        if (!string.IsNullOrWhiteSpace(templateVersion)
            && !string.IsNullOrWhiteSpace(templatePackage)
            && templatePackage.Contains("::", StringComparison.Ordinal))
        {
            var message = "templatePackage already contains '::'. Provide version either via templatePackage (e.g., 'My.Templates::1.2.3') or via templateVersion, not both.";
            if (machineReadable)
            {
                var error = ErrorResultFactory.CreateValidationError(message, parameterName: "templateVersion", reason: "conflict");
                return ErrorResultFactory.ToJson(error);
            }
            return $"Error: {message}";
        }

        return await DotnetTemplatePackInstall(templatePackage!, templateVersion, nugetSource, interactive, force, machineReadable);
    }

    private async Task<string> HandleTemplatePackUninstallAction(string? templatePackage, bool machineReadable)
    {
        // For uninstall, templatePackage is optional: if not provided, dotnet will list installed template packages.
        if (!string.IsNullOrWhiteSpace(templatePackage)
            && !ParameterValidator.ValidateTemplatePackage(templatePackage, out var packageError))
        {
            if (machineReadable)
            {
                var error = ErrorResultFactory.CreateValidationError(
                    packageError!,
                    parameterName: "templatePackage",
                    reason: "invalid format");
                return ErrorResultFactory.ToJson(error);
            }
            return $"Error: {packageError}";
        }

        return await DotnetTemplatePackUninstall(templatePackage, machineReadable);
    }

    // ===== Template & SDK helper methods (moved from DotNetCliTools.Template.cs and DotNetCliTools.Sdk.cs) =====
    /// <summary>
    /// List all installed .NET templates with their metadata using the Template Engine. 
    /// Provides structured information about available project templates.
    /// </summary>
    /// <param name="forceReload">If true, bypasses cache and reloads templates from disk</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text (currently unused, returns same format)</param>
    [McpMeta("category", "template")]
    [McpMeta("usesTemplateEngine", true)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("priority", 10.0)]
    [McpMeta("tags", JsonValue = """["template","list","discovery","project-creation"]""")]
    internal async Task<string> DotnetTemplateList(bool forceReload = false, bool machineReadable = false)
            => await TemplateEngineHelper.GetInstalledTemplatesAsync(forceReload, _logger, machineReadable);

    /// <summary>
    /// Search for .NET templates by name or description. Returns matching templates with their details.
    /// </summary>
    /// <param name="searchTerm">Search term to find templates (searches in name, short name, and description)</param>
    /// <param name="forceReload">If true, bypasses cache and reloads templates from disk</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text (currently unused, returns same format)</param>
    [McpMeta("category", "template")]
    [McpMeta("usesTemplateEngine", true)]
    internal async Task<string> DotnetTemplateSearch(string searchTerm, bool forceReload = false, bool machineReadable = false)
        => await TemplateEngineHelper.SearchTemplatesAsync(searchTerm, forceReload, _logger, machineReadable);

    /// <summary>
    /// Get detailed information about a specific template including available parameters and options.
    /// </summary>
    /// <param name="templateShortName">The template short name (e.g., 'console', 'webapi', 'classlib')</param>
    /// <param name="forceReload">If true, bypasses cache and reloads templates from disk</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text (currently unused, returns same format)</param>
    [McpMeta("category", "template")]
    [McpMeta("usesTemplateEngine", true)]
    internal async Task<string> DotnetTemplateInfo(string templateShortName, bool forceReload = false, bool machineReadable = false)
        => await TemplateEngineHelper.GetTemplateDetailsAsync(templateShortName, forceReload, _logger, machineReadable);

    /// <summary>
    /// Clear all caches (templates, SDK, runtime) to force reload from disk. 
    /// Use this after installing or uninstalling templates or SDK versions. Also resets all cache metrics.
    /// </summary>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text (currently unused, returns same format)</param>
    [McpMeta("category", "template")]
    [McpMeta("usesTemplateEngine", true)]
    internal async Task<string> DotnetTemplateClearCache(bool machineReadable = false)
    {
        await DotNetResources.ClearAllCachesAsync();
        return "All caches (templates, SDK, runtime) and metrics cleared successfully. Next query will reload from disk.";
    }

    /// <summary>
    /// Install a template package/pack using <c>dotnet new install</c>.
    /// </summary>
    /// <param name="templatePackage">NuGet package ID or path to folder/.nupkg</param>
    /// <param name="templateVersion">Optional version to install (used as &lt;id&gt;::&lt;version&gt;)</param>
    /// <param name="nugetSource">Optional NuGet source to use (feed URL or local source)</param>
    /// <param name="interactive">Allow user interaction for authentication</param>
    /// <param name="force">Allow overriding a template pack from another source</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpMeta("category", "template")]
    [McpMeta("commonlyUsed", true)]
    internal async Task<string> DotnetTemplatePackInstall(
        string templatePackage,
        string? templateVersion = null,
        string? nugetSource = null,
        bool interactive = false,
        bool force = false,
        bool machineReadable = false)
    {
        var packageExpression = !string.IsNullOrWhiteSpace(templateVersion)
            ? $"{templatePackage}::{templateVersion}"
            : templatePackage;

        var args = new StringBuilder($"new install \"{packageExpression}\"");
        if (!string.IsNullOrWhiteSpace(nugetSource)) args.Append($" --nuget-source \"{nugetSource}\"");
        if (interactive) args.Append(" --interactive");
        if (force) args.Append(" --force");

        var result = await ExecuteDotNetCommand(args.ToString(), machineReadable);

        // Installing templates changes the template engine state. Clear internal caches so follow-up template queries refresh.
        await DotNetResources.ClearAllCachesAsync();

        return result;
    }

    /// <summary>
    /// Uninstall a template package/pack using <c>dotnet new uninstall</c>.
    /// If no package is specified, lists all installed template packages.
    /// </summary>
    /// <param name="templatePackage">NuGet package ID (without version) or path to folder to uninstall</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpMeta("category", "template")]
    internal async Task<string> DotnetTemplatePackUninstall(
        string? templatePackage = null,
        bool machineReadable = false)
    {
        var args = new StringBuilder("new uninstall");
        if (!string.IsNullOrWhiteSpace(templatePackage)) args.Append($" \"{templatePackage}\"");

        var result = await ExecuteDotNetCommand(args.ToString(), machineReadable);

        // Uninstalling templates changes the template engine state. Clear internal caches so follow-up template queries refresh.
        await DotNetResources.ClearAllCachesAsync();

        return result;
    }

    /// <summary>
    /// List installed template packages/packs.
    /// Under the hood this runs <c>dotnet new uninstall</c> with no arguments, which lists installed packs.
    /// </summary>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpMeta("category", "template")]
    internal async Task<string> DotnetTemplatePackList(bool machineReadable = false)
    {
        // NOTE: We intentionally do not clear caches here; this is a read-only listing.
        return await ExecuteDotNetCommand("new uninstall", machineReadable);
    }

    /// <summary>
    /// Get cache metrics showing hit/miss statistics for templates, SDK, and runtime information.
    /// </summary>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text (currently unused, returns same format)</param>
    [McpMeta("category", "template")]
    [McpMeta("usesTemplateEngine", true)]
    internal Task<string> DotnetCacheMetrics(bool machineReadable = false)
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
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text (currently unused, returns same format)</param>
    [McpMeta("category", "framework")]
    [McpMeta("usesFrameworkHelper", true)]
    internal async Task<string> DotnetFrameworkInfo(string? framework = null, bool machineReadable = false)
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
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpMeta("category", "sdk")]
    [McpMeta("priority", 6.0)]
    internal async Task<string> DotnetSdkInfo(bool machineReadable = false)
        => await ExecuteDotNetCommand("--info", machineReadable);

    /// <summary>
    /// Get the version of the .NET SDK.
    /// </summary>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpMeta("category", "sdk")]
    [McpMeta("priority", 6.0)]
    internal async Task<string> DotnetSdkVersion(bool machineReadable = false)
        => await ExecuteDotNetCommand("--version", machineReadable);

    /// <summary>
    /// List installed .NET SDKs.
    /// </summary>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpMeta("category", "sdk")]
    [McpMeta("priority", 6.0)]
    internal async Task<string> DotnetSdkList(bool machineReadable = false)
        => await ExecuteDotNetCommand("--list-sdks", machineReadable);

    /// <summary>
    /// List installed .NET runtimes.
    /// </summary>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpMeta("category", "sdk")]
    [McpMeta("priority", 6.0)]
    internal async Task<string> DotnetRuntimeList(bool machineReadable = false)
        => await ExecuteDotNetCommand("--list-runtimes", machineReadable);
}
