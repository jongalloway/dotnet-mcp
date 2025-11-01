using System.Text.Json.Serialization;

namespace DotNetMcp;

/// <summary>
/// Represents the capabilities and version information of the .NET MCP Server.
/// This provides a machine-readable snapshot for agent orchestration and discovery.
/// </summary>
public sealed class ServerCapabilities
{
    /// <summary>
    /// The version of the .NET MCP Server
    /// </summary>
    [JsonPropertyName("serverVersion")]
    public string ServerVersion { get; init; } = string.Empty;

    /// <summary>
    /// The Model Context Protocol version being used
    /// </summary>
    [JsonPropertyName("protocolVersion")]
    public string ProtocolVersion { get; init; } = string.Empty;

    /// <summary>
    /// Categories of tools supported by the server
    /// </summary>
    [JsonPropertyName("supportedCategories")]
    public string[] SupportedCategories { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Feature flags indicating what capabilities the server supports
    /// </summary>
    [JsonPropertyName("supports")]
    public ServerFeatureSupport Supports { get; init; } = new();

    /// <summary>
    /// Information about installed .NET SDKs and recommended versions
    /// </summary>
    [JsonPropertyName("sdkVersions")]
    public SdkVersionInfo SdkVersions { get; init; } = new();
}

/// <summary>
/// Feature support flags for the MCP server
/// </summary>
public sealed class ServerFeatureSupport
{
    /// <summary>
    /// Whether the server returns structured error responses in JSON format
    /// </summary>
    [JsonPropertyName("structuredErrors")]
    public bool StructuredErrors { get; init; }

    /// <summary>
    /// Whether the server supports machine-readable output format
    /// </summary>
    [JsonPropertyName("machineReadable")]
    public bool MachineReadable { get; init; }

    /// <summary>
    /// Whether the server supports cancellation of long-running operations
    /// </summary>
    [JsonPropertyName("cancellation")]
    public bool Cancellation { get; init; }

    /// <summary>
    /// Whether the server supports telemetry reporting (future feature)
    /// </summary>
    [JsonPropertyName("telemetry")]
    public bool Telemetry { get; init; }
}

/// <summary>
/// Information about .NET SDK versions
/// </summary>
public sealed class SdkVersionInfo
{
    /// <summary>
    /// List of installed SDK versions
    /// </summary>
    [JsonPropertyName("installed")]
    public string[] Installed { get; init; } = Array.Empty<string>();

    /// <summary>
    /// The recommended (latest) framework version
    /// </summary>
    [JsonPropertyName("recommended")]
    public string Recommended { get; init; } = string.Empty;

    /// <summary>
    /// The latest LTS (Long-Term Support) framework version
    /// </summary>
    [JsonPropertyName("lts")]
    public string Lts { get; init; } = string.Empty;
}
