using DotNetMcp;
using DotNetMcp.Actions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotNetMcp.Tests.Helpers;

/// <summary>
/// Unit tests for <see cref="WorkspaceDiscovery"/>.
/// Tests the local-path extraction utility and the null-server / no-roots fallback paths.
/// The roots-scanning paths that require a live <see cref="ModelContextProtocol.Server.McpServer"/>
/// are exercised indirectly via the tool-level roots fallback tests in
/// <see cref="DotNetMcp.Tests.Tools.RootsFallbackTests"/>.
/// </summary>
public class WorkspaceDiscoveryTests
{
    // ===== TryGetLocalPath =====

    [Fact]
    public void TryGetLocalPath_WithFileUri_ReturnsLocalPath()
    {
        var dir = Path.GetTempPath();
        var uri = new Uri(dir).AbsoluteUri; // produces file:///...

        var result = WorkspaceDiscovery.TryGetLocalPath(uri);

        Assert.NotNull(result);
        // Normalize separators for cross-platform comparison
        Assert.Equal(
            Path.GetFullPath(dir).TrimEnd(Path.DirectorySeparatorChar),
            Path.GetFullPath(result!).TrimEnd(Path.DirectorySeparatorChar));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryGetLocalPath_WithNullOrEmpty_ReturnsNull(string? uri)
    {
        Assert.Null(WorkspaceDiscovery.TryGetLocalPath(uri));
    }

    [Fact]
    public void TryGetLocalPath_WithNonFileUri_ReturnsNull()
    {
        Assert.Null(WorkspaceDiscovery.TryGetLocalPath("https://example.com/workspace"));
    }

    [Fact]
    public void TryGetLocalPath_WithInvalidUri_ReturnsNull()
    {
        Assert.Null(WorkspaceDiscovery.TryGetLocalPath("not a valid uri :::"));
    }

    // ===== TryFindProjectInRootsAsync — null server fallback =====

    [Fact]
    public async Task TryFindProjectInRootsAsync_WithNullServer_ReturnsNull()
    {
        var result = await WorkspaceDiscovery.TryFindProjectInRootsAsync(
            server: null,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    // ===== TryFindSolutionInRootsAsync — null server fallback =====

    [Fact]
    public async Task TryFindSolutionInRootsAsync_WithNullServer_ReturnsNull()
    {
        var result = await WorkspaceDiscovery.TryFindSolutionInRootsAsync(
            server: null,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Null(result);
    }
}
