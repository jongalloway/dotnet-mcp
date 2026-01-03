using System.Text;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace DotNetMcp;

/// <summary>
/// Template management tools for discovering and working with .NET templates.
/// </summary>
public sealed partial class DotNetCliTools
{
    /// <summary>
    /// List all installed .NET templates with their metadata using the Template Engine. 
    /// Provides structured information about available project templates.
    /// </summary>
    /// <param name="forceReload">If true, bypasses cache and reloads templates from disk</param>
    [McpServerTool]
    [McpMeta("category", "template")]
    [McpMeta("usesTemplateEngine", true)]
    [McpMeta("commonlyUsed", true)]
    [McpMeta("priority", 10.0)]
    [McpMeta("tags", JsonValue = """["template","list","discovery","project-creation"]""")]
    public async partial Task<string> DotnetTemplateList(bool forceReload = false)
          => await TemplateEngineHelper.GetInstalledTemplatesAsync(forceReload, _logger);

    /// <summary>
    /// Search for .NET templates by name or description. Returns matching templates with their details.
    /// </summary>
    /// <param name="searchTerm">Search term to find templates (searches in name, short name, and description)</param>
    /// <param name="forceReload">If true, bypasses cache and reloads templates from disk</param>
    [McpServerTool]
    [McpMeta("category", "template")]
    [McpMeta("usesTemplateEngine", true)]
    public async partial Task<string> DotnetTemplateSearch(string searchTerm, bool forceReload = false)
        => await TemplateEngineHelper.SearchTemplatesAsync(searchTerm, forceReload, _logger);

    /// <summary>
    /// Get detailed information about a specific template including available parameters and options.
    /// </summary>
    /// <param name="templateShortName">The template short name (e.g., 'console', 'webapi', 'classlib')</param>
    /// <param name="forceReload">If true, bypasses cache and reloads templates from disk</param>
    [McpServerTool]
    [McpMeta("category", "template")]
    [McpMeta("usesTemplateEngine", true)]
    public async partial Task<string> DotnetTemplateInfo(string templateShortName, bool forceReload = false)
        => await TemplateEngineHelper.GetTemplateDetailsAsync(templateShortName, forceReload, _logger);

    /// <summary>
    /// Clear all caches (templates, SDK, runtime) to force reload from disk. 
    /// Use this after installing or uninstalling templates or SDK versions. Also resets all cache metrics.
    /// </summary>
    [McpServerTool]
    [McpMeta("category", "template")]
    [McpMeta("usesTemplateEngine", true)]
    public async partial Task<string> DotnetTemplateClearCache()
    {
        await DotNetResources.ClearAllCachesAsync();
        return "All caches (templates, SDK, runtime) and metrics cleared successfully. Next query will reload from disk.";
    }

    /// <summary>
    /// Get cache metrics showing hit/miss statistics for templates, SDK, and runtime information.
    /// </summary>
    [McpServerTool]
    [McpMeta("category", "template")]
    [McpMeta("usesTemplateEngine", true)]
    public partial Task<string> DotnetCacheMetrics()
    {
        var result = new StringBuilder();
        result.AppendLine("Cache Metrics:");
        result.AppendLine();
        result.AppendLine($"Templates: {TemplateEngineHelper.Metrics}");
        result.AppendLine($"SDK Info: {DotNetResources.GetSdkMetrics()}");
        result.AppendLine($"Runtime Info: {DotNetResources.GetRuntimeMetrics()}");
        return Task.FromResult(result.ToString());
    }
}
