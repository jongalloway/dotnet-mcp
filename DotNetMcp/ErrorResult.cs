using System.Text.Json.Serialization;

namespace DotNetMcp;

/// <summary>
/// Represents a structured error result from a .NET CLI command execution.
/// </summary>
public sealed class ErrorResult
{
    /// <summary>
    /// Error code (e.g., "CS1001", "MSB3644", "NU1101", or "EXIT_1" for generic errors)
    /// </summary>
    [JsonPropertyName("code")]
    public string Code { get; init; } = string.Empty;

    /// <summary>
    /// Human-readable error message
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Error category (Compilation, Build, Package, Runtime, Validation, Unknown)
    /// </summary>
    [JsonPropertyName("category")]
    public string Category { get; init; } = "Unknown";

    /// <summary>
    /// Optional hint or suggestion for fixing the error
    /// </summary>
    [JsonPropertyName("hint")]
    public string? Hint { get; init; }

    /// <summary>
    /// Plain English explanation of what this error means
    /// </summary>
    [JsonPropertyName("explanation")]
    public string? Explanation { get; init; }

    /// <summary>
    /// URL to official documentation for this error
    /// </summary>
    [JsonPropertyName("documentationUrl")]
    public string? DocumentationUrl { get; init; }

    /// <summary>
    /// List of suggested fixes for this error
    /// </summary>
    [JsonPropertyName("suggestedFixes")]
    public List<string>? SuggestedFixes { get; init; }

    /// <summary>
    /// Optional list of alternative approaches when a capability is unavailable.
    /// This is primarily used for CAPABILITY_NOT_AVAILABLE responses.
    /// </summary>
    [JsonPropertyName("alternatives")]
    public List<string>? Alternatives { get; init; }

    /// <summary>
    /// Original raw output for reference (sanitized to remove sensitive data)
    /// </summary>
    [JsonPropertyName("rawOutput")]
    public string RawOutput { get; init; } = string.Empty;

    /// <summary>
    /// MCP (Model Context Protocol) error code following JSON-RPC 2.0 specification.
    /// Common codes: -32002 (ResourceNotFound), -32602 (InvalidParams), -32603 (InternalError).
    /// Only set when an MCP error code is applicable to this error.
    /// </summary>
    [JsonPropertyName("mcpErrorCode")]
    public int? McpErrorCode { get; init; }

    /// <summary>
    /// Structured data payload with actionable details for programmatic error handling.
    /// Contains information like exit code, command arguments, and error details (with secrets redacted).
    /// </summary>
    [JsonPropertyName("data")]
    public ErrorData? Data { get; init; }
}

/// <summary>
/// Structured data payload for error results, providing actionable details for debugging.
/// All sensitive information is redacted before inclusion.
/// </summary>
public sealed class ErrorData
{
    /// <summary>
    /// The command that was executed (e.g., "dotnet build MyProject.csproj")
    /// </summary>
    [JsonPropertyName("command")]
    public string? Command { get; init; }

    /// <summary>
    /// Process exit code
    /// </summary>
    [JsonPropertyName("exitCode")]
    public int? ExitCode { get; init; }

    /// <summary>
    /// Standard error output from the command (redacted for sensitive information)
    /// </summary>
    [JsonPropertyName("stderr")]
    public string? Stderr { get; init; }

    /// <summary>
    /// Additional context-specific data as key-value pairs
    /// </summary>
    [JsonPropertyName("additionalData")]
    public Dictionary<string, string>? AdditionalData { get; init; }
}

/// <summary>
/// Represents a structured success result from a .NET CLI command execution.
/// </summary>
public sealed class SuccessResult
{
    /// <summary>
    /// Indicates the operation was successful
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; init; } = true;

    /// <summary>
    /// The command that was executed (e.g., "dotnet build MyProject.csproj").
    /// Included for machine-readable output to support logging and diagnostics.
    /// </summary>
    [JsonPropertyName("command")]
    public string? Command { get; init; }

    /// <summary>
    /// Output from the command
    /// </summary>
    [JsonPropertyName("output")]
    public string Output { get; init; } = string.Empty;

    /// <summary>
    /// Exit code (0 for success)
    /// </summary>
    [JsonPropertyName("exitCode")]
    public int ExitCode { get; init; } = 0;
}

/// <summary>
/// Represents a structured error response from a .NET CLI command execution.
/// </summary>
public sealed class ErrorResponse
{
    /// <summary>
    /// Indicates the operation failed
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; init; } = false;

    /// <summary>
    /// List of parsed errors
    /// </summary>
    [JsonPropertyName("errors")]
    public List<ErrorResult> Errors { get; init; } = new();

    /// <summary>
    /// Exit code from the command
    /// </summary>
    [JsonPropertyName("exitCode")]
    public int ExitCode { get; init; }
}
