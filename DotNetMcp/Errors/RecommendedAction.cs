using System.Text.Json.Serialization;

namespace DotNetMcp;

/// <summary>
/// A recommended next action to resolve an error, suitable for programmatic consumption.
/// </summary>
public sealed class RecommendedAction
{
    /// <summary>
    /// The kind of action recommended.
    /// </summary>
    [JsonPropertyName("actionKind")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ActionKind ActionKind { get; init; }

    /// <summary>
    /// The MCP tool to call (when ActionKind is CallTool).
    /// </summary>
    [JsonPropertyName("toolName")]
    public string? ToolName { get; init; }

    /// <summary>
    /// Suggested arguments for the tool call.
    /// </summary>
    [JsonPropertyName("toolArgs")]
    public Dictionary<string, string>? ToolArgs { get; init; }

    /// <summary>
    /// A dotnet CLI command to run (when ActionKind is RunCommand).
    /// </summary>
    [JsonPropertyName("command")]
    public string? Command { get; init; }

    /// <summary>
    /// Human-readable description of the recommended action.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;
}

/// <summary>
/// The kind of action recommended to resolve an error.
/// </summary>
public enum ActionKind
{
    /// <summary>Call another MCP tool</summary>
    CallTool,

    /// <summary>Run a dotnet CLI command</summary>
    RunCommand,

    /// <summary>Manual user intervention needed</summary>
    ManualStep
}
