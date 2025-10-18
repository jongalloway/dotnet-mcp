using System.Text;
using System.Text.Json;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Edge;
using Microsoft.TemplateEngine.Edge.Settings;

namespace DotNetMcp;

/// <summary>
/// Helper class for interacting with the .NET Template Engine.
/// Provides programmatic access to installed templates and their metadata.
/// </summary>
public class TemplateEngineHelper
{
    /// <summary>
    /// Get a list of all installed templates with their metadata.
    /// </summary>
    public static async Task<string> GetInstalledTemplatesAsync()
    {
        try
        {
            var engineEnvironmentSettings = new EngineEnvironmentSettings(
                new DefaultTemplateEngineHost("dotnet-mcp", "1.0.0"),
                virtualizeSettings: true);

            var templatePackageManager = new TemplatePackageManager(engineEnvironmentSettings);
            
            // Get all installed templates
            var templates = await templatePackageManager.GetTemplatesAsync(default);
            
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
    public static async Task<string> GetTemplateDetailsAsync(string templateShortName)
    {
        try
        {
            var engineEnvironmentSettings = new EngineEnvironmentSettings(
                new DefaultTemplateEngineHost("dotnet-mcp", "1.0.0"),
                virtualizeSettings: true);

            var templatePackageManager = new TemplatePackageManager(engineEnvironmentSettings);
            
            var templates = await templatePackageManager.GetTemplatesAsync(default);
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
    public static async Task<string> SearchTemplatesAsync(string searchTerm)
    {
        try
        {
            var engineEnvironmentSettings = new EngineEnvironmentSettings(
                new DefaultTemplateEngineHost("dotnet-mcp", "1.0.0"),
                virtualizeSettings: true);

            var templatePackageManager = new TemplatePackageManager(engineEnvironmentSettings);
            
            var templates = await templatePackageManager.GetTemplatesAsync(default);
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
    public static async Task<bool> ValidateTemplateExistsAsync(string templateShortName)
    {
        try
        {
            var engineEnvironmentSettings = new EngineEnvironmentSettings(
                new DefaultTemplateEngineHost("dotnet-mcp", "1.0.0"),
                virtualizeSettings: true);

            var templatePackageManager = new TemplatePackageManager(engineEnvironmentSettings);
            
            var templates = await templatePackageManager.GetTemplatesAsync(default);
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
