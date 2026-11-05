using Microsoft.Extensions.Logging;

namespace DotNetMcp;

internal static class SdkFeatureSupport
{
    internal static async Task<(int? Major, string? Error)> GetMajorVersionAsync(ILogger? logger = null)
    {
        try
        {
            var text = (await DotNetCommandExecutor.ExecuteCommandForResourceAsync("--version", logger,
                cancellationToken: DotNetCommandExecutor.RequestCancellationToken.Value)).Trim();
            if (Version.TryParse(text.Split('-')[0], out var version))
                return (version.Major, null);

            return (null, $"Error: Could not determine the active SDK version from '{text}'.");
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception or IOException)
        {
            logger?.LogWarning(ex, "Could not determine the active SDK version");
            return (null, $"Error: Could not determine the active SDK version: {ex.Message}");
        }
    }

    internal static async Task<string?> RequireSdk11Async(string feature, ILogger? logger = null)
    {
        var (major, error) = await GetMajorVersionAsync(logger);
        return error ?? (major < 11 ? $"Error: {feature} requires .NET SDK 11 or later in the command's working directory." : null);
    }
}
