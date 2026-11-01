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
    public static CallToolResult ToCallToolResult(string text, object? structuredContent = null, bool isError = false)
    {
        if (structuredContent != null)
        {
            var payload = JsonSerializer.SerializeToElement(structuredContent, _defaultOptions);
            var content = new List<ContentBlock> { new TextContentBlock { Text = text } };
            if (!RepresentsPayload(text, payload))
                content.Add(new TextContentBlock { Text = payload.GetRawText() });
            return new CallToolResult
            {
                Content = content,
                StructuredContent = payload,
                IsError = isError ? true : null
            };
        }

        return new CallToolResult
        {
            Content = [new TextContentBlock { Text = text }],
            IsError = isError ? true : null
        };
    }

    private static bool RepresentsPayload(string text, JsonElement payload)
    {
        try { return JsonElement.DeepEquals(JsonSerializer.Deserialize<JsonElement>(text), payload); }
        catch (JsonException) { return false; }
    }

    internal static bool IsFailure(string text, string? command = null)
    {
        var trimmed = text.TrimStart();
        const string prefix = "Exit Code: ";
        var index = text.LastIndexOf(prefix, StringComparison.Ordinal);
        if (index >= 0)
        {
            var codeText = text[(index + prefix.Length)..].Split(['\r', '\n'])[0];
            if (int.TryParse(codeText.Trim(), out var code))
                return code != 0 && ErrorResultFactory.CreateResult(string.Empty, string.Empty, code, command) is not SuccessResult;
        }
        if (trimmed.StartsWith("Error:", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("Error accessing ", StringComparison.Ordinal) ||
            trimmed.StartsWith("Error getting ", StringComparison.Ordinal) ||
            trimmed.StartsWith("Error searching ", StringComparison.Ordinal) ||
            trimmed.StartsWith("Template '", StringComparison.Ordinal) && trimmed.Contains("' not found.", StringComparison.Ordinal))
            return true;
        if (trimmed.StartsWith('{'))
        {
            try
            {
                var value = JsonSerializer.Deserialize<JsonElement>(text);
                return value.TryGetProperty("success", out var success) && success.ValueKind == JsonValueKind.False;
            }
            catch (JsonException) { }
        }
        return false;
    }

    internal static CallToolResult ApplicationError(string text)
    {
        return ToCallToolResult(text, new ApplicationErrorResult { Error = text }, isError: true);
    }

    internal static CallToolResult ActionResult<TAction>(TAction action, string text, object? payload = null)
        where TAction : struct, Enum =>
        IsFailure(text) ? ApplicationError(text) :
            ToCallToolResult(text, payload ?? new ActionTextResult { Action = action.ToString(), Text = text });

    /// <summary>
    /// Extract text content from a CallToolResult for testing and comparison.
    /// Returns the text from the first TextContentBlock, or empty string if none.
    /// </summary>
    public static string GetText(this CallToolResult result)
    {
        return result.Content.OfType<TextContentBlock>().FirstOrDefault()?.Text ?? string.Empty;
    }
}
