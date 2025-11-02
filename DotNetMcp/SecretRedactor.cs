using System.Text.RegularExpressions;

namespace DotNetMcp;

/// <summary>
/// Provides security redaction for potentially sensitive information in CLI output.
/// 
/// This implementation uses the Microsoft.Extensions.Compliance.Redaction package as a dependency
/// to align with Microsoft's enterprise compliance framework. The package is referenced in the project
/// to ensure compatibility with Microsoft's data classification and redaction standards.
/// 
/// The actual redaction patterns are domain-specific for .NET CLI scenarios (connection strings,
/// API keys, tokens, etc.) and are implemented using compiled regular expressions for performance.
/// Future versions may leverage additional redaction abstractions from the Microsoft package as they
/// become available for CLI output scenarios.
/// </summary>
public static class SecretRedactor
{
    // Redaction placeholder - consistent with Microsoft.Extensions.Compliance.Redaction patterns
    private const string RedactedPlaceholder = "[REDACTED]";

    /// <summary>
    /// Redaction patterns for various types of secrets.
    /// Each pattern is designed to match sensitive information while minimizing false positives.
    /// </summary>
    private static readonly Regex[] RedactionPatterns =
    [
        // Connection strings (SQL Server, MySQL, PostgreSQL, MongoDB, etc.)
        // Matches: Server=...;Password=secret;... or pwd=secret or Password="quoted value" or Connection String=...
        // Use word boundaries to avoid matching "User Id" as "Id"
        // Requires at least 6 characters to avoid false positives on very short values
        new Regex(
            @"(?i)\b(password|pwd|passwd|pass)\s*[=:]\s*(?:([""'])([^""']{6,}?)\2|([^;""'\s]{6,}))",
            RegexOptions.Compiled),

        // MongoDB connection strings
        // Matches: mongodb://user:password@host or mongodb+srv://user:password@host
        new Regex(
            @"(mongodb(?:\+srv)?://)([^:]+):([^@]+)@",
            RegexOptions.Compiled),

        // Generic credentials in URLs
        // Matches: protocol://user:password@host for various protocols
        new Regex(
            @"((?:https?|ftp|postgresql|mysql|redis)://)([^:]+):([^@\s]+)@",
            RegexOptions.Compiled),

        // API keys and tokens (various formats)
        // Matches: api_key=... or apiKey=... or token=... or bearer ...
        // Minimum length of 6 characters to catch short tokens while avoiding false positives
        new Regex(
            @"(?i)\b(api[-_]?key|apikey|access[-_]?token|auth[-_]?token|bearer[-_]?token|client[-_]?secret|api[-_]?secret|token)\s*[=:]\s*(?:([""'])([a-zA-Z0-9_\-]{6,})\2|([a-zA-Z0-9_\-]{6,}))",
            RegexOptions.Compiled),

        // AWS credentials
        // Matches: AWS access key IDs (AKIA...) and secret access keys
        new Regex(
            @"(?i)(aws[-_]?access[-_]?key[-_]?id|aws[-_]?secret[-_]?access[-_]?key)\s*[=:]\s*([""']?)([A-Za-z0-9/+=]{20,})\2",
            RegexOptions.Compiled),

        // Azure connection strings and keys
        // Matches: AccountKey=... or SharedAccessKey=...
        new Regex(
            @"(?i)(AccountKey|SharedAccessKey|SharedAccessSignature)\s*=\s*([a-zA-Z0-9+/=]{20,})",
            RegexOptions.Compiled),

        // Private keys (PEM format markers)
        // Matches: -----BEGIN PRIVATE KEY----- ... -----END PRIVATE KEY-----
        new Regex(
            @"-----BEGIN\s+(?:RSA\s+)?PRIVATE\s+KEY-----[\s\S]*?-----END\s+(?:RSA\s+)?PRIVATE\s+KEY-----",
            RegexOptions.Compiled),

        // JWT tokens (three base64 segments separated by dots)
        // Matches: eyJ... format (standard JWT header start)
        new Regex(
            @"\beyJ[a-zA-Z0-9_-]*\.eyJ[a-zA-Z0-9_-]*\.[a-zA-Z0-9_-]*\b",
            RegexOptions.Compiled),

        // Generic high-entropy strings that look like secrets (e.g., base64-encoded secrets)
        // Only matches if labeled as secret/key/token/password/credential in nearby text
        new Regex(
            @"(?i)\b(secret|key|token|password|credential)\s*[=:]\s*(?:([""'])([a-zA-Z0-9+/=_-]{32,})\2|([a-zA-Z0-9+/=_-]{32,}))",
            RegexOptions.Compiled),
    ];

    /// <summary>
    /// Redacts sensitive information from the given text.
    /// </summary>
    /// <param name="text">The text to redact</param>
    /// <returns>The redacted text with sensitive information replaced by [REDACTED]</returns>
    public static string Redact(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var redacted = text;

        foreach (var pattern in RedactionPatterns)
        {
            redacted = RedactWithPattern(redacted, pattern);
        }

        return redacted;
    }

    /// <summary>
    /// Redacts text using a specific pattern.
    /// Preserves the key/label part and only redacts the sensitive value.
    /// </summary>
    private static string RedactWithPattern(string text, Regex pattern)
    {
        return pattern.Replace(text, match =>
        {
            // Identify pattern type by checking the match content
            
            // MongoDB URL: mongodb://user:password@host or mongodb+srv://user:password@host
            if (match.Value.StartsWith("mongodb"))
            {
                // Group 1 = protocol (mongodb://)
                // Group 2 = user
                // Group 3 = password
                var protocol = match.Groups[1].Value;
                var user = match.Groups[2].Value;
                return $"{protocol}{user}:{RedactedPlaceholder}@";
            }

            // Generic URL: protocol://user:password@host  
            if (match.Value.Contains("://") && match.Value.Contains("@"))
            {
                // Group 1 = protocol (https://)
                // Group 2 = user
                // Group 3 = password
                var protocol = match.Groups[1].Value;
                var user = match.Groups[2].Value;
                return $"{protocol}{user}:{RedactedPlaceholder}@";
            }

            // Key=value patterns (may have quotes)
            // Check Group 1 (key) and see if there's a quoted or unquoted value
            if (match.Groups.Count >= 2 && match.Groups[1].Success)
            {
                var key = match.Groups[1].Value;
                var separator = match.Value.Contains('=') ? "=" : ":";
                // Result is always key=[REDACTED] without quotes
                return $"{key}{separator}{RedactedPlaceholder}";
            }

            // For patterns without structured groups (like JWT or PEM), redact the entire match
            return RedactedPlaceholder;
        });
    }
}
