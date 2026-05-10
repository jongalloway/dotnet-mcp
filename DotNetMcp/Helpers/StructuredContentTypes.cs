using System.Text.Json.Serialization;

namespace DotNetMcp;

/// <summary>
/// Represents a single installed .NET SDK entry (version + installation path).
/// </summary>
public sealed class InstalledSdkInfo
{
    /// <summary>SDK version string (e.g. "10.0.100").</summary>
    [JsonPropertyName("version")]
    public string Version { get; init; } = string.Empty;

    /// <summary>Installation path of the SDK.</summary>
    [JsonPropertyName("path")]
    public string? Path { get; init; }
}

/// <summary>
/// Represents a single installed .NET runtime entry.
/// </summary>
public sealed class InstalledRuntimeInfo
{
    /// <summary>Runtime display name (e.g. "Microsoft.NETCore.App").</summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>Runtime version string (e.g. "10.0.0").</summary>
    [JsonPropertyName("version")]
    public string Version { get; init; } = string.Empty;

    /// <summary>Installation path of the runtime.</summary>
    [JsonPropertyName("path")]
    public string? Path { get; init; }
}

/// <summary>
/// Structured result for the dotnet_sdk ListSdks action.
/// The <c>runtimes</c> array is also included when available so SDK dashboard clients
/// can render both from a single tool-result notification.
/// </summary>
public sealed class SdkListResult
{
    /// <summary>All installed .NET SDKs.</summary>
    [JsonPropertyName("sdks")]
    public InstalledSdkInfo[] Sdks { get; init; } = [];

    /// <summary>
    /// All installed .NET runtimes, included alongside SDKs for dashboard rendering.
    /// <c>null</c> when the runtime fetch fails.
    /// </summary>
    [JsonPropertyName("runtimes")]
    public InstalledRuntimeInfo[]? Runtimes { get; init; }
}

/// <summary>
/// Structured result for the dotnet_sdk ListRuntimes action.
/// </summary>
public sealed class RuntimeListResult
{
    /// <summary>All installed .NET runtimes.</summary>
    [JsonPropertyName("runtimes")]
    public InstalledRuntimeInfo[] Runtimes { get; init; } = [];
}

/// <summary>
/// Union output schema for the dotnet_sdk tool.
/// Each field corresponds to the structured content returned by a specific action:
/// <list type="bullet">
///   <item><term>version</term><description>Returned by the Version action.</description></item>
///   <item><term>sdks</term><description>Returned by the ListSdks action.</description></item>
///   <item><term>runtimes</term><description>Returned by both ListSdks and ListRuntimes actions.</description></item>
/// </list>
/// Only the relevant fields are populated for each action; all others are absent.
/// </summary>
public sealed class SdkActionResult
{
    /// <summary>SDK version string. Present for the Version action.</summary>
    [JsonPropertyName("version")]
    public string? Version { get; init; }

    /// <summary>Installed SDKs. Present for the ListSdks action.</summary>
    [JsonPropertyName("sdks")]
    public InstalledSdkInfo[]? Sdks { get; init; }

    /// <summary>Installed runtimes. Present for ListSdks and ListRuntimes actions.</summary>
    [JsonPropertyName("runtimes")]
    public InstalledRuntimeInfo[]? Runtimes { get; init; }
}

/// <summary>
/// Structured result for the dotnet_solution List action.
/// </summary>
public sealed class SolutionListResult
{
    /// <summary>
    /// Paths of all projects in the solution.
    /// Paths are relative to the solution file directory, as reported by <c>dotnet solution list</c>.
    /// </summary>
    [JsonPropertyName("projects")]
    public string[] Projects { get; init; } = [];
}

/// <summary>
/// Metadata for a single NuGet package as reported by <c>dotnet list package</c>.
/// </summary>
public sealed class PackageInfo
{
    /// <summary>Package ID (e.g. "Newtonsoft.Json").</summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>Requested version constraint from the project file (e.g. "13.0.1").</summary>
    [JsonPropertyName("requestedVersion")]
    public string? RequestedVersion { get; init; }

    /// <summary>Resolved (actual) version installed (e.g. "13.0.3").</summary>
    [JsonPropertyName("resolvedVersion")]
    public string? ResolvedVersion { get; init; }
}

/// <summary>
/// Structured result for the dotnet_package List action.
/// </summary>
public sealed class PackageListResult
{
    /// <summary>NuGet packages referenced by the project.</summary>
    [JsonPropertyName("packages")]
    public PackageInfo[] Packages { get; init; } = [];
}
