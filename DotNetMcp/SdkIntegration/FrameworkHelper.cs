
namespace DotNetMcp;

/// <summary>
/// Helper class for validating and working with .NET Target Framework Monikers (TFMs).
/// Uses MSBuild utilities for framework parsing and validation.
/// </summary>
public static class FrameworkHelper
{
    /// <summary>
    /// Recorded release type and support dates for a .NET release. LTS/STS classification is independent of support status.
    /// </summary>
    public sealed record FrameworkLifecycle(string ReleaseType, string SupportStatus, DateOnly ReleaseDate, DateOnly EndOfSupport);

    private sealed record Release(string Framework, string ReleaseType, DateOnly ReleaseDate, DateOnly EndOfSupport);

    // Explicit lifecycle dates, not a parity-based estimate of a release's support duration.
    private static readonly Release[] Releases =
    [
        new(DotNetSdkConstants.TargetFrameworks.Net110, "STS", new(2026, 11, 10), new(2028, 11, 14)),
        new(DotNetSdkConstants.TargetFrameworks.Net100, "LTS", new(2025, 11, 11), new(2028, 11, 14)),
        new(DotNetSdkConstants.TargetFrameworks.Net90, "STS", new(2024, 11, 12), new(2026, 11, 10)),
        new(DotNetSdkConstants.TargetFrameworks.Net80, "LTS", new(2023, 11, 14), new(2026, 11, 10)),
        new(DotNetSdkConstants.TargetFrameworks.Net70, "STS", new(2022, 11, 8), new(2024, 5, 14)),
        new(DotNetSdkConstants.TargetFrameworks.Net60, "LTS", new(2021, 11, 8), new(2024, 11, 12)),
        new(DotNetSdkConstants.TargetFrameworks.Net50, "STS", new(2020, 11, 10), new(2022, 5, 10)),
        new(DotNetSdkConstants.TargetFrameworks.NetCoreApp31, "LTS", new(2019, 12, 3), new(2022, 12, 13)),
        new(DotNetSdkConstants.TargetFrameworks.NetCoreApp30, "STS", new(2019, 9, 23), new(2020, 3, 3)),
        new(DotNetSdkConstants.TargetFrameworks.NetCoreApp22, "STS", new(2018, 12, 4), new(2019, 12, 23)),
        new(DotNetSdkConstants.TargetFrameworks.NetCoreApp21, "LTS", new(2018, 5, 30), new(2021, 8, 21)),
        new(DotNetSdkConstants.TargetFrameworks.NetCoreApp20, "STS", new(2017, 8, 14), new(2018, 10, 1))
    ];

    internal static DateOnly EvaluationDate => DateOnly.FromDateTime(DateTime.UtcNow);

    /// <summary>
    /// Get release type and date-aware support status, or null when lifecycle data is unknown or not applicable.
    /// Support ends after the published end-of-support date (inclusive), evaluated in UTC.
    /// </summary>
    public static FrameworkLifecycle? GetFrameworkLifecycle(string framework)
        => GetFrameworkLifecycle(framework, EvaluationDate);

    internal static FrameworkLifecycle? GetFrameworkLifecycle(string framework, DateOnly asOf)
    {
        var release = Releases.FirstOrDefault(release =>
            string.Equals(release.Framework, framework, StringComparison.OrdinalIgnoreCase));
        return release is null ? null : new FrameworkLifecycle(release.ReleaseType,
            asOf < release.ReleaseDate ? "Preview" : asOf > release.EndOfSupport ? "EOL" : "Supported",
            release.ReleaseDate, release.EndOfSupport);
    }

    /// <summary>
    /// Validate if a given framework string is a valid Target Framework Moniker.
    /// </summary>
    public static bool IsValidFramework(string framework)
    {
        if (string.IsNullOrWhiteSpace(framework))
            return false;

        // Check if it matches known patterns
        return framework.StartsWith("net", StringComparison.OrdinalIgnoreCase) ||
               framework.StartsWith("netcoreapp", StringComparison.OrdinalIgnoreCase) ||
               framework.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Get a descriptive name for a framework version.
    /// </summary>
    public static string GetFrameworkDescription(string framework)
        => GetFrameworkDescription(framework, EvaluationDate);

    internal static string GetFrameworkDescription(string framework, DateOnly asOf)
    {
        if (string.IsNullOrWhiteSpace(framework))
            return "Unknown";

        var description = framework.ToLowerInvariant() switch
        {
            "net11.0" => ".NET 11.0",
            "net10.0" => ".NET 10.0",
            "net9.0" => ".NET 9.0",
            "net8.0" => ".NET 8.0",
            "net7.0" => ".NET 7.0",
            "net6.0" => ".NET 6.0",
            "net5.0" => ".NET 5.0",
            "netcoreapp3.1" => ".NET Core 3.1",
            "netcoreapp3.0" => ".NET Core 3.0",
            "netcoreapp2.2" => ".NET Core 2.2",
            "netcoreapp2.1" => ".NET Core 2.1",
            "netcoreapp2.0" => ".NET Core 2.0",
            "netstandard2.1" => ".NET Standard 2.1",
            "netstandard2.0" => ".NET Standard 2.0",
            "net481" => ".NET Framework 4.8.1",
            "net48" => ".NET Framework 4.8",
            "net472" => ".NET Framework 4.7.2",
            "net471" => ".NET Framework 4.7.1",
            "net47" => ".NET Framework 4.7",
            "net462" => ".NET Framework 4.6.2",
            "net461" => ".NET Framework 4.6.1",
            "net46" => ".NET Framework 4.6",
            "net452" => ".NET Framework 4.5.2",
            "net451" => ".NET Framework 4.5.1",
            "net45" => ".NET Framework 4.5",
            "net40" => ".NET Framework 4.0",
            "net35" => ".NET Framework 3.5",
            _ => framework
        };

        var lifecycle = GetFrameworkLifecycle(framework, asOf);
        return lifecycle is null ? description : lifecycle.SupportStatus switch
        {
            "Preview" => $"{description} (Preview)",
            "EOL" => $"{description} ({lifecycle.ReleaseType}, EOL)",
            _ => $"{description} ({lifecycle.ReleaseType})"
        };
    }

    /// <summary>
    /// Check if a framework is a Long-Term Support (LTS) version.
    /// </summary>
    public static bool IsLtsFramework(string framework)
    {
        return GetFrameworkLifecycle(framework)?.ReleaseType == "LTS";
    }

    /// <summary>
    /// Get the latest recorded modern .NET release that is GA and still supported.
    /// </summary>
    public static string GetLatestRecommendedFramework()
        => GetLatestRecommendedFramework(EvaluationDate);

    internal static string GetLatestRecommendedFramework(DateOnly asOf)
        => GetLatestFramework(asOf, ltsOnly: false);

    /// <summary>
    /// Get the latest recorded modern .NET LTS release that is GA and still supported.
    /// </summary>
    public static string GetLatestLtsFramework()
        => GetLatestLtsFramework(EvaluationDate);

    internal static string GetLatestLtsFramework(DateOnly asOf)
        => GetLatestFramework(asOf, ltsOnly: true);

    private static string GetLatestFramework(DateOnly asOf, bool ltsOnly)
        => Releases.FirstOrDefault(release => IsModernNet(release.Framework) &&
            (!ltsOnly || release.ReleaseType == "LTS") &&
            asOf >= release.ReleaseDate && asOf <= release.EndOfSupport)?.Framework
            ?? throw new InvalidOperationException("No supported .NET release is recorded for the evaluation date. Update the framework lifecycle catalog.");

    /// <summary>
    /// Get known modern .NET versions, including EOL releases but excluding previews.
    /// </summary>
    public static IReadOnlyList<string> GetSupportedModernFrameworks()
        => GetModernFrameworks(EvaluationDate);

    internal static IReadOnlyList<string> GetModernFrameworks(DateOnly asOf, bool includePreview = false)
        => Releases.Where(release => IsModernNet(release.Framework) &&
            (includePreview || asOf >= release.ReleaseDate)).Select(release => release.Framework).ToArray();

    /// <summary>
    /// Get known .NET Core framework versions, including EOL releases.
    /// </summary>
    public static IReadOnlyList<string> GetSupportedNetCoreFrameworks()
        => Releases.Where(release => IsNetCore(release.Framework)).Select(release => release.Framework).ToArray();

    /// <summary>
    /// Get all supported .NET Standard versions.
    /// </summary>
    public static IReadOnlyList<string> GetSupportedNetStandardFrameworks()
    {
        return new[]
        {
            DotNetSdkConstants.TargetFrameworks.NetStandard21,
            DotNetSdkConstants.TargetFrameworks.NetStandard20,
            DotNetSdkConstants.TargetFrameworks.NetStandard16,
            DotNetSdkConstants.TargetFrameworks.NetStandard15,
            DotNetSdkConstants.TargetFrameworks.NetStandard14,
            DotNetSdkConstants.TargetFrameworks.NetStandard13,
            DotNetSdkConstants.TargetFrameworks.NetStandard12,
            DotNetSdkConstants.TargetFrameworks.NetStandard11,
            DotNetSdkConstants.TargetFrameworks.NetStandard10
        };
    }

    /// <summary>
    /// Parse framework string to extract version number.
    /// </summary>
    public static string? GetFrameworkVersion(string framework)
    {
        if (string.IsNullOrWhiteSpace(framework))
            return null;

        var normalized = framework.ToLowerInvariant();

        if (normalized.StartsWith("net"))
        {
            // Extract version part (e.g., "net8.0" -> "8.0")
            var versionPart = normalized.Replace("net", "")
                                       .Replace("coreapp", "")
                                       .Replace("standard", "");
            return versionPart;
        }

        return null;
    }

    /// <summary>
    /// Determine if a framework is .NET Framework (not .NET Core or modern .NET).
    /// </summary>
    public static bool IsNetFramework(string framework)
    {
        if (string.IsNullOrWhiteSpace(framework))
            return false;

        var normalized = framework.ToLowerInvariant();

        // .NET Framework uses "net" followed by version without decimal
        // e.g., net481, net48, net472, etc.
        return normalized.StartsWith("net") &&
               !normalized.StartsWith("netcoreapp") &&
               !normalized.StartsWith("netstandard") &&
               !normalized.Contains(".");
    }

    /// <summary>
    /// Determine if a framework is .NET Core.
    /// </summary>
    public static bool IsNetCore(string framework)
    {
        if (string.IsNullOrWhiteSpace(framework))
            return false;

        return framework.StartsWith("netcoreapp", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determine if a framework is modern .NET (5.0+).
    /// </summary>
    public static bool IsModernNet(string framework)
    {
        if (string.IsNullOrWhiteSpace(framework))
            return false;

        var normalized = framework.ToLowerInvariant();

        return normalized.StartsWith("net") &&
               !normalized.StartsWith("netcoreapp") &&
               !normalized.StartsWith("netstandard") &&
               !normalized.StartsWith("netframework") &&
               normalized.Contains(".");
    }

    /// <summary>
    /// Determine if a framework is .NET Standard.
    /// </summary>
    public static bool IsNetStandard(string framework)
    {
        if (string.IsNullOrWhiteSpace(framework))
            return false;

        return framework.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase);
    }
}
