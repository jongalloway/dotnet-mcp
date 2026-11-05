namespace DotNetMcp;

internal static class SdkHelperProtocol
{
    internal const int MaximumRequestBytes = 1024 * 1024;
    internal const int MaximumResponseBytes = 16 * 1024 * 1024;

    internal static async Task<byte[]> ReadMessageAsync(Stream stream, int maximumBytes, CancellationToken cancellationToken = default)
    {
        using var message = new MemoryStream();
        var buffer = new byte[8192];
        int count;
        while ((count = await stream.ReadAsync(buffer, cancellationToken)) != 0)
        {
            if (message.Length + count > maximumBytes)
                throw new InvalidDataException($"SDK helper message exceeded its {maximumBytes}-byte limit.");
            await message.WriteAsync(buffer.AsMemory(0, count), cancellationToken);
        }
        return message.ToArray();
    }
}

internal enum SdkHelperOperation
{
    AnalyzeProject, AnalyzeDependencies, ValidateProject, SetProperty, GetProperty,
    RemoveProperty, AddItem, RemoveItem, ListItems, ReadTemplates
}

internal sealed record SdkHelperRequest(
    SdkHelperOperation Operation,
    string? ProjectPath = null,
    string? PropertyName = null,
    string? PropertyValue = null,
    string? ItemType = null,
    string? Include = null,
    KeyValuePair<string, string>[]? Metadata = null);

internal sealed record SdkHelperResponse(string? Json = null, TemplateResourceMetadata[]? Templates = null);
