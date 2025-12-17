
namespace DotNetMcp;

/// <summary>
/// Helper class for validating and working with .NET Target Framework Monikers (TFMs).
/// Uses MSBuild utilities for framework parsing and validation.
/// </summary>
public static class FrameworkHelper
{
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
    {
        if (string.IsNullOrWhiteSpace(framework))
            return "Unknown";

        return framework.ToLowerInvariant() switch
        {
            "net11.0" => ".NET 11.0 (Preview)",
            "net10.0" => ".NET 10.0 (LTS)",
            "net9.0" => ".NET 9.0",
            "net8.0" => ".NET 8.0 (LTS)",
            "net7.0" => ".NET 7.0",
            "net6.0" => ".NET 6.0 (LTS)",
            "net5.0" => ".NET 5.0",
            "netcoreapp3.1" => ".NET Core 3.1 (LTS)",
            "netcoreapp3.0" => ".NET Core 3.0",
            "netcoreapp2.2" => ".NET Core 2.2",
            "netcoreapp2.1" => ".NET Core 2.1 (LTS)",
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
    }

    /// <summary>
    /// Check if a framework is a Long-Term Support (LTS) version.
    /// </summary>
    public static bool IsLtsFramework(string framework)
    {
        if (string.IsNullOrWhiteSpace(framework))
            return false;

        return framework.ToLowerInvariant() switch
        {
            "net10.0" => true,
            "net8.0" => true,
            "net6.0" => true,
            "netcoreapp3.1" => true,
            "netcoreapp2.1" => true,
            _ => false
        };
    }

    /// <summary>
    /// Get the latest recommended framework version.
    /// </summary>
    public static string GetLatestRecommendedFramework()
    {
        return DotNetSdkConstants.TargetFrameworks.Net100;
    }

    /// <summary>
    /// Get the latest LTS framework version.
    /// </summary>
    public static string GetLatestLtsFramework()
    {
        return DotNetSdkConstants.TargetFrameworks.Net100;
    }

    /// <summary>
    /// Get all supported .NET (modern) framework versions.
    /// </summary>
    public static IReadOnlyList<string> GetSupportedModernFrameworks()
    {
        return new[]
        {
            DotNetSdkConstants.TargetFrameworks.Net100,
            DotNetSdkConstants.TargetFrameworks.Net90,
            DotNetSdkConstants.TargetFrameworks.Net80,
            DotNetSdkConstants.TargetFrameworks.Net70,
            DotNetSdkConstants.TargetFrameworks.Net60,
            DotNetSdkConstants.TargetFrameworks.Net50
        };
    }

    /// <summary>
    /// Get all supported .NET Core framework versions.
    /// </summary>
    public static IReadOnlyList<string> GetSupportedNetCoreFrameworks()
    {
        return new[]
        {
            DotNetSdkConstants.TargetFrameworks.NetCoreApp31,
            DotNetSdkConstants.TargetFrameworks.NetCoreApp30,
            DotNetSdkConstants.TargetFrameworks.NetCoreApp22,
            DotNetSdkConstants.TargetFrameworks.NetCoreApp21,
            DotNetSdkConstants.TargetFrameworks.NetCoreApp20
        };
    }

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
