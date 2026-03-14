namespace DotNetMcp;

/// <summary>
/// Provides autocomplete suggestions for prompt argument and resource template parameter completions.
/// Delegates to existing SDK integration helpers (TemplateEngineHelper, FrameworkHelper, DotNetSdkConstants)
/// to return live, filtered suggestions based on the argument name and current input prefix.
/// </summary>
internal static class CompletionProvider
{
    internal const int MaxResults = 20;

    /// <summary>
    /// Returns up to <see cref="MaxResults"/> completion candidates for the given argument name,
    /// filtered to those that start with <paramref name="prefix"/> (case-insensitive).
    /// </summary>
    /// <param name="argumentName">The MCP argument name (e.g., "template", "framework").</param>
    /// <param name="prefix">The current input value used to filter results.</param>
    /// <param name="ct">Cancellation token.</param>
    public static async Task<IEnumerable<string>> GetCompletionsAsync(string argumentName, string prefix, CancellationToken ct)
    {
        IEnumerable<string> candidates = argumentName switch
        {
            "template" => await GetTemplateCompletionsAsync(ct),
            "framework" => FrameworkHelper.GetSupportedModernFrameworks(),
            "configuration" => [DotNetSdkConstants.Configurations.Debug, DotNetSdkConstants.Configurations.Release],
            "runtime" => GetRuntimeCompletions(),
            _ => []
        };

        return candidates
            .Where(c => c.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Take(MaxResults);
    }

    private static async Task<IEnumerable<string>> GetTemplateCompletionsAsync(CancellationToken ct)
    {
        try
        {
            var templates = await TemplateEngineHelper.GetTemplatesCachedInternalAsync();
            return templates
                .SelectMany(t => t.ShortNameList)
                .Where(sn => !string.IsNullOrEmpty(sn))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(sn => sn, StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return GetFallbackTemplateCompletions();
        }
    }

    /// <summary>
    /// Returns a curated list of common template short names used when the template engine is unavailable.
    /// Internal for unit testing.
    /// </summary>
    internal static IEnumerable<string> GetFallbackTemplateCompletions() =>
    [
        DotNetSdkConstants.Templates.Blazor,
        DotNetSdkConstants.Templates.BlazorWasm,
        DotNetSdkConstants.Templates.ClassLib,
        DotNetSdkConstants.Templates.Console,
        DotNetSdkConstants.Templates.Grpc,
        DotNetSdkConstants.Templates.MsTest,
        DotNetSdkConstants.Templates.Mvc,
        DotNetSdkConstants.Templates.NUnit,
        DotNetSdkConstants.Templates.Sln,
        DotNetSdkConstants.Templates.Web,
        DotNetSdkConstants.Templates.WebApi,
        DotNetSdkConstants.Templates.Webapp,
        DotNetSdkConstants.Templates.Worker,
        DotNetSdkConstants.Templates.XUnit
    ];

    /// <summary>
    /// Returns the standard set of runtime identifiers for cross-platform publishing.
    /// Internal for unit testing.
    /// </summary>
    internal static IEnumerable<string> GetRuntimeCompletions() =>
    [
        DotNetSdkConstants.RuntimeIdentifiers.LinuxArm,
        DotNetSdkConstants.RuntimeIdentifiers.LinuxArm64,
        DotNetSdkConstants.RuntimeIdentifiers.LinuxMuslArm64,
        DotNetSdkConstants.RuntimeIdentifiers.LinuxMuslX64,
        DotNetSdkConstants.RuntimeIdentifiers.LinuxX64,
        DotNetSdkConstants.RuntimeIdentifiers.OsxArm64,
        DotNetSdkConstants.RuntimeIdentifiers.OsxX64,
        DotNetSdkConstants.RuntimeIdentifiers.WinArm64,
        DotNetSdkConstants.RuntimeIdentifiers.WinX64,
        DotNetSdkConstants.RuntimeIdentifiers.WinX86
    ];
}
