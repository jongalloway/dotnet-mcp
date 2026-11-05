using System.Text.Json.Serialization;

namespace DotNetMcp;

internal sealed record ResourceError([property: JsonPropertyName("error")] string Error);
internal sealed record SdkResourceData(
    [property: JsonPropertyName("sdks")] List<SdkInfo> Sdks,
    [property: JsonPropertyName("latestSdk")] string? LatestSdk);
internal sealed record RuntimeResourceData(
    [property: JsonPropertyName("runtimes")] List<RuntimeInfo> Runtimes);
internal sealed record CachedResourceResponse(
    [property: JsonPropertyName("data")] object Data,
    [property: JsonPropertyName("cache")] ResourceCacheMetadata Cache);
internal sealed record ResourceCacheMetadata(
    [property: JsonPropertyName("timestamp")] string Timestamp,
    [property: JsonPropertyName("cacheAgeSeconds")] double CacheAgeSeconds,
    [property: JsonPropertyName("cacheDurationSeconds")] int CacheDurationSeconds,
    [property: JsonPropertyName("metrics")] CacheSnapshot Metrics);
internal sealed record TemplateResourceData(
    [property: JsonPropertyName("templates")] TemplateResourceMetadata[] Templates);
internal sealed record FrameworkResourceData(
    [property: JsonPropertyName("modernFrameworks")] ResourceFrameworkInfo[] ModernFrameworks,
    [property: JsonPropertyName("netCoreFrameworks")] ResourceFrameworkInfo[] NetCoreFrameworks,
    [property: JsonPropertyName("netStandardFrameworks")] ResourceFrameworkInfo[] NetStandardFrameworks,
    [property: JsonPropertyName("latestRecommended")] string LatestRecommended,
    [property: JsonPropertyName("latestLts")] string LatestLts);
internal sealed record ResourceFrameworkInfo(
    [property: JsonPropertyName("tfm")] string Tfm,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("version")] string? Version,
    [property: JsonPropertyName("isLts"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? IsLts = null,
    [property: JsonPropertyName("isModernNet"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? IsModernNet = null,
    [property: JsonPropertyName("isNetCore"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? IsNetCore = null,
    [property: JsonPropertyName("isNetStandard"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? IsNetStandard = null);
internal sealed record WorkspaceResourceData(
    [property: JsonPropertyName("solution")] string? Solution,
    [property: JsonPropertyName("projects")] WorkspaceProjectResource[] Projects,
    [property: JsonPropertyName("generatedAt")] string GeneratedAt);
internal sealed record WorkspaceProjectResource(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("path")] string Path,
    [property: JsonPropertyName("targetFrameworks")] IReadOnlyList<string> TargetFrameworks,
    [property: JsonPropertyName("packageCount")] int PackageCount,
    [property: JsonPropertyName("isTestProject")] bool IsTestProject);
