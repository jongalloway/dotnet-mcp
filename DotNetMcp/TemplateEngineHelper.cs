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
/// This class uses SemaphoreSlim for thread-safe async caching, following .NET 9+ best practices.
/// The cache expires after 5 minutes to allow for template installations/updates.
/// All public methods are thread-safe and may be called concurrently.
/// 
/// The SemaphoreSlim instance is static and follows a singleton pattern for the application lifetime.
/// While this is appropriate for typical MCP server scenarios where the process runs until termination,
/// proper cleanup can be performed by calling <see cref="DisposeAsync"/> in testing or hosting scenarios
/// where the application may be stopped and restarted without process termination.
/// </remarks>
public class TemplateEngineHelper
{
    private static readonly SemaphoreSlim _cacheLock = new(1, 1);
    private static IEnumerable<ITemplateInfo>? _templatesCache;
    private static DateTime _cacheExpiry = DateTime.MinValue;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Get templates from cache or load them if cache is expired.
    /// Cache expires after 5 minutes to allow for template installations/updates.
    /// </summary>
    private static async Task<IEnumerable<ITemplateInfo>> GetTemplatesCachedAsync(ILogger? logger = null)
    {
        await _cacheLock.WaitAsync();
        try
        {
            if (_templatesCache == null || DateTime.UtcNow > _cacheExpiry)
            {
                logger?.LogDebug("Template cache miss - loading templates from template engine");
                var engineEnvironmentSettings = new EngineEnvironmentSettings(
                    new DefaultTemplateEngineHost("dotnet-mcp", "1.0.0"),
                    virtualizeSettings: true);

                var templatePackageManager = new TemplatePackageManager(engineEnvironmentSettings);
                _templatesCache = await templatePackageManager.GetTemplatesAsync(default);
                _cacheExpiry = DateTime.UtcNow.Add(CacheDuration);
                logger?.LogInformation("Loaded {TemplateCount} templates into cache (expires in {CacheDuration})",
                    _templatesCache.Count(), CacheDuration);
            }
            else
            {
                logger?.LogDebug("Template cache hit - returning cached templates");
            }
            return _templatesCache;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    /// <summary>
    /// Clear the template cache asynchronously. Useful after installing or uninstalling templates.
    /// </summary>
    /// <remarks>
    /// This method properly uses async/await to prevent potential deadlocks that could occur
    /// with synchronous Wait() calls on SemaphoreSlim.
    /// </remarks>
    public static async Task ClearCacheAsync(ILogger? logger = null)
    {
        await _cacheLock.WaitAsync();
        try
        {
            _templatesCache = null;
            _cacheExpiry = DateTime.MinValue;
            logger?.LogInformation("Template cache cleared");
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    /// <summary>
    /// Dispose of the SemaphoreSlim resource. This should be called when the helper is no longer needed,
    /// particularly in testing or hosting scenarios where the application may be stopped/restarted.
    /// </summary>
    /// <remarks>
    /// This method is provided to follow IDisposable best practices. In typical MCP server scenarios
    /// where the process runs until termination, calling this method is not necessary.
    /// This method is NOT thread-safe and should only be called when all other operations have completed.
    /// </remarks>
    public static void Dispose()
    {
        _cacheLock.Dispose();
    }

    /// <summary>
    /// Get a list of all installed templates with their metadata.
    /// </summary>
    public static async Task<string> GetInstalledTemplatesAsync(ILogger? logger = null)
    {
        try
        {
            // Get all installed templates from cache
            var templates = await GetTemplatesCachedAsync(logger);

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
    public static async Task<string> GetTemplateDetailsAsync(string templateShortName, ILogger? logger = null)
    {
        try
        {
            var templates = await GetTemplatesCachedAsync(logger);
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
    public static async Task<string> SearchTemplatesAsync(string searchTerm, ILogger? logger = null)
    {
        try
        {
            var templates = await GetTemplatesCachedAsync(logger);
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
    public static async Task<bool> ValidateTemplateExistsAsync(string templateShortName, ILogger? logger = null)
    {
        try
        {
            var templates = await GetTemplatesCachedAsync(logger);
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
