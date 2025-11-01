using System.Text.RegularExpressions;

namespace DotNetMcp;

/// <summary>
/// Provides security redaction for potentially sensitive information in CLI output.
/// Redacts connection strings, passwords, tokens, and other sensitive patterns.
/// </summary>
public static class SecretRedactor
{
    // Redaction placeholder
    private const string RedactedPlaceholder = "[REDACTED]";

    /// <summary>
    /// Redaction patterns for various types of secrets.
    /// Each pattern is designed to match sensitive information while minimizing false positives.
    /// </summary>
    private static readonly Regex[] RedactionPatterns =
    [
        // Connection strings (SQL Server, MySQL, PostgreSQL, MongoDB, etc.)
        // Matches: Server=...;Password=secret;... or pwd=secret or Connection String=...
        new Regex(
            @"(?i)(password|pwd|passwd|pass)\s*[=:]\s*([""']?)([^;""'\s]+)\2",
            RegexOptions.Compiled),

        // Connection string user IDs with passwords nearby (more context-aware)
        new Regex(
            @"(?i)(user\s*id|uid|username|user)\s*[=:]\s*([""']?)([^;""'\s]+)\2\s*;?\s*(password|pwd|passwd|pass)\s*[=:]\s*([""']?)([^;""'\s]+)\5",
            RegexOptions.Compiled),

        // MongoDB connection strings
        // Matches: mongodb://user:password@host or mongodb+srv://user:password@host
        new Regex(
            @"mongodb(\+srv)?://([^:]+):([^@]+)@",
            RegexOptions.Compiled),

        // Generic credentials in URLs
        // Matches: protocol://user:password@host
        new Regex(
            @"([a-zA-Z][a-zA-Z0-9+.-]*://)([^:]+):([^@]+)@",
            RegexOptions.Compiled),

        // API keys and tokens (various formats)
        // Matches: api_key=... or apiKey=... or token=... or bearer ...
        new Regex(
            @"(?i)(api[-_]?key|apikey|access[-_]?token|auth[-_]?token|bearer[-_]?token|client[-_]?secret|api[-_]?secret)\s*[=:]\s*([""']?)([a-zA-Z0-9_\-]{20,})\2",
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
        // Only matches if labeled as secret/key/token/password in nearby text
        new Regex(
            @"(?i)(?:secret|key|token|password|credential)\s*[=:]\s*([""']?)([a-zA-Z0-9+/=_-]{32,})\1",
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
            // For patterns with multiple groups, preserve the key part and redact the value
            // Group 0 is always the full match
            
            // Handle connection string password patterns (Group 1 = key, Group 3 = value)
            if (match.Groups.Count >= 4 && match.Groups[1].Success && match.Groups[3].Success)
            {
                var key = match.Groups[1].Value;
                var quote = match.Groups[2].Success ? match.Groups[2].Value : string.Empty;
                return $"{key}={quote}{RedactedPlaceholder}{quote}";
            }

            // Handle URL-based credentials (protocol://user:password@host)
            if (match.Groups.Count >= 4 && match.Groups[1].Success && match.Value.Contains("://"))
            {
                // For MongoDB: mongodb://user:[REDACTED]@host
                // For other URLs: protocol://user:[REDACTED]@host
                var protocol = match.Groups[1].Value;
                var user = match.Groups[2].Success ? match.Groups[2].Value : "user";
                return $"{protocol}{user}:{RedactedPlaceholder}@";
            }

            // Handle key=value patterns (Group 1 = key, Group 3 = value)
            if (match.Groups.Count >= 3 && match.Groups[1].Success)
            {
                var key = match.Groups[1].Value;
                var separator = match.Value.Contains('=') ? "=" : ":";
                var quote = match.Groups.Count >= 3 && match.Groups[2].Success ? match.Groups[2].Value : string.Empty;
                return $"{key}{separator}{quote}{RedactedPlaceholder}{quote}";
            }

            // For patterns without structured groups (like JWT or PEM), redact the entire match
            return RedactedPlaceholder;
        });
    }
}
