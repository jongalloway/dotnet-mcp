using System.Text.Json.Serialization;
using Microsoft.TemplateEngine.Abstractions;

namespace DotNetMcp;

internal sealed record TemplateResourceMetadata(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("shortNames")] string[] ShortNames,
    [property: JsonPropertyName("author")] string? Author,
    [property: JsonPropertyName("language")] string? Language,
    [property: JsonPropertyName("type")] string? Type,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("parameters")] TemplateParameterDisplayInfo[] Parameters)
{
    internal static TemplateResourceMetadata FromTemplateInfo(ITemplateInfo template) => new(
        template.Name, template.ShortNameList.ToArray(), template.Author,
        template.GetLanguage(), template.GetTemplateType(), template.Description,
        template.ParameterDefinitions.Select(parameter => new TemplateParameterDisplayInfo(
            parameter.Name, parameter.Description, parameter.DataType, parameter.DefaultValue)).ToArray());

    internal TemplateDisplayInfo ToDisplayInfo() => new(
        ShortNames.FirstOrDefault() ?? "N/A", ShortNames, Language ?? "Multiple", Type ?? "Unknown",
        Description, Name, Author, Parameters.OrderBy(parameter => parameter.Name).ToArray());
}
