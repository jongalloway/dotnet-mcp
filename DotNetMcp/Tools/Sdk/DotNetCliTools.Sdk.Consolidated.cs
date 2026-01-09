using System.ComponentModel;
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
    /// <param name="framework">Specific framework to query for framework info (e.g., 'net10.0', 'net8.0')</param>
    /// <param name="forceReload">If true, bypasses cache and reloads from disk (applies to template operations)</param>
    /// <param name="workingDirectory">Working directory for command execution</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [Description("Query .NET SDK, runtime, template, and framework information. Supports version info, SDK/runtime listing, template operations, framework metadata, and cache metrics.")]
    [McpMeta("category", "sdk")]
    [McpMeta("priority", 9.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("consolidatedTool", true)]
    [McpMeta("actions", JsonValue = """["Version","Info","ListSdks","ListRuntimes","ListTemplates","SearchTemplates","TemplateInfo","ClearTemplateCache","FrameworkInfo","CacheMetrics"]""")]
    internal async Task<string> DotnetSdk(
        DotnetSdkAction action,
        string? searchTerm = null,
        string? templateShortName = null,
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
                // Use executor directly so workingDirectory is honored without changing legacy tool signatures
                DotnetSdkAction.Version => await ExecuteDotNetCommand("--version", machineReadable),
                DotnetSdkAction.Info => await ExecuteDotNetCommand("--info", machineReadable),
                DotnetSdkAction.ListSdks => await ExecuteDotNetCommand("--list-sdks", machineReadable),
                DotnetSdkAction.ListRuntimes => await ExecuteDotNetCommand("--list-runtimes", machineReadable),

                DotnetSdkAction.ListTemplates => await DotnetTemplateList(forceReload, machineReadable),
                DotnetSdkAction.SearchTemplates => await HandleSearchTemplatesAction(searchTerm, forceReload, machineReadable),
                DotnetSdkAction.TemplateInfo => await HandleTemplateInfoAction(templateShortName, forceReload, machineReadable),
                DotnetSdkAction.ClearTemplateCache => await DotnetTemplateClearCache(machineReadable),
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
          => await TemplateEngineHelper.GetInstalledTemplatesAsync(forceReload, _logger);

    /// <summary>
    /// Search for .NET templates by name or description. Returns matching templates with their details.
    /// </summary>
    /// <param name="searchTerm">Search term to find templates (searches in name, short name, and description)</param>
    /// <param name="forceReload">If true, bypasses cache and reloads templates from disk</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text (currently unused, returns same format)</param>
    [McpMeta("category", "template")]
    [McpMeta("usesTemplateEngine", true)]
    internal async Task<string> DotnetTemplateSearch(string searchTerm, bool forceReload = false, bool machineReadable = false)
        => await TemplateEngineHelper.SearchTemplatesAsync(searchTerm, forceReload, _logger);

    /// <summary>
    /// Get detailed information about a specific template including available parameters and options.
    /// </summary>
    /// <param name="templateShortName">The template short name (e.g., 'console', 'webapi', 'classlib')</param>
    /// <param name="forceReload">If true, bypasses cache and reloads templates from disk</param>
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text (currently unused, returns same format)</param>
    [McpMeta("category", "template")]
    [McpMeta("usesTemplateEngine", true)]
    internal async Task<string> DotnetTemplateInfo(string templateShortName, bool forceReload = false, bool machineReadable = false)
        => await TemplateEngineHelper.GetTemplateDetailsAsync(templateShortName, forceReload, _logger);

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
