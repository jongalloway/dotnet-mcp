using Microsoft.TemplateEngine.Abstractions;

namespace DotNetMcp;

/// <summary>Extension methods for template-related operations.</summary>
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
