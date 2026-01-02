using System.Text.RegularExpressions;

namespace DotNetMcp;

/// <summary>
/// Factory class for parsing .NET CLI output and creating structured error objects.
/// Handles common error codes: CS#### (C# compiler), MSB#### (MSBuild), NU#### (NuGet)
/// </summary>
public static partial class ErrorResultFactory
{
    /// <summary>
    /// Create a standardized error response for when a capability exists but is not available
    /// due to environment limitations, feature flags, or unimplemented functionality.
    /// </summary>
    /// <param name="feature">The capability/feature name (e.g., "dotnet CLI", "telemetry")</param>
    /// <param name="alternatives">Optional alternative suggestions for how to proceed</param>
    /// <param name="command">Optional command context (e.g., "dotnet build")</param>
    /// <param name="details">Optional additional details (e.g., exception message)</param>
    public static ErrorResponse ReturnCapabilityNotAvailable(
        string feature,
        IEnumerable<string>? alternatives = null,
        string? command = null,
        string? details = null)
    {
        var safeFeature = string.IsNullOrWhiteSpace(feature) ? "capability" : feature.Trim();

        var altList = alternatives
            ?.Where(a => !string.IsNullOrWhiteSpace(a))
            .Select(a => a.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Set to null if the list is empty after filtering
        if (altList?.Count == 0)
        {
            altList = null;
        }

        var message = $"Capability '{safeFeature}' is not available in the current environment.";
        if (!string.IsNullOrWhiteSpace(details))
        {
            message += $" Details: {details.Trim()}";
        }

        return new ErrorResponse
        {
            Success = false,
            Errors = new List<ErrorResult>
            {
                new ErrorResult
                {
                    Code = "CAPABILITY_NOT_AVAILABLE",
                    Message = message,
                    Category = "Capability",
                    Hint = altList?.Count > 0 
                        ? "Try one of the alternatives or adjust the environment to enable this capability."
                        : "Adjust the environment to enable this capability.",
                    Alternatives = altList,
                    RawOutput = string.Empty,
                    McpErrorCode = McpErrorCodes.CapabilityNotAvailable,
                    Data = CreateErrorData(command, exitCode: -1, stderr: details ?? string.Empty)
                }
            },
            ExitCode = -1
        };
    }

    // Regular expressions for parsing common error patterns
    [GeneratedRegex(@"(?<file>[^(]+)\((?<line>\d+),(?<col>\d+)\):\s+(?<severity>error|warning)\s+(?<code>[A-Z]+\d+):\s+(?<message>.+)")]
    private static partial Regex CompilerErrorRegex();

    [GeneratedRegex(@"(?<severity>error|warning)\s+(?<code>[A-Z]+\d+):\s+(?<message>.+)")]
    private static partial Regex GenericErrorRegex();

    [GeneratedRegex(@"(?<code>NU\d+):\s+(?<message>.+)")]
    private static partial Regex NuGetErrorRegex();

    /// <summary>
    /// Parse CLI output and create structured error response.
    /// </summary>
    /// <param name="output">Standard output from the command</param>
    /// <param name="error">Standard error from the command</param>
    /// <param name="exitCode">Exit code from the command</param>
    /// <param name="command">Optional command that was executed for structured data</param>
    /// <returns>ErrorResponse with parsed errors or SuccessResult if exitCode is 0</returns>
    public static object CreateResult(string output, string error, int exitCode, string? command = null)
    {
        // Success case
        if (exitCode == 0)
        {
            return new SuccessResult
            {
                Success = true,
                Command = string.IsNullOrWhiteSpace(command) ? null : SanitizeOutput(command),
                Output = SanitizeOutput(output),
                ExitCode = 0
            };
        }

        // Error case - parse errors from output
        var errors = new List<ErrorResult>();
        var combinedOutput = $"{output}\n{error}";
        var lines = combinedOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // Parse errors from each line using LINQ
        errors.AddRange(lines
            .Select(line => ParseErrorLine(line, error, exitCode, command))
            .OfType<ErrorResult>());

        // If no specific errors were parsed, create a generic error
        if (errors.Count == 0)
        {
            // Truncate long error messages to avoid verbose fallback errors
            var errorMessage = string.IsNullOrWhiteSpace(error)
                ? "Command failed with no error output"
                : error.Length > 500 ? error[..500] + "..." : error.Trim();

            var genericCode = $"EXIT_{exitCode}";
            var category = "Unknown";
            var mcpErrorCode = McpErrorCodes.GetMcpErrorCode(genericCode, category, exitCode);

            errors.Add(new ErrorResult
            {
                Code = genericCode,
                Message = errorMessage,
                Category = category,
                Hint = "Check the command syntax and arguments",
                RawOutput = SanitizeOutput(combinedOutput),
                McpErrorCode = mcpErrorCode,
                Data = CreateErrorData(command, exitCode, error)
            });
        }

        return new ErrorResponse
        {
            Success = false,
            Errors = errors,
            ExitCode = exitCode
        };
    }

    /// <summary>
    /// Parse a single line and extract error information if present.
    /// </summary>
    private static ErrorResult? ParseErrorLine(string line, string stderr, int exitCode, string? command)
    {
        // Try compiler error format first (most specific)
        var compilerMatch = CompilerErrorRegex().Match(line);
        if (compilerMatch.Success)
        {
            var code = compilerMatch.Groups["code"].Value;
            var category = GetCategory(code);
            var mcpErrorCode = McpErrorCodes.GetMcpErrorCode(code, category, exitCode);
            var errorInfo = ErrorCodeDictionary.GetErrorInfo(code);

            return new ErrorResult
            {
                Code = code,
                Message = compilerMatch.Groups["message"].Value.Trim(),
                Category = category,
                Hint = GetHint(code),
                Explanation = errorInfo?.Explanation,
                DocumentationUrl = errorInfo?.DocumentationUrl,
                SuggestedFixes = errorInfo?.SuggestedFixes,
                RawOutput = SanitizeOutput(line),
                McpErrorCode = mcpErrorCode,
                Data = CreateErrorData(command, exitCode, stderr)
            };
        }

        // Try NuGet error format
        var nugetMatch = NuGetErrorRegex().Match(line);
        if (nugetMatch.Success)
        {
            var code = nugetMatch.Groups["code"].Value;
            var category = "Package";
            var mcpErrorCode = McpErrorCodes.GetMcpErrorCode(code, category, exitCode);
            var errorInfo = ErrorCodeDictionary.GetErrorInfo(code);

            return new ErrorResult
            {
                Code = code,
                Message = nugetMatch.Groups["message"].Value.Trim(),
                Category = category,
                Hint = GetHint(code),
                Explanation = errorInfo?.Explanation,
                DocumentationUrl = errorInfo?.DocumentationUrl,
                SuggestedFixes = errorInfo?.SuggestedFixes,
                RawOutput = SanitizeOutput(line),
                McpErrorCode = mcpErrorCode,
                Data = CreateErrorData(command, exitCode, stderr)
            };
        }

        // Try generic error format
        var genericMatch = GenericErrorRegex().Match(line);
        if (genericMatch.Success)
        {
            var code = genericMatch.Groups["code"].Value;
            var category = GetCategory(code);
            var mcpErrorCode = McpErrorCodes.GetMcpErrorCode(code, category, exitCode);
            var errorInfo = ErrorCodeDictionary.GetErrorInfo(code);

            return new ErrorResult
            {
                Code = code,
                Message = genericMatch.Groups["message"].Value.Trim(),
                Category = category,
                Hint = GetHint(code),
                Explanation = errorInfo?.Explanation,
                DocumentationUrl = errorInfo?.DocumentationUrl,
                SuggestedFixes = errorInfo?.SuggestedFixes,
                RawOutput = SanitizeOutput(line),
                McpErrorCode = mcpErrorCode,
                Data = CreateErrorData(command, exitCode, stderr)
            };
        }

        return null;
    }

    /// <summary>
    /// Determine error category based on error code prefix.
    /// </summary>
    private static string GetCategory(string code)
    {
        if (code.StartsWith("CS", StringComparison.OrdinalIgnoreCase))
            return "Compilation";

        if (code.StartsWith("MSB", StringComparison.OrdinalIgnoreCase))
            return "Build";

        if (code.StartsWith("NU", StringComparison.OrdinalIgnoreCase))
            return "Package";

        if (code.StartsWith("NETSDK", StringComparison.OrdinalIgnoreCase))
            return "SDK";

        return "Unknown";
    }

    /// <summary>
    /// Get hints for common error codes.
    /// </summary>
    private static string? GetHint(string code)
    {
        return code.ToUpperInvariant() switch
        {
            // C# Compiler errors
            "CS0103" => "The name does not exist in the current context. Check for typos or missing using directives.",
            "CS1001" => "Identifier expected. Check for syntax errors or missing identifiers.",
            "CS1002" => "Expected semicolon. Check for missing semicolons.",
            "CS1513" => "Expected closing brace. Check for mismatched braces.",
            "CS0246" => "Type or namespace not found. Check for missing using directives or package references.",

            // MSBuild errors
            "MSB3644" => "The reference assemblies were not found. Install the .NET SDK or targeting pack for the specified framework.",
            "MSB4236" => "The SDK could not be found. Ensure the .NET SDK is installed and in PATH.",
            "MSB1003" => "Specify a project or solution file. The directory does not contain one.",

            // NuGet errors
            "NU1101" => "Unable to find package. Check package name and source.",
            "NU1102" => "Unable to find package with version. Check version number.",
            "NU1103" => "Unable to find a stable package. Consider using --prerelease.",
            "NU1605" => "Detected package downgrade. Check package version constraints.",

            // SDK errors
            "NETSDK1045" => "The current .NET SDK does not support targeting this framework. Update the SDK or change the target framework.",
            "NETSDK1004" => "Assets file not found. Run 'dotnet restore' to generate it.",

            _ => null
        };
    }

    /// <summary>
    /// Sanitize output to remove sensitive data like passwords, tokens, etc., from the output.
    /// Uses SecretRedactor for consistent redaction patterns across the application.
    /// </summary>
    private static string SanitizeOutput(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
            return output;

        // Use SecretRedactor for consistent redaction
        return SecretRedactor.Redact(output);
    }

    /// <summary>
    /// Maximum length for stderr in structured error data before truncation.
    /// </summary>
    private const int MaxStderrLength = 1000;

    /// <summary>
    /// Truncation suffix appended to truncated stderr messages.
    /// </summary>
    private const string TruncationSuffix = "... (truncated)";

    /// <summary>
    /// Create structured error data payload with sanitized command and stderr strings and the raw exit code.
    /// </summary>
    private static ErrorData? CreateErrorData(string? command, int exitCode, string stderr)
    {
        // Create data if we have meaningful information (command, stderr, or non-zero exit code)
        if (string.IsNullOrWhiteSpace(command) && string.IsNullOrWhiteSpace(stderr) && exitCode == 0)
        {
            return null;
        }

        // Sanitize command and stderr to remove sensitive information
        var sanitizedCommand = string.IsNullOrWhiteSpace(command) ? null : SanitizeOutput(command);
        var sanitizedStderr = string.IsNullOrWhiteSpace(stderr) ? null : SanitizeOutput(stderr);

        // Truncate stderr if it's too long, accounting for the suffix length
        if (sanitizedStderr != null && sanitizedStderr.Length > MaxStderrLength)
        {
            sanitizedStderr = sanitizedStderr[..(MaxStderrLength - TruncationSuffix.Length)] + TruncationSuffix;
        }

        return new ErrorData
        {
            Command = sanitizedCommand,
            ExitCode = exitCode,
            Stderr = sanitizedStderr
        };
    }

    /// <summary>
    /// Create a concurrency conflict error result.
    /// </summary>
    /// <param name="operationType">The type of operation that was attempted</param>
    /// <param name="target">The target resource</param>
    /// <param name="conflictingOperation">Description of the conflicting operation</param>
    /// <returns>ErrorResponse with CONCURRENCY_CONFLICT error</returns>
    public static ErrorResponse CreateConcurrencyConflict(string operationType, string target, string conflictingOperation)
    {
        var code = "CONCURRENCY_CONFLICT";
        var category = "Concurrency";
        var mcpErrorCode = McpErrorCodes.GetMcpErrorCode(code, category, -1);

        return new ErrorResponse
        {
            Success = false,
            Errors = new List<ErrorResult>
            {
                new ErrorResult
                {
                    Code = code,
                    Message = $"Cannot execute '{operationType}' on '{target}' because a conflicting operation is already in progress: {conflictingOperation}",
                    Category = category,
                    Hint = "Wait for the conflicting operation to complete, or cancel it before retrying this operation.",
                    RawOutput = string.Empty,
                    McpErrorCode = mcpErrorCode,
                    Data = new ErrorData
                    {
                        ExitCode = -1,
                        AdditionalData = new Dictionary<string, string>
                        {
                            ["operationType"] = SanitizeOutput(operationType),
                            ["target"] = SanitizeOutput(target),
                            ["conflictingOperation"] = SanitizeOutput(conflictingOperation)
                        }
                    }
                }
            },
            ExitCode = -1
        };
    }

    /// <summary>
    /// Create a capability not available error result.
    /// Used when a feature exists but cannot be executed due to missing dependencies,
    /// feature flags being disabled, OS limitations, or not yet being implemented.
    /// </summary>
    /// <param name="feature">The feature or capability that is not available</param>
    /// <param name="reason">Why the capability is not available (e.g., "Not yet implemented", "Requires Windows", "Feature flag disabled")</param>
    /// <param name="alternatives">List of alternative actions or tools to use instead</param>
    /// <returns>ErrorResponse with CAPABILITY_NOT_AVAILABLE error</returns>
    public static ErrorResponse ReturnCapabilityNotAvailable(string feature, string reason, List<string>? alternatives = null)
    {
        var code = "CAPABILITY_NOT_AVAILABLE";
        var category = "Capability";
        var mcpErrorCode = McpErrorCodes.GetMcpErrorCode(code, category, -1);

        return new ErrorResponse
        {
            Success = false,
            Errors = new List<ErrorResult>
            {
                new ErrorResult
                {
                    Code = code,
                    Message = $"The '{feature}' capability is not available: {reason}",
                    Category = category,
                    Hint = alternatives?.Count > 0 
                        ? "Consider using one of the suggested alternatives." 
                        : "This feature is not currently supported in this environment.",
                    Explanation = "This tool/feature exists but cannot be executed in the current environment or configuration. This may be due to missing dependencies, disabled feature flags, OS limitations, or features that are planned but not yet implemented.",
                    Alternatives = alternatives,
                    RawOutput = string.Empty,
                    McpErrorCode = mcpErrorCode,
                    Data = new ErrorData
                    {
                        ExitCode = -1,
                        AdditionalData = new Dictionary<string, string>
                        {
                            ["feature"] = SanitizeOutput(feature),
                            ["reason"] = SanitizeOutput(reason)
                        }
                    }
                }
            },
            ExitCode = -1
        };
    }

    /// <summary>
    /// Create a validation error result for invalid method parameters.
    /// Used when parameters fail validation before command execution.
    /// </summary>
    /// <param name="message">Human-readable error message</param>
    /// <param name="parameterName">Optional name of the parameter that failed validation</param>
    /// <param name="reason">Optional reason why the parameter is invalid (e.g., "required", "invalid format")</param>
    /// <returns>ErrorResponse with INVALID_PARAMS error code</returns>
    public static ErrorResponse CreateValidationError(string message, string? parameterName = null, string? reason = null)
    {
        var code = "INVALID_PARAMS";
        var category = "Validation";
        var mcpErrorCode = McpErrorCodes.InvalidParams;

        var additionalData = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(parameterName))
        {
            additionalData["parameter"] = SanitizeOutput(parameterName);
        }
        if (!string.IsNullOrWhiteSpace(reason))
        {
            additionalData["reason"] = SanitizeOutput(reason);
        }

        return new ErrorResponse
        {
            Success = false,
            Errors = new List<ErrorResult>
            {
                new ErrorResult
                {
                    Code = code,
                    Message = SanitizeOutput(message),
                    Category = category,
                    Hint = "Verify the parameter values and try again.",
                    RawOutput = string.Empty,
                    McpErrorCode = mcpErrorCode,
                    Data = new ErrorData
                    {
                        Command = null, // No command executed for validation errors
                        ExitCode = -1,
                        AdditionalData = additionalData.Count > 0 ? additionalData : null
                    }
                }
            },
            ExitCode = -1
        };
    }

    /// <summary>
    /// Format result as JSON string.
    /// </summary>
    public static string ToJson(object result)
    {
        return System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
    }
}
