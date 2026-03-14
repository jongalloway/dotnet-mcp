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
    /// Whether the server supports telemetry reporting.
    /// When enabled, the server emits request duration logs and follows OpenTelemetry semantic conventions (SDK v0.6+).
    /// In-memory per-tool metrics are accessible via the dotnet_server_metrics tool.
    /// See doc/telemetry.md for configuration details.
    /// </summary>
    [JsonPropertyName("telemetry")]
    public bool Telemetry { get; init; }

    /// <summary>
    /// Whether the server collects and exposes in-memory tool invocation metrics.
    /// When true, use dotnet_server_metrics to retrieve per-tool counts, average durations,
    /// and success/failure rates collected by the MCP message filter.
    /// </summary>
    [JsonPropertyName("metrics")]
    public bool Metrics { get; init; }

    /// <summary>
    /// Whether the server supports MCP Task augmentation for long-running operations such as
    /// build, test, and publish. When true, AI clients can call supported tools as async tasks
    /// and poll for completion instead of blocking on the result.
    /// </summary>
    [JsonPropertyName("asyncTasks")]
    public bool AsyncTasks { get; init; }

    /// <summary>
    /// Whether the server provides a predefined prompt catalog for common .NET development workflows.
    /// When true, clients can discover and use prompts via the MCP prompts protocol.
    /// </summary>
    [JsonPropertyName("prompts")]
    public bool Prompts { get; init; }

    /// <summary>
    /// Whether the server uses MCP Elicitation to request user confirmation before destructive operations.
    /// When true, clients that support elicitation will receive confirmation requests before actions
    /// such as Clean and solution Remove.
    /// </summary>
    [JsonPropertyName("elicitation")]
    public bool Elicitation { get; init; }

    /// <summary>
    /// Whether the server provides argument autocomplete suggestions for prompt arguments and resource
    /// template parameters (template names, framework TFMs, configurations, runtime identifiers).
    /// </summary>
    [JsonPropertyName("completions")]
    public bool Completions { get; init; }

    /// <summary>
    /// Whether the server uses MCP Sampling to request LLM completions from the client for
    /// intelligent error interpretation. When true, clients that support sampling will receive
    /// AI-assisted analysis of build and test failures.
    /// </summary>
    [JsonPropertyName("sampling")]
    public bool Sampling { get; init; }

    /// <summary>
    /// Whether the server reports progress notifications for long-running operations.
    /// When true, clients that supply a progress token will receive start and completion
    /// progress notifications during build, test, publish, and other slow operations
    /// via the MCP progress notification protocol.
    /// </summary>
    [JsonPropertyName("progressNotifications")]
    public bool ProgressNotifications { get; init; }
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
