using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Edge;
using Microsoft.TemplateEngine.Edge.Settings;

namespace DotNetMcp;

/// <summary>
/// Helper class for interacting with the .NET Template Engine.
/// Provides programmatic access to installed templates and their metadata.
/// Implements caching to improve performance for repeated template queries.
/// </summary>
/// <remarks>
/// This class uses CachedResourceManager for thread-safe async caching with metrics.
/// The cache expires after 5 minutes (300 seconds) to allow for template installations/updates.
/// All public methods are thread-safe and may be called concurrently.
/// </remarks>
public class TemplateEngineHelper
{
    private static readonly CachedResourceManager<IEnumerable<ITemplateInfo>> _cacheManager =
        new("Templates", defaultTtlSeconds: 300);

    /// <summary>
    /// Gets cache metrics for template caching.
    /// </summary>
    public static CacheMetrics Metrics => _cacheManager.Metrics;

    /// <summary>
    /// Load templates from the Template Engine.
    /// </summary>
    private static async Task<IEnumerable<ITemplateInfo>> LoadTemplatesAsync()
    {
        var host = new DefaultTemplateEngineHost("dotnet-mcp", "1.0.0");
        using var engineEnvironmentSettings = new EngineEnvironmentSettings(
            host,
            virtualizeSettings: false);

        using var templatePackageManager = new TemplatePackageManager(engineEnvironmentSettings);
        return await templatePackageManager.GetTemplatesAsync(default);
    }

    /// <summary>
    /// Get templates from cache or load them if cache is expired.
    /// Cache expires after 5 minutes to allow for template installations/updates.
    /// </summary>
    private static async Task<IEnumerable<ITemplateInfo>> GetTemplatesCachedAsync(bool forceReload = false, ILogger? logger = null)
    {
        var entry = await _cacheManager.GetOrLoadAsync(LoadTemplatesAsync, forceReload);
        return entry.Data;
    }

    /// <summary>
    /// Get templates from cache or load them if cache is expired (internal access for resources).
    /// This is intended for use by DotNetResources class to provide template data.
    /// </summary>
    internal static Task<IEnumerable<ITemplateInfo>> GetTemplatesCachedInternalAsync(bool forceReload = false, ILogger? logger = null)
        => GetTemplatesCachedAsync(forceReload, logger);

    /// <summary>
    /// Clear the template cache asynchronously. Useful after installing or uninstalling templates.
    /// Also resets cache metrics.
    /// </summary>
    public static async Task ClearCacheAsync(ILogger? logger = null)
    {
        await _cacheManager.ClearAsync();
        _cacheManager.ResetMetrics();
        logger?.LogInformation("Template cache and metrics cleared");
    }

    /// <summary>
    /// Get a list of all installed templates with their metadata.
    /// </summary>
    /// <param name="forceReload">If true, bypasses cache and reloads from disk.</param>
    /// <param name="logger">Optional logger instance.</param>
    public static async Task<string> GetInstalledTemplatesAsync(bool forceReload = false, ILogger? logger = null)
    {
        try
        {
            // Get all installed templates from cache
            var templates = await GetTemplatesCachedAsync(forceReload, logger);

            if (!templates.Any())
            {
                return "No templates found. This might indicate an issue accessing the template engine.";
            }

            var result = new StringBuilder();
            result.AppendLine("Installed .NET Templates:");
            result.AppendLine();
            result.AppendLine($"{"Short Name",-25} {"Language",-10} {"Type",-15} {"Description"}");
            result.AppendLine(new string('-', 100));

            foreach (var template in templates.OrderBy(t => t.ShortNameList.FirstOrDefault() ?? ""))
            {
                var shortName = template.ShortNameList.FirstOrDefault() ?? "N/A";
                var language = template.GetLanguage() ?? "Multiple";
                var type = template.GetTemplateType() ?? "Unknown";
                var description = template.Description ?? "";

                // Truncate long descriptions
                if (description.Length > 40)
                    description = description.Substring(0, 37) + "...";

                result.AppendLine($"{shortName,-25} {language,-10} {type,-15} {description}");
            }

            result.AppendLine();
            result.AppendLine($"Total templates: {templates.Count()}");

            return result.ToString();
        }
        catch (Exception ex)
        {
            return $"Error accessing template engine: {ex.Message}\n\nYou may try running 'dotnet new --list' from the command line for more information.";
        }
    }

    /// <summary>
    /// Get detailed information about a specific template.
    /// </summary>
    /// <param name="templateShortName">The template short name to query.</param>
    /// <param name="forceReload">If true, bypasses cache and reloads from disk.</param>
    /// <param name="logger">Optional logger instance.</param>
    public static async Task<string> GetTemplateDetailsAsync(string templateShortName, bool forceReload = false, ILogger? logger = null)
    {
        try
        {
            var templates = await GetTemplatesCachedAsync(forceReload, logger);
            var template = templates.FirstOrDefault(t =>
                t.ShortNameList.Any(sn => sn.Equals(templateShortName, StringComparison.OrdinalIgnoreCase)));

            if (template == null)
            {
                return $"Template '{templateShortName}' not found.\n\nUse DotnetTemplateList to see all available templates.";
            }

            var result = new StringBuilder();
            result.AppendLine($"Template: {template.Name}");
            result.AppendLine($"Short Name(s): {string.Join(", ", template.ShortNameList)}");
            result.AppendLine($"Author: {template.Author ?? "N/A"}");
            result.AppendLine($"Language: {template.GetLanguage() ?? "Multiple"}");
            result.AppendLine($"Type: {template.GetTemplateType() ?? "Unknown"}");
            result.AppendLine($"Description: {template.Description ?? "N/A"}");
            result.AppendLine();

            // Get parameters/options
            var parameters = template.ParameterDefinitions;
            if (parameters.Any())
            {
                result.AppendLine("Parameters:");
                foreach (var param in parameters.OrderBy(p => p.Name))
                {
                    result.AppendLine($"  --{param.Name}");
                    result.AppendLine($"    Description: {param.Description ?? "N/A"}");
                    result.AppendLine($"    Type: {param.DataType}");
                    if (param.DefaultValue != null)
                        result.AppendLine($"    Default: {param.DefaultValue}");
                    result.AppendLine();
                }
            }

            return result.ToString();
        }
        catch (Exception ex)
        {
            return $"Error getting template details: {ex.Message}";
        }
    }

    /// <summary>
    /// Search for templates by name or description.
    /// </summary>
    /// <param name="searchTerm">Search term to filter templates.</param>
    /// <param name="forceReload">If true, bypasses cache and reloads from disk.</param>
    /// <param name="logger">Optional logger instance.</param>
    public static async Task<string> SearchTemplatesAsync(string searchTerm, bool forceReload = false, ILogger? logger = null)
    {
        try
        {
            var templates = await GetTemplatesCachedAsync(forceReload, logger);
            var matches = templates.Where(t =>
                t.ShortNameList.Any(sn => sn.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                (t.Name?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (t.Description?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false)
            ).ToList();

            if (!matches.Any())
            {
                return $"No templates found matching '{searchTerm}'.";
            }

            var result = new StringBuilder();
            result.AppendLine($"Templates matching '{searchTerm}':");
            result.AppendLine();
            result.AppendLine($"{"Short Name",-25} {"Language",-10} {"Description"}");
            result.AppendLine(new string('-', 80));

            foreach (var template in matches.OrderBy(t => t.ShortNameList.FirstOrDefault() ?? ""))
            {
                var shortName = template.ShortNameList.FirstOrDefault() ?? "N/A";
                var language = template.GetLanguage() ?? "Multiple";
                var description = template.Description ?? "";

                if (description.Length > 35)
                    description = description.Substring(0, 32) + "...";

                result.AppendLine($"{shortName,-25} {language,-10} {description}");
            }

            result.AppendLine();
            result.AppendLine($"Found {matches.Count} matching template(s).");

            return result.ToString();
        }
        catch (Exception ex)
        {
            return $"Error searching templates: {ex.Message}";
        }
    }

    /// <summary>
    /// Validate if a template short name exists.
    /// </summary>
    /// <param name="templateShortName">The template short name to validate.</param>
    /// <param name="forceReload">If true, bypasses cache and reloads from disk.</param>
    /// <param name="logger">Optional logger instance.</param>
    public static async Task<bool> ValidateTemplateExistsAsync(string templateShortName, bool forceReload = false, ILogger? logger = null)
    {
        try
        {
            var templates = await GetTemplatesCachedAsync(forceReload, logger);
            return templates.Any(t =>
                t.ShortNameList.Any(sn => sn.Equals(templateShortName, StringComparison.OrdinalIgnoreCase)));
        }
        catch
        {
            // If template engine fails, do not assume template exists; return false to avoid false positives
            return false;
        }
    }
}

/// <summary>
/// Extension methods for template-related operations.
/// </summary>
public static class TemplateInfoExtensions
{
    public static string? GetLanguage(this ITemplateInfo template)
    {
        if (template.TagsCollection.TryGetValue("language", out var language))
            return language;
        return null;
    }

    public static string? GetTemplateType(this ITemplateInfo template)
    {
        if (template.TagsCollection.TryGetValue("type", out var type))
            return type;
        return null;
    }
}
