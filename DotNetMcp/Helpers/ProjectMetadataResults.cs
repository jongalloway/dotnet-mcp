using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotNetMcp;

// These models mirror the existing ProjectAnalysisHelper JSON contracts.
internal sealed record ProjectPackageMetadata(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("privateAssets")] string? PrivateAssets,
    [property: JsonPropertyName("includeAssets")] string? IncludeAssets);
internal sealed record ProjectReferenceMetadata(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("path")] string Path);
internal sealed record AnalyzerMetadata([property: JsonPropertyName("path")] string Path);
internal sealed record ProjectAnalysisResult(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("projectPath")] string ProjectPath,
    [property: JsonPropertyName("projectName")] string ProjectName,
    [property: JsonPropertyName("sdk")] string Sdk,
    [property: JsonPropertyName("targetFrameworks")] string[] TargetFrameworks,
    [property: JsonPropertyName("outputType")] string OutputType,
    [property: JsonPropertyName("packageReferences")] ProjectPackageMetadata[] PackageReferences,
    [property: JsonPropertyName("projectReferences")] ProjectReferenceMetadata[] ProjectReferences,
    [property: JsonPropertyName("buildProperties")] Dictionary<string, string> BuildProperties,
    [property: JsonPropertyName("analyzers")] AnalyzerMetadata[] Analyzers,
    [property: JsonPropertyName("implicitUsings")] string ImplicitUsings,
    [property: JsonPropertyName("nullable")] string Nullable,
    [property: JsonPropertyName("langVersion")] string LangVersion);
internal sealed record ProjectDependenciesResult(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("projectPath")] string ProjectPath,
    [property: JsonPropertyName("targetFrameworks")] string[] TargetFrameworks,
    [property: JsonPropertyName("directPackageDependencies")] ProjectPackageMetadata[] DirectPackageDependencies,
    [property: JsonPropertyName("directProjectDependencies")] ProjectReferenceMetadata[] DirectProjectDependencies,
    [property: JsonPropertyName("totalDirectDependencies")] int TotalDirectDependencies,
    [property: JsonPropertyName("notes")] string[] Notes);
internal sealed record ProjectValidationResult(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("projectPath")] string ProjectPath,
    [property: JsonPropertyName("isValid")] bool IsValid,
    [property: JsonPropertyName("errors")] string[] Errors,
    [property: JsonPropertyName("warnings")] string[] Warnings,
    [property: JsonPropertyName("recommendations")] string[] Recommendations);
internal sealed record ProjectPropertyResult(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("projectPath")] string ProjectPath,
    [property: JsonPropertyName("propertyName")] string PropertyName,
    [property: JsonPropertyName("propertyValue")] string? PropertyValue,
    [property: JsonPropertyName("isSet")] bool? IsSet,
    [property: JsonPropertyName("removed")] bool? Removed);
internal sealed record ProjectItemResult(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("projectPath")] string ProjectPath,
    [property: JsonPropertyName("itemType")] string ItemType,
    [property: JsonPropertyName("include")] string Include,
    [property: JsonPropertyName("metadata")] Dictionary<string, string>? Metadata,
    [property: JsonPropertyName("removed")] bool? Removed);
internal sealed record ProjectItemMetadata(
    [property: JsonPropertyName("itemType")] string ItemType,
    [property: JsonPropertyName("include")] string Include,
    [property: JsonPropertyName("metadata")] Dictionary<string, string> Metadata);
internal sealed record ProjectItemsResult(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("projectPath")] string ProjectPath,
    [property: JsonPropertyName("itemType")] string ItemType,
    [property: JsonPropertyName("count")] int Count,
    [property: JsonPropertyName("items")] ProjectItemMetadata[] Items);

internal static class ProjectMetadataResults
{
    internal static object? FromText(DotnetProjectAction action, string text) => action switch
    {
        DotnetProjectAction.Analyze => JsonSerializer.Deserialize(text, ServerJsonContext.Default.ProjectAnalysisResult),
        DotnetProjectAction.Dependencies => JsonSerializer.Deserialize(text, ServerJsonContext.Default.ProjectDependenciesResult),
        DotnetProjectAction.Validate => JsonSerializer.Deserialize(text, ServerJsonContext.Default.ProjectValidationResult),
        DotnetProjectAction.SetProperty or DotnetProjectAction.GetProperty or DotnetProjectAction.RemoveProperty =>
            JsonSerializer.Deserialize(text, ServerJsonContext.Default.ProjectPropertyResult),
        DotnetProjectAction.AddItem or DotnetProjectAction.RemoveItem => JsonSerializer.Deserialize(text, ServerJsonContext.Default.ProjectItemResult),
        DotnetProjectAction.ListItems => JsonSerializer.Deserialize(text, ServerJsonContext.Default.ProjectItemsResult),
        _ => null
    };
}
