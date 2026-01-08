using System.ComponentModel;
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
    /// <param name="machineReadable">Return structured JSON output for both success and error responses instead of plain text</param>
    [McpServerTool]
    [Description("Query .NET SDK, runtime, template, and framework information. Supports version info, SDK/runtime listing, template operations, framework metadata, and cache metrics.")]
    [McpMeta("category", "sdk")]
    [McpMeta("priority", 9.0)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("consolidatedTool", true)]
    [McpMeta("actions", JsonValue = """["Version","Info","ListSdks","ListRuntimes","ListTemplates","SearchTemplates","TemplateInfo","ClearTemplateCache","FrameworkInfo","CacheMetrics"]""")]
    public async Task<string> DotnetSdk(
        DotnetSdkAction action,
        string? searchTerm = null,
        string? templateShortName = null,
        string? framework = null,
        bool forceReload = false,
        bool machineReadable = false)
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
            DotnetSdkAction.Version => await DotnetSdkVersion(machineReadable),
            DotnetSdkAction.Info => await DotnetSdkInfo(machineReadable),
            DotnetSdkAction.ListSdks => await DotnetSdkList(machineReadable),
            DotnetSdkAction.ListRuntimes => await DotnetRuntimeList(machineReadable),
            DotnetSdkAction.ListTemplates => await DotnetTemplateList(forceReload),
            DotnetSdkAction.SearchTemplates => await HandleSearchTemplatesAction(searchTerm, forceReload, machineReadable),
            DotnetSdkAction.TemplateInfo => await HandleTemplateInfoAction(templateShortName, forceReload, machineReadable),
            DotnetSdkAction.ClearTemplateCache => await DotnetTemplateClearCache(),
            DotnetSdkAction.FrameworkInfo => await DotnetFrameworkInfo(framework),
            DotnetSdkAction.CacheMetrics => await DotnetCacheMetrics(),
            _ => machineReadable
                ? ErrorResultFactory.ToJson(ErrorResultFactory.CreateValidationError(
                    $"Action '{action}' is not supported.",
                    parameterName: "action",
                    reason: "not supported"))
                : $"Error: Action '{action}' is not supported."
        };
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

        return await DotnetTemplateSearch(searchTerm!, forceReload);
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

        return await DotnetTemplateInfo(templateShortName!, forceReload);
    }
}
