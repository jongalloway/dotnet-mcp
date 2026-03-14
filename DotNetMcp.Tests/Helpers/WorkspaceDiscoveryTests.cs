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
    public void GetLocalPath_WithFileUri_ReturnsLocalPath()
    {
        var dir = Path.GetTempPath();
        var uri = new Uri(dir).AbsoluteUri; // produces file:///...

        var result = WorkspaceDiscovery.GetLocalPath(uri);

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
    public void GetLocalPath_WithNullOrEmpty_ReturnsNull(string? uri)
    {
        Assert.Null(WorkspaceDiscovery.GetLocalPath(uri));
    }

    [Fact]
    public void GetLocalPath_WithNonFileUri_ReturnsNull()
    {
        Assert.Null(WorkspaceDiscovery.GetLocalPath("https://example.com/workspace"));
    }

    [Fact]
    public void GetLocalPath_WithInvalidUri_ReturnsNull()
    {
        Assert.Null(WorkspaceDiscovery.GetLocalPath("not a valid uri :::"));
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
