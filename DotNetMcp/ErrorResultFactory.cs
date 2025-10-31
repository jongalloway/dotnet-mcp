using System.Text.RegularExpressions;

namespace DotNetMcp;

/// <summary>
/// Factory class for parsing .NET CLI output and creating structured error objects.
/// Handles common error codes: CS#### (C# compiler), MSB#### (MSBuild), NU#### (NuGet)
/// </summary>
public static partial class ErrorResultFactory
{
    // Regular expressions for parsing common error patterns
    [GeneratedRegex(@"(?<file>[^(]+)\((?<line>\d+),(?<col>\d+)\):\s+(?<severity>error|warning)\s+(?<code>[A-Z]+\d+):\s+(?<message>.+)")]
    private static partial Regex CompilerErrorRegex();

    [GeneratedRegex(@"(?<severity>error|warning)\s+(?<code>[A-Z]+\d+):\s+(?<message>.+)")]
    private static partial Regex GenericErrorRegex();

    [GeneratedRegex(@"(?<code>NU\d+):\s+(?<message>.+)")]
    private static partial Regex NuGetErrorRegex();

    // Patterns to detect and filter sensitive data
    private static readonly string[] SensitivePatterns = new[]
    {
        "password", "secret", "token", "apikey", "api-key", "api_key",
        "connectionstring", "connection-string", "connection_string",
        "credentials", "authorization", "bearer"
    };

    // Regex for masking sensitive values - compiled once and reused
    // Captures: (1) keyword, (2) separator (= or :), (3) optional whitespace, (4) value
    [GeneratedRegex(@"(password|secret|token|apikey|api-key|api_key|connectionstring|connection-string|connection_string|credentials|authorization|bearer)([\s]*[=:]\s*)[""']?([^\s""']+)[""']?", RegexOptions.IgnoreCase)]
    private static partial Regex SensitiveValueRegex();

    /// <summary>
    /// Parse CLI output and create structured error response.
    /// </summary>
    /// <param name="output">Standard output from the command</param>
    /// <param name="error">Standard error from the command</param>
    /// <param name="exitCode">Exit code from the command</param>
    /// <returns>ErrorResponse with parsed errors or SuccessResult if exitCode is 0</returns>
    public static object CreateResult(string output, string error, int exitCode)
    {
        // Success case
        if (exitCode == 0)
        {
            return new SuccessResult
            {
                Success = true,
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
            .Select(ParseErrorLine)
            .Where(errorResult => errorResult != null)!);

        // If no specific errors were parsed, create a generic error
        if (errors.Count == 0)
        {
            errors.Add(new ErrorResult
            {
                Code = $"EXIT_{exitCode}",
                Message = string.IsNullOrWhiteSpace(error) ? "Command failed with no error output" : error.Trim(),
                Category = "Unknown",
                Hint = "Check the command syntax and arguments",
                RawOutput = SanitizeOutput(combinedOutput)
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
    private static ErrorResult? ParseErrorLine(string line)
    {
        // Try compiler error format first (most specific)
        var compilerMatch = CompilerErrorRegex().Match(line);
        if (compilerMatch.Success)
        {
            var code = compilerMatch.Groups["code"].Value;
            return new ErrorResult
            {
                Code = code,
                Message = compilerMatch.Groups["message"].Value.Trim(),
                Category = GetCategory(code),
                Hint = GetHint(code),
                RawOutput = SanitizeOutput(line)
            };
        }

        // Try NuGet error format
        var nugetMatch = NuGetErrorRegex().Match(line);
        if (nugetMatch.Success)
        {
            var code = nugetMatch.Groups["code"].Value;
            return new ErrorResult
            {
                Code = code,
                Message = nugetMatch.Groups["message"].Value.Trim(),
                Category = "Package",
                Hint = GetHint(code),
                RawOutput = SanitizeOutput(line)
            };
        }

        // Try generic error format
        var genericMatch = GenericErrorRegex().Match(line);
        if (genericMatch.Success)
        {
            var code = genericMatch.Groups["code"].Value;
            return new ErrorResult
            {
                Code = code,
                Message = genericMatch.Groups["message"].Value.Trim(),
                Category = GetCategory(code),
                Hint = GetHint(code),
                RawOutput = SanitizeOutput(line)
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
    /// Sanitize output to remove sensitive data like passwords, tokens, etc.
    /// </summary>
    private static string SanitizeOutput(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
            return output;

        var sanitized = output;

        // Check if output contains any sensitive patterns using LINQ
        var containsSensitiveData = SensitivePatterns
            .Any(pattern => sanitized.Contains(pattern, StringComparison.OrdinalIgnoreCase));

        if (!containsSensitiveData)
            return sanitized;

        // Use pre-compiled regex to mask values after sensitive keywords
        sanitized = SensitiveValueRegex().Replace(sanitized, m =>
        {
            // Replace the captured value with redacted text, preserving the original separator
            var keyword = m.Groups[1].Value;
            var separator = m.Groups[2].Value;
            return $"{keyword}{separator}***REDACTED***";
        });

        return sanitized;
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
