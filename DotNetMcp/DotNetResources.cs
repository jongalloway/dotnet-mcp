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
                var result = await DotNetCommandExecutor.ExecuteCommandForResourceAsync("--list-sdks", _logger);

                // Parse the SDK list output
                var sdks = new List<SdkInfo>();
                var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in lines)
                {
                    // Format: "9.0.100 [C:\Program Files\dotnet\sdk]"
                    var parts = line.Split('[', 2);
                    if (parts.Length == 2)
                    {
                        var version = parts[0].Trim();
                        var path = parts[1].TrimEnd(']').Trim();
                        sdks.Add(new SdkInfo(version, Path.Combine(path, version)));
                    }
                }

                // Sort SDKs by version to ensure we always return the latest, regardless of CLI output order
                return sdks
                    .OrderBy(sdk => Version.TryParse(sdk.Version, out var v) ? v : new Version(0, 0))
                    .ToList();
            });

            var responseData = new
            {
                sdks = entry.Data,
                latestSdk = entry.Data.LastOrDefault()?.Version
            };

            return _sdkCacheManager.GetJsonResponse(entry, responseData);
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

                // Parse the runtime list output
                var runtimes = new List<RuntimeInfo>();
                var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in lines)
                {
                    // Format: "Microsoft.NETCore.App 9.0.0 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]"
                    var parts = line.Split('[', 2);
                    if (parts.Length == 2)
                    {
                        var nameAndVersion = parts[0].Trim().Split(' ', 2);
                        if (nameAndVersion.Length == 2)
                        {
                            var name = nameAndVersion[0];
                            var version = nameAndVersion[1];
                            var path = parts[1].TrimEnd(']').Trim();
                            runtimes.Add(new RuntimeInfo(name, version, Path.Combine(path, version)));
                        }
                    }
                }

                return runtimes;
            });

            var responseData = new { runtimes = entry.Data };
            return _runtimeCacheManager.GetJsonResponse(entry, responseData);
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
    public Task<string> GetFrameworks()
    {
        _logger.LogDebug("Reading framework information");
        try
        {
            var modernFrameworks = FrameworkHelper.GetSupportedModernFrameworks()
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

            return Task.FromResult(JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting framework information");
            return Task.FromResult(JsonSerializer.Serialize(new { error = ex.Message }));
        }
    }
}
