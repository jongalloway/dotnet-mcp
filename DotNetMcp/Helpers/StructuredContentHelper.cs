using System.Text.Json;
using ModelContextProtocol.Protocol;

namespace DotNetMcp;

/// <summary>
/// Helper methods for creating structured CallToolResult responses.
/// </summary>
public static class StructuredContentHelper
{
    private static readonly JsonSerializerOptions _defaultOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Create a CallToolResult with both text content and structured content.
    /// </summary>
    public static CallToolResult ToCallToolResult(string text, object? structuredContent = null)
    {
        if (structuredContent != null)
        {
            return new CallToolResult
            {
                Content = [new TextContentBlock { Text = text }],
                StructuredContent = JsonSerializer.SerializeToElement(structuredContent, _defaultOptions)
            };
        }

        return new CallToolResult
        {
            Content = [new TextContentBlock { Text = text }]
        };
    }

    /// <summary>
    /// Extract text content from a CallToolResult for testing and comparison.
    /// Returns the text from the first TextContentBlock, or empty string if none.
    /// </summary>
    public static string GetText(this CallToolResult result)
    {
        return result.Content.OfType<TextContentBlock>().FirstOrDefault()?.Text ?? string.Empty;
    }
}
