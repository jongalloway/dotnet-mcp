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
    /// List of alternative actions or tools to use when a capability is not available.
    /// Only populated for CAPABILITY_NOT_AVAILABLE errors.
    /// </summary>
    [JsonPropertyName("alternatives")]
    public List<string>? Alternatives { get; init; }

    /// <summary>
    /// Source file where the error or warning occurred (for compiler errors).
    /// Populated from MSBuild/Roslyn diagnostics that include a file location.
    /// </summary>
    [JsonPropertyName("file")]
    public string? File { get; init; }

    /// <summary>
    /// Line number within the source file (1-based, for compiler errors).
    /// </summary>
    [JsonPropertyName("line")]
    public int? Line { get; init; }

    /// <summary>
    /// Column number within the source file (1-based, for compiler errors).
    /// </summary>
    [JsonPropertyName("column")]
    public int? Column { get; init; }

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
    /// Classified root cause of the error for programmatic handling.
    /// </summary>
    [JsonPropertyName("rootCauseKind")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public RootCauseKind? RootCauseKind { get; init; }

    /// <summary>
    /// Recommended next action to resolve the error.
    /// </summary>
    [JsonPropertyName("recommendedAction")]
    public RecommendedAction? RecommendedAction { get; init; }

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

    /// <summary>
    /// Optional metadata providing additional context about the operation.
    /// Used for tool-specific information like test runner selection.
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Represents a single compiler/MSBuild diagnostic (error or warning) with source location.
/// Used in <see cref="BuildResult"/> to report individual diagnostics from a build.
/// </summary>
public sealed class BuildDiagnostic
{
    /// <summary>
    /// Source file where the diagnostic occurred (relative or absolute path).
    /// </summary>
    [JsonPropertyName("file")]
    public string? File { get; init; }

    /// <summary>
    /// Line number within the source file (1-based).
    /// </summary>
    [JsonPropertyName("line")]
    public int? Line { get; init; }

    /// <summary>
    /// Column number within the source file (1-based).
    /// </summary>
    [JsonPropertyName("column")]
    public int? Column { get; init; }

    /// <summary>
    /// Diagnostic code (e.g., "CS0246", "MSB3644").
    /// </summary>
    [JsonPropertyName("code")]
    public string Code { get; init; } = string.Empty;

    /// <summary>
    /// Human-readable diagnostic message.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Severity: "error" or "warning".
    /// </summary>
    [JsonPropertyName("severity")]
    public string Severity { get; init; } = "error";
}

/// <summary>
/// Structured result of a <c>dotnet build</c> invocation, including counts and
/// individual compiler/MSBuild diagnostics so callers can diagnose failures
/// without re-parsing raw CLI output.
/// </summary>
public sealed class BuildResult
{
    /// <summary>
    /// <c>true</c> when the build succeeded (exit code 0).
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    /// <summary>
    /// The project or solution file that was built, if specified.
    /// </summary>
    [JsonPropertyName("project")]
    public string? Project { get; init; }

    /// <summary>
    /// Build configuration used (e.g., "Debug", "Release").
    /// </summary>
    [JsonPropertyName("configuration")]
    public string? Configuration { get; init; }

    /// <summary>
    /// Number of compiler/MSBuild errors.
    /// </summary>
    [JsonPropertyName("errorCount")]
    public int ErrorCount { get; init; }

    /// <summary>
    /// Number of compiler/MSBuild warnings.
    /// </summary>
    [JsonPropertyName("warningCount")]
    public int WarningCount { get; init; }

    /// <summary>
    /// Short summary line (e.g., "Build succeeded" or "Build FAILED (1 errors, 0 warnings)").
    /// </summary>
    [JsonPropertyName("summary")]
    public string Summary { get; init; } = string.Empty;

    /// <summary>
    /// Individual error diagnostics. <c>null</c> when there are no errors.
    /// </summary>
    [JsonPropertyName("errors")]
    public List<BuildDiagnostic>? Errors { get; init; }

    /// <summary>
    /// Individual warning diagnostics. <c>null</c> when there are no warnings.
    /// </summary>
    [JsonPropertyName("warnings")]
    public List<BuildDiagnostic>? Warnings { get; init; }
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

    /// <summary>
    /// Optional metadata providing additional context about the operation.
    /// Used for tool-specific information like test runner selection.
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; init; }
}
