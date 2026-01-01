using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotNetMcp;

/// <summary>
/// Provides comprehensive error code information including explanations, documentation links, and suggested fixes.
/// </summary>
public static class ErrorCodeDictionary
{
    private static readonly Lazy<Dictionary<string, ErrorCodeInfo>> _errorCodes = new(LoadErrorCodes);

    /// <summary>
    /// Lookup error code information.
    /// </summary>
    /// <param name="errorCode">The error code to look up (e.g., "CS0103", "MSB3644", "NU1101")</param>
    /// <returns>ErrorCodeInfo if found, null otherwise</returns>
    public static ErrorCodeInfo? GetErrorInfo(string errorCode)
    {
        if (string.IsNullOrWhiteSpace(errorCode))
            return null;

        // Normalize to uppercase for lookup
        var normalizedCode = errorCode.ToUpperInvariant();
        return _errorCodes.Value.TryGetValue(normalizedCode, out var info) ? info : null;
    }

    /// <summary>
    /// Check if an error code has detailed information available.
    /// </summary>
    /// <param name="errorCode">The error code to check</param>
    /// <returns>True if information is available, false otherwise</returns>
    public static bool HasErrorInfo(string errorCode)
    {
        if (string.IsNullOrWhiteSpace(errorCode))
            return false;

        var normalizedCode = errorCode.ToUpperInvariant();
        return _errorCodes.Value.ContainsKey(normalizedCode);
    }

    /// <summary>
    /// Get the total number of error codes in the dictionary.
    /// </summary>
    public static int Count => _errorCodes.Value.Count;

    private static Dictionary<string, ErrorCodeInfo> LoadErrorCodes()
    {
        try
        {
            // Load from embedded resource
            var assembly = typeof(ErrorCodeDictionary).Assembly;
            var resourceName = "DotNetMcp.ErrorCodes.json";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream != null)
            {
                using var reader = new StreamReader(stream);
                var jsonContent = reader.ReadToEnd();
                return ParseErrorCodes(jsonContent);
            }

            // Fallback: try loading from file in same directory as assembly
            var assemblyLocation = assembly.Location;
            if (!string.IsNullOrEmpty(assemblyLocation))
            {
                var directory = Path.GetDirectoryName(assemblyLocation);
                if (directory != null)
                {
                    var filePath = Path.Combine(directory, "ErrorCodes.json");
                    if (File.Exists(filePath))
                    {
                        var json = File.ReadAllText(filePath);
                        return ParseErrorCodes(json);
                    }
                }
            }

            // Second fallback: try loading from current directory
            var currentDirPath = Path.Combine(Directory.GetCurrentDirectory(), "ErrorCodes.json");
            if (File.Exists(currentDirPath))
            {
                var json = File.ReadAllText(currentDirPath);
                return ParseErrorCodes(json);
            }

            return new Dictionary<string, ErrorCodeInfo>(StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception)
        {
            // Return empty dictionary on error - non-critical failure
            return new Dictionary<string, ErrorCodeInfo>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static Dictionary<string, ErrorCodeInfo> ParseErrorCodes(string json)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        var data = JsonSerializer.Deserialize<ErrorCodeData>(json, options);
        if (data?.ErrorCodes == null)
        {
            return new Dictionary<string, ErrorCodeInfo>(StringComparer.OrdinalIgnoreCase);
        }

        // Create case-insensitive dictionary
        var result = new Dictionary<string, ErrorCodeInfo>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in data.ErrorCodes)
        {
            result[kvp.Key.ToUpperInvariant()] = kvp.Value;
        }

        return result;
    }

    private sealed class ErrorCodeData
    {
        [JsonPropertyName("errorCodes")]
        public Dictionary<string, ErrorCodeInfo>? ErrorCodes { get; set; }
    }
}

/// <summary>
/// Detailed information about a specific error code.
/// </summary>
public sealed class ErrorCodeInfo
{
    /// <summary>
    /// Short title/summary of the error
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Detailed explanation of what causes this error
    /// </summary>
    [JsonPropertyName("explanation")]
    public string Explanation { get; set; } = string.Empty;

    /// <summary>
    /// Error category (Compilation, Build, Package, SDK, Runtime, etc.)
    /// </summary>
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Common causes of this error
    /// </summary>
    [JsonPropertyName("commonCauses")]
    public List<string> CommonCauses { get; set; } = new();

    /// <summary>
    /// Suggested fixes for this error
    /// </summary>
    [JsonPropertyName("suggestedFixes")]
    public List<string> SuggestedFixes { get; set; } = new();

    /// <summary>
    /// URL to official documentation
    /// </summary>
    [JsonPropertyName("documentationUrl")]
    public string? DocumentationUrl { get; set; }
}
