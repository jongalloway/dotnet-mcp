using System.ComponentModel;
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

    // Test hooks (internal for DotNetMcp.Tests via InternalsVisibleTo)
    internal static Func<Task<IEnumerable<ITemplateInfo>>>? LoadTemplatesOverride { get; set; }

    internal static Func<string, ILogger?, Task<string>> ExecuteDotNetForTemplatesAsync { get; set; } =
        (args, logger) => DotNetCommandExecutor.ExecuteCommandForResourceAsync(args, logger);

    /// <summary>
    /// Gets cache metrics for template caching.
    /// </summary>
    public static CacheMetrics Metrics => _cacheManager.Metrics;

    /// <summary>
    /// Load templates from the Template Engine.
    /// </summary>
    private static async Task<IEnumerable<ITemplateInfo>> LoadTemplatesAsync()
    {
        if (LoadTemplatesOverride is not null)
        {
            return await LoadTemplatesOverride();
        }

        var host = new DefaultTemplateEngineHost("dotnet-mcp", "1.0.0");
        using var engineEnvironmentSettings = new EngineEnvironmentSettings(
            host,
            virtualizeSettings: true);

        using var templatePackageManager = new TemplatePackageManager(engineEnvironmentSettings);
        return await templatePackageManager.GetTemplatesAsync(default);
    }

    private static async Task<string?> TryGetDotnetNewListOutputAsync(string? templateNameFilter, ILogger? logger)
    {
        try
        {
            // Keep output parseable and readable by limiting columns.
            // Note: Template Name and Short Name are always included.
            var columns = "--columns author --columns language --columns type --columns tags";
            var args = string.IsNullOrWhiteSpace(templateNameFilter)
                ? $"new list {columns}"
                : $"new list \"{templateNameFilter}\" {columns}";

            // Use dotnet CLI as a fallback for environments where the Template Engine API cannot enumerate templates.
            // This is still consistent with the server's hybrid approach: SDK integration first, CLI execution fallback.
            return await ExecuteDotNetForTemplatesAsync(args, logger);
        }
        catch (InvalidOperationException ex)
        {
            logger?.LogDebug(ex, "Invalid operation during template query via 'dotnet new list' fallback");
            return null;
        }
        catch (Win32Exception ex)
        {
            logger?.LogDebug(ex, "Process execution failed for 'dotnet new list' fallback");
            return null;
        }
        catch (OperationCanceledException ex)
        {
            logger?.LogDebug(ex, "Template query cancelled");
            return null;
        }
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
    /// <param name="machineReadable">When true, returns JSON output consistent with other MCP tools.</param>
    public static async Task<string> GetInstalledTemplatesAsync(bool forceReload = false, ILogger? logger = null, bool machineReadable = false)
    {
        try
        {
            // Get all installed templates from cache
            var templates = await GetTemplatesCachedAsync(forceReload, logger);

            if (!templates.Any())
            {
                var cliOutput = await TryGetDotnetNewListOutputAsync(templateNameFilter: null, logger);
                if (!string.IsNullOrWhiteSpace(cliOutput))
                {
                    var text = $"Installed .NET Templates (from 'dotnet new list'):\n\n{cliOutput.TrimEnd()}";
                    if (machineReadable)
                    {
                        return ErrorResultFactory.ToJson(
                            ErrorResultFactory.CreateResult(
                                text,
                                error: string.Empty,
                                exitCode: 0,
                                command: "dotnet new list --columns author --columns language --columns type --columns tags"));
                    }

                    return text;
                }

                var message = "No templates found. This might indicate an issue accessing the template engine.";
                if (machineReadable)
                {
                    return ErrorResultFactory.ToJson(
                        ErrorResultFactory.CreateResult(message, error: string.Empty, exitCode: 0, command: "template-engine"));
                }

                return message;
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

            var output = result.ToString();
            if (machineReadable)
            {
                return ErrorResultFactory.ToJson(
                    ErrorResultFactory.CreateResult(output, error: string.Empty, exitCode: 0, command: "template-engine"));
            }

            return output;
        }
        catch (Exception ex)
        {
            var message = $"Error accessing template engine: {ex.Message}\n\nYou may try running 'dotnet new --list' from the command line for more information.";
            if (machineReadable)
            {
                return ErrorResultFactory.ToJson(
                    ErrorResultFactory.CreateResult(output: string.Empty, error: message, exitCode: 1, command: "template-engine"));
            }

            return message;
        }
    }

    /// <summary>
    /// Get detailed information about a specific template.
    /// </summary>
    /// <param name="templateShortName">The template short name to query.</param>
    /// <param name="forceReload">If true, bypasses cache and reloads from disk.</param>
    /// <param name="logger">Optional logger instance.</param>
    /// <param name="machineReadable">When true, returns JSON output consistent with other MCP tools.</param>
    public static async Task<string> GetTemplateDetailsAsync(string templateShortName, bool forceReload = false, ILogger? logger = null, bool machineReadable = false)
    {
        try
        {
            var templates = await GetTemplatesCachedAsync(forceReload, logger);
            var template = templates.FirstOrDefault(t =>
                t.ShortNameList.Any(sn => sn.Equals(templateShortName, StringComparison.OrdinalIgnoreCase)));

            if (template == null)
            {
                // If the Template Engine API cannot enumerate templates in this environment,
                // fall back to the CLI help for the template short name.
                if (!templates.Any())
                {
                    try
                    {
                        var help = await ExecuteDotNetForTemplatesAsync($"new {templateShortName} --help", logger);
                        if (!string.IsNullOrWhiteSpace(help))
                        {
                            var text = $"Template help (from 'dotnet new {templateShortName} --help'):\n\n{help.TrimEnd()}";
                            if (machineReadable)
                            {
                                return ErrorResultFactory.ToJson(
                                    ErrorResultFactory.CreateResult(text, error: string.Empty, exitCode: 0, command: $"dotnet new {templateShortName} --help"));
                            }

                            return text;
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        // Ignore and fall through to the normal not-found message.
                    }
                    catch (Win32Exception)
                    {
                        // Ignore and fall through to the normal not-found message.
                    }
                    catch (OperationCanceledException)
                    {
                        // Ignore and fall through to the normal not-found message.
                    }
                }

                var notFound = $"Template '{templateShortName}' not found.\n\nUse DotnetTemplateList to see all available templates.";
                if (machineReadable)
                {
                    return ErrorResultFactory.ToJson(
                        ErrorResultFactory.CreateResult(output: string.Empty, error: notFound, exitCode: 1, command: "template-engine"));
                }

                return notFound;
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

            var output = result.ToString();
            if (machineReadable)
            {
                return ErrorResultFactory.ToJson(
                    ErrorResultFactory.CreateResult(output, error: string.Empty, exitCode: 0, command: "template-engine"));
            }

            return output;
        }
        catch (Exception ex)
        {
            var message = $"Error getting template details: {ex.Message}";
            if (machineReadable)
            {
                return ErrorResultFactory.ToJson(
                    ErrorResultFactory.CreateResult(output: string.Empty, error: message, exitCode: 1, command: "template-engine"));
            }

            return message;
        }
    }

    /// <summary>
    /// Search for templates by name or description.
    /// </summary>
    /// <param name="searchTerm">Search term to filter templates.</param>
    /// <param name="forceReload">If true, bypasses cache and reloads from disk.</param>
    /// <param name="logger">Optional logger instance.</param>
    /// <param name="machineReadable">When true, returns JSON output consistent with other MCP tools.</param>
    public static async Task<string> SearchTemplatesAsync(string searchTerm, bool forceReload = false, ILogger? logger = null, bool machineReadable = false)
    {
        try
        {
            var templates = await GetTemplatesCachedAsync(forceReload, logger);

            if (!templates.Any())
            {
                var cliOutput = await TryGetDotnetNewListOutputAsync(searchTerm, logger);
                if (!string.IsNullOrWhiteSpace(cliOutput))
                {
                    var text = $"Templates matching '{searchTerm}' (from 'dotnet new list'):\n\n{cliOutput.TrimEnd()}";
                    if (machineReadable)
                    {
                        return ErrorResultFactory.ToJson(
                            ErrorResultFactory.CreateResult(text, error: string.Empty, exitCode: 0, command: $"dotnet new list \"{searchTerm}\" --columns author --columns language --columns type --columns tags"));
                    }

                    return text;
                }
            }

            var matches = templates.Where(t =>
                t.ShortNameList.Any(sn => sn.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                (t.Name?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (t.Description?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false)
            ).ToList();

            if (!matches.Any())
            {
                var message = $"No templates found matching '{searchTerm}'.";
                if (machineReadable)
                {
                    return ErrorResultFactory.ToJson(
                        ErrorResultFactory.CreateResult(output: string.Empty, error: message, exitCode: 1, command: "template-engine"));
                }

                return message;
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

            var output = result.ToString();
            if (machineReadable)
            {
                return ErrorResultFactory.ToJson(
                    ErrorResultFactory.CreateResult(output, error: string.Empty, exitCode: 0, command: "template-engine"));
            }

            return output;
        }
        catch (Exception ex)
        {
            var message = $"Error searching templates: {ex.Message}";
            if (machineReadable)
            {
                return ErrorResultFactory.ToJson(
                    ErrorResultFactory.CreateResult(output: string.Empty, error: message, exitCode: 1, command: "template-engine"));
            }

            return message;
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

            if (!templates.Any())
            {
                // If the Template Engine API can't enumerate templates here, fall back to the CLI.
                // dotnet new list returns exit code 0 when templates match, 103 when no templates match.
                try
                {
                    var cliOutput = await ExecuteDotNetForTemplatesAsync(
                        $"new list \"{templateShortName}\" --columns author --columns language --columns type --columns tags",
                        logger);
                    
                    // If we got here without exception, the command succeeded (exit code 0).
                    // This means templates were found matching the search term.
                    logger?.LogDebug("Template '{TemplateName}' validated via CLI fallback (exit code 0)", templateShortName);
                    return true;
                }
                catch (InvalidOperationException ex)
                {
                    // Command failed with non-zero exit code.
                    // Exit code 103 means "no templates found" which is the expected case for invalid templates.
                    // Any other non-zero exit code also means validation failed.
                    logger?.LogDebug(ex, "Template '{TemplateName}' not found via CLI fallback", templateShortName);
                    return false;
                }
                catch (Win32Exception ex)
                {
                    // Process failed to start - CLI not available
                    logger?.LogDebug(ex, "Template validation failed: process execution error");
                    return false;
                }
                catch (OperationCanceledException ex)
                {
                    // Operation was cancelled
                    logger?.LogDebug(ex, "Template validation cancelled");
                    return false;
                }
            }

            return templates.Any(t => t.ShortNameList.Any(sn => sn.Equals(templateShortName, StringComparison.OrdinalIgnoreCase)));
        }
        catch (InvalidOperationException ex)
        {
            // If template engine fails, do not assume template exists; return false to avoid false positives
            logger?.LogDebug(ex, "Template validation failed: invalid operation");
            return false;
        }
        catch (Win32Exception ex)
        {
            // If CLI process fails to start, return false
            logger?.LogDebug(ex, "Template validation failed: process execution error");
            return false;
        }
        catch (OperationCanceledException ex)
        {
            // If operation is cancelled, return false
            logger?.LogDebug(ex, "Template validation cancelled");
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
