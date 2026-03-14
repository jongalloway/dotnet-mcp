using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace DotNetMcp;

/// <summary>
/// Provides workspace discovery helpers using the MCP roots capability.
/// When a client provides workspace roots, these helpers scan them to auto-detect
/// project and solution files so tools can work without explicit path arguments.
/// </summary>
internal static class WorkspaceDiscovery
{
    /// <summary>
    /// Tries to auto-detect a project file from the client's workspace roots.
    /// Returns the file path when exactly one <c>.csproj</c> file is found in any root directory;
    /// returns <see langword="null"/> when the client does not support roots, no roots are
    /// provided, or more than one project is found (to avoid ambiguous auto-detection).
    /// </summary>
    internal static async Task<string?> TryFindProjectInRootsAsync(
        McpServer? server,
        CancellationToken cancellationToken = default)
    {
        if (server == null || server.ClientCapabilities?.Roots == null)
            return null;

        var roots = await RequestRootsSafeAsync(server, cancellationToken);
        if (roots == null)
            return null;

        var candidates = new List<string>();
        foreach (var root in roots)
        {
            var localPath = GetLocalPath(root.Uri);
            if (localPath == null || !Directory.Exists(localPath))
                continue;

            try
            {
                candidates.AddRange(Directory.GetFiles(localPath, "*.csproj", SearchOption.TopDirectoryOnly));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Skip roots that cannot be accessed
            }
        }

        return candidates.Count == 1 ? candidates[0] : null;
    }

    /// <summary>
    /// Tries to auto-detect a solution file from the client's workspace roots.
    /// Returns the file path when exactly one <c>.sln</c> or <c>.slnx</c> file is found in any
    /// root directory; returns <see langword="null"/> when the client does not support roots,
    /// no roots are provided, or more than one solution is found.
    /// </summary>
    internal static async Task<string?> TryFindSolutionInRootsAsync(
        McpServer? server,
        CancellationToken cancellationToken = default)
    {
        if (server == null || server.ClientCapabilities?.Roots == null)
            return null;

        var roots = await RequestRootsSafeAsync(server, cancellationToken);
        if (roots == null)
            return null;

        var candidates = new List<string>();
        foreach (var root in roots)
        {
            var localPath = GetLocalPath(root.Uri);
            if (localPath == null || !Directory.Exists(localPath))
                continue;

            try
            {
                candidates.AddRange(Directory.GetFiles(localPath, "*.sln", SearchOption.TopDirectoryOnly));
                candidates.AddRange(Directory.GetFiles(localPath, "*.slnx", SearchOption.TopDirectoryOnly));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Skip roots that cannot be accessed
            }
        }

        return candidates.Count == 1 ? candidates[0] : null;
    }

    private static async Task<IList<Root>?> RequestRootsSafeAsync(McpServer server, CancellationToken cancellationToken)
    {
        try
        {
            var result = await server.RequestRootsAsync(new ListRootsRequestParams(), cancellationToken);
            return result?.Roots is { Count: > 0 } roots ? roots : null;
        }
        catch
        {
            return null;
        }
    }

    internal static string? GetLocalPath(string? uri)
    {
        if (string.IsNullOrEmpty(uri))
            return null;

        try
        {
            var parsed = new Uri(uri);
            if (parsed.IsFile)
                return parsed.LocalPath;
        }
        catch (UriFormatException)
        {
            // Not a valid URI
        }

        return null;
    }
}
