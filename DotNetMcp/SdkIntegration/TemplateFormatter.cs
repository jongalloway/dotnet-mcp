using System.Text;
using Microsoft.TemplateEngine.Abstractions;

namespace DotNetMcp;

/// <summary>
/// A pure-data representation of a template used for formatting output.
/// Decoupled from <see cref="ITemplateInfo"/> to enable deterministic tests without Template Engine state.
/// </summary>
internal sealed record TemplateDisplayInfo(
    string ShortName,
    IReadOnlyList<string> ShortNames,
    string Language,
    string Type,
    string? Description,
    string? Name,
    string? Author,
    IReadOnlyList<TemplateParameterDisplayInfo> Parameters)
{
    /// <summary>Creates a <see cref="TemplateDisplayInfo"/> from an <see cref="ITemplateInfo"/> instance.</summary>
    internal static TemplateDisplayInfo FromTemplateInfo(ITemplateInfo template) =>
        new(
            ShortName: template.ShortNameList.FirstOrDefault() ?? "N/A",
            ShortNames: template.ShortNameList,
            Language: template.GetLanguage() ?? "Multiple",
            Type: template.GetTemplateType() ?? "Unknown",
            Description: template.Description,
            Name: template.Name,
            Author: template.Author,
            Parameters: template.ParameterDefinitions
                .OrderBy(p => p.Name)
                .Select(p => new TemplateParameterDisplayInfo(p.Name, p.Description, p.DataType, p.DefaultValue))
                .ToList()
                .AsReadOnly()
        );
}

/// <summary>
/// A pure-data representation of a template parameter used for formatting output.
/// </summary>
internal sealed record TemplateParameterDisplayInfo(
    string Name,
    string? Description,
    string DataType,
    string? DefaultValue);

/// <summary>
/// Pure formatting functions for template metadata.
/// No I/O, no async, no global state — accepts plain data and returns formatted strings.
/// </summary>
internal static class TemplateFormatter
{
    /// <summary>Formats a list of installed templates as a human-readable table.</summary>
    internal static string FormatInstalledTemplates(IEnumerable<TemplateDisplayInfo> templates)
    {
        var templateList = templates.OrderBy(t => t.ShortName).ToList();

        var result = new StringBuilder();
        result.AppendLine("Installed .NET Templates:");
        result.AppendLine();
        result.AppendLine($"{"Short Name",-25} {"Language",-10} {"Type",-15} {"Description"}");
        result.AppendLine(new string('-', 100));

        foreach (var template in templateList)
        {
            var description = template.Description ?? "";

            // Truncate long descriptions
            if (description.Length > 40)
                description = description.Substring(0, 37) + "...";

            result.AppendLine($"{template.ShortName,-25} {template.Language,-10} {template.Type,-15} {description}");
        }

        result.AppendLine();
        result.AppendLine($"Total templates: {templateList.Count}");

        return result.ToString();
    }

    /// <summary>Formats detailed information about a single template.</summary>
    internal static string FormatTemplateDetails(TemplateDisplayInfo template)
    {
        var result = new StringBuilder();
        result.AppendLine($"Template: {template.Name ?? "N/A"}");
        result.AppendLine($"Short Name(s): {string.Join(", ", template.ShortNames)}");
        result.AppendLine($"Author: {template.Author ?? "N/A"}");
        result.AppendLine($"Language: {template.Language}");
        result.AppendLine($"Type: {template.Type}");
        result.AppendLine($"Description: {template.Description ?? "N/A"}");
        result.AppendLine();

        if (template.Parameters.Count > 0)
        {
            result.AppendLine("Parameters:");
            foreach (var param in template.Parameters.OrderBy(p => p.Name))
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

    /// <summary>Formats a list of template search results as a human-readable table.</summary>
    internal static string FormatSearchResults(string searchTerm, IEnumerable<TemplateDisplayInfo> matches)
    {
        var matchList = matches.OrderBy(t => t.ShortName).ToList();

        var result = new StringBuilder();
        result.AppendLine($"Templates matching '{searchTerm}':");
        result.AppendLine();
        result.AppendLine($"{"Short Name",-25} {"Language",-10} {"Description"}");
        result.AppendLine(new string('-', 80));

        foreach (var template in matchList)
        {
            var description = template.Description ?? "";

            if (description.Length > 35)
                description = description.Substring(0, 32) + "...";

            result.AppendLine($"{template.ShortName,-25} {template.Language,-10} {description}");
        }

        result.AppendLine();
        result.AppendLine($"Found {matchList.Count} matching template(s).");

        return result.ToString();
    }
}
