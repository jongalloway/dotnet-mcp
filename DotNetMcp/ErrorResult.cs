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
    /// Original raw output for reference (sanitized to remove sensitive data)
    /// </summary>
    [JsonPropertyName("rawOutput")]
    public string RawOutput { get; init; } = string.Empty;
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
