using System.Text.Json;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace DotNetMcp;

/// <summary>
/// Represents SDK information.
/// </summary>
internal record SdkInfo(string Version, string Path);

/// <summary>
/// Represents runtime information.
/// </summary>
internal record RuntimeInfo(string Name, string Version, string Path);

/// <summary>
/// MCP Resources for .NET environment information.
/// Provides read-only access to .NET SDK, runtime, template, and framework metadata.
/// Implements caching with configurable TTL and metrics for performance.
/// </summary>
[McpServerResourceType]
public sealed class DotNetResources
{
    private readonly ILogger<DotNetResources> _logger;

    // Static cache managers for SDK and Runtime info (300 second TTL by default)
    private static readonly CachedResourceManager<List<SdkInfo>> _sdkCacheManager =
        new("SDK", defaultTtlSeconds: 300);
    private static readonly CachedResourceManager<List<RuntimeInfo>> _runtimeCacheManager =
        new("Runtime", defaultTtlSeconds: 300);

    internal static List<SdkInfo> ParseSdkListOutput(string sdkListOutput)
    {
        if (string.IsNullOrWhiteSpace(sdkListOutput))
            return [];

        // Parse the SDK list output and sort by version
        var lines = sdkListOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        static Version ParseForSorting(string sdkVersion)
        {
            if (string.IsNullOrWhiteSpace(sdkVersion))
                return new Version(0, 0);

            // Examples:
            // - 10.0.101
            // - 11.0.100-preview.1
            // - 11.0.100-alpha.1+abcdef
            var baseVersion = sdkVersion.Split('-', '+')[0];
            return Version.TryParse(baseVersion, out var v) ? v : new Version(0, 0);
        }

        static bool IsPrerelease(string sdkVersion) => sdkVersion.Contains('-', StringComparison.Ordinal);

        return lines
            .Select(line => line.Split('[', 2))
            .Where(parts => parts.Length == 2)
            .Select(parts =>
            {
                var version = parts[0].Trim();
                var path = parts[1].TrimEnd(']').Trim();
                return new SdkInfo(version, Path.Combine(path, version));
            })
            .OrderBy(sdk => ParseForSorting(sdk.Version))
            // If the numeric version is the same, put prereleases before stable so the stable appears as the latest.
            .ThenBy(sdk => IsPrerelease(sdk.Version) ? 0 : 1)
            .ToList();
    }

    internal static List<RuntimeInfo> ParseRuntimeListOutput(string runtimeListOutput)
    {
        if (string.IsNullOrWhiteSpace(runtimeListOutput))
            return [];

        // Parse the runtime list output
        var lines = runtimeListOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        return lines
            .Select(line => line.Split('[', 2))
            .Where(parts => parts.Length == 2)
            .Select(parts =>
            {
                var nameAndVersion = parts[0].Trim().Split(' ', 2);
                if (nameAndVersion.Length == 2)
                {
                    var name = nameAndVersion[0];
                    var version = nameAndVersion[1];
                    var path = parts[1].TrimEnd(']').Trim();
                    return new RuntimeInfo(name, version, Path.Combine(path, version));
                }

                return null;
            })
            .OfType<RuntimeInfo>()
            .ToList();
    }

    private static async Task<List<SdkInfo>> LoadSdksAsync(ILogger logger)
    {
        var result = await DotNetCommandExecutor.ExecuteCommandForResourceAsync("--list-sdks", logger);

        return ParseSdkListOutput(result);
    }

    private static bool TryGetSdkMajorVersion(string sdkVersion, out int major)
    {
        major = 0;
        if (string.IsNullOrWhiteSpace(sdkVersion))
            return false;

        var firstSegment = sdkVersion.Split('.', 2, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return int.TryParse(firstSegment, out major);
    }

    internal static List<string> GetSupportedModernFrameworksForResources(IEnumerable<SdkInfo> installedSdks)
    {
        var supportedModernFrameworks = FrameworkHelper.GetSupportedModernFrameworks().ToList();

        // Only surface preview TFMs when the corresponding SDK major version is installed.
        // This avoids suggesting frameworks users can't actually target.
        if (installedSdks.Any(sdk => TryGetSdkMajorVersion(sdk.Version, out var major) && major == 11))
        {
            supportedModernFrameworks.Insert(0, DotNetSdkConstants.TargetFrameworks.Net110);
        }

        return supportedModernFrameworks;
    }

    public DotNetResources(ILogger<DotNetResources> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Clears all caches (SDK, Runtime, Templates) and resets metrics.
    /// </summary>
    public static async Task ClearAllCachesAsync()
    {
        await _sdkCacheManager.ClearAsync();
        await _runtimeCacheManager.ClearAsync();
        await TemplateEngineHelper.ClearCacheAsync();

        _sdkCacheManager.ResetMetrics();
        _runtimeCacheManager.ResetMetrics();
    }

    /// <summary>
    /// Gets SDK cache metrics.
    /// </summary>
    public static CacheMetrics GetSdkMetrics() => _sdkCacheManager.Metrics;

    /// <summary>
    /// Gets Runtime cache metrics.
    /// </summary>
    public static CacheMetrics GetRuntimeMetrics() => _runtimeCacheManager.Metrics;

    [McpServerResource(
        UriTemplate = "dotnet://sdk-info",
        Name = ".NET SDK Information",
        Title = "Information about installed .NET SDKs including versions and paths",
        MimeType = "application/json")]
    [McpMeta("category", "sdk")]
    [McpMeta("dataFormat", "json")]
    [McpMeta("refreshable", true)]
    [McpMeta("cached", true)]
    public async Task<string> GetSdkInfo()
    {
        _logger.LogDebug("Reading SDK information");
        try
        {
            var entry = await _sdkCacheManager.GetOrLoadAsync(async () =>
            {
                return await LoadSdksAsync(_logger);
            });

            var responseData = new
            {
                sdks = entry.Data,
                latestSdk = entry.Data.LastOrDefault()?.Version
            };

            return _sdkCacheManager.GetJsonResponse(entry, responseData, DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting SDK information");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerResource(
        UriTemplate = "dotnet://runtime-info",
        Name = ".NET Runtime Information",
        Title = "Information about installed .NET runtimes including versions and types",
        MimeType = "application/json")]
    [McpMeta("category", "runtime")]
    [McpMeta("dataFormat", "json")]
    [McpMeta("refreshable", true)]
    [McpMeta("cached", true)]
    public async Task<string> GetRuntimeInfo()
    {
        _logger.LogDebug("Reading runtime information");
        try
        {
            var entry = await _runtimeCacheManager.GetOrLoadAsync(async () =>
            {
                var result = await DotNetCommandExecutor.ExecuteCommandForResourceAsync("--list-runtimes", _logger);
                return ParseRuntimeListOutput(result);
            });

            var responseData = new { runtimes = entry.Data };
            return _runtimeCacheManager.GetJsonResponse(entry, responseData, DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting runtime information");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerResource(
        UriTemplate = "dotnet://templates",
        Name = "Template Catalog",
        Title = "Complete catalog of installed .NET templates with metadata",
        MimeType = "application/json")]
    [McpMeta("category", "template")]
    [McpMeta("dataFormat", "json")]
    [McpMeta("cached", true)]
    [McpMeta("usesTemplateEngine", true)]
    public async Task<string> GetTemplates()
    {
        _logger.LogDebug("Reading template catalog");
        try
        {
            var templates = await TemplateEngineHelper.GetTemplatesCachedInternalAsync(forceReload: false, logger: _logger);

            var templateList = templates.Select(t => new
            {
                name = t.Name,
                shortNames = t.ShortNameList.ToArray(),
                author = t.Author,
                language = t.GetLanguage(),
                type = t.GetTemplateType(),
                description = t.Description,
                parameters = t.ParameterDefinitions.Select(p => new
                {
                    name = p.Name,
                    description = p.Description,
                    dataType = p.DataType,
                    defaultValue = p.DefaultValue
                }).ToArray()
            }).ToArray();

            return JsonSerializer.Serialize(new { templates = templateList }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting template catalog");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerResource(
        UriTemplate = "dotnet://frameworks",
        Name = "Framework Information",
        Title = "Information about supported .NET frameworks (TFMs) including LTS status",
        MimeType = "application/json")]
    [McpMeta("category", "framework")]
    [McpMeta("dataFormat", "json")]
    [McpMeta("usesFrameworkHelper", true)]
    public async Task<string> GetFrameworks()
    {
        _logger.LogDebug("Reading framework information");
        try
        {
            var sdkEntry = await _sdkCacheManager.GetOrLoadAsync(() => LoadSdksAsync(_logger));
            var supportedModernFrameworks = GetSupportedModernFrameworksForResources(sdkEntry.Data);

            var modernFrameworks = supportedModernFrameworks
                .Select(fw => new
                {
                    tfm = fw,
                    description = FrameworkHelper.GetFrameworkDescription(fw),
                    isLts = FrameworkHelper.IsLtsFramework(fw),
                    isModernNet = true,
                    version = FrameworkHelper.GetFrameworkVersion(fw)
                }).ToArray();

            var netCoreFrameworks = FrameworkHelper.GetSupportedNetCoreFrameworks()
                .Select(fw => new
                {
                    tfm = fw,
                    description = FrameworkHelper.GetFrameworkDescription(fw),
                    isLts = FrameworkHelper.IsLtsFramework(fw),
                    isNetCore = true,
                    version = FrameworkHelper.GetFrameworkVersion(fw)
                }).ToArray();

            var netStandardFrameworks = FrameworkHelper.GetSupportedNetStandardFrameworks()
                .Select(fw => new
                {
                    tfm = fw,
                    description = FrameworkHelper.GetFrameworkDescription(fw),
                    isNetStandard = true,
                    version = FrameworkHelper.GetFrameworkVersion(fw)
                }).ToArray();

            var response = new
            {
                modernFrameworks,
                netCoreFrameworks,
                netStandardFrameworks,
                latestRecommended = FrameworkHelper.GetLatestRecommendedFramework(),
                latestLts = FrameworkHelper.GetLatestLtsFramework()
            };

            return JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting framework information");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Example resource demonstrating CAPABILITY_NOT_AVAILABLE usage when a resource is conditionally disabled.
    /// This shows how to handle feature flags or environment-specific limitations.
    /// </summary>
    /// <returns>JSON error response indicating telemetry collection is not yet implemented, with suggested alternatives</returns>
    [McpServerResource(
        UriTemplate = "dotnet://telemetry-data",
        Name = "Telemetry Data",
        Title = "Server telemetry and usage analytics (not yet available)",
        MimeType = "application/json")]
    [McpMeta("category", "telemetry")]
    [McpMeta("planned", true)]
    public Task<string> GetTelemetryData()
    {
        _logger.LogDebug("Telemetry data resource requested");
        
        // This resource is not yet implemented
        var alternatives = new List<string>
        {
            "Use dotnet://sdk-info to get SDK version information",
            "Use dotnet://runtime-info for runtime details",
            "Check server logs for basic usage patterns"
        };

        var error = ErrorResultFactory.ReturnCapabilityNotAvailable(
            "telemetry data resource",
            "Telemetry collection not yet implemented",
            alternatives);

        return Task.FromResult(ErrorResultFactory.ToJson(error));
    }
}
