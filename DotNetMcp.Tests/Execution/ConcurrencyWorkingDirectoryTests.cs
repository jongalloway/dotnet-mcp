using DotNetMcp;
using DotNetMcp.Actions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotNetMcp.Tests.Execution;

/// <summary>
/// Tests for concurrency target selection when using workingDirectory parameter.
/// These tests verify the fix for GitHub issue: concurrency target too broad when
/// dotnet_project runs without project parameter but with workingDirectory.
/// </summary>
public class ConcurrencyWorkingDirectoryTests
{
    private readonly DotNetCliTools _tools;
    private readonly ConcurrencyManager _concurrencyManager;

    public ConcurrencyWorkingDirectoryTests()
    {
        _concurrencyManager = new ConcurrencyManager();
        _tools = new DotNetCliTools(NullLogger<DotNetCliTools>.Instance, _concurrencyManager);
    }

    [Fact]
    public async Task DotnetProject_Test_WithDifferentWorkingDirectories_ShouldNotConflict()
    {
        // Arrange: Create two different temporary directories
        var workingDir1 = Path.Join(Path.GetTempPath(), "dotnet-mcp-test-dir1-" + Guid.NewGuid().ToString("N"));
        var workingDir2 = Path.Join(Path.GetTempPath(), "dotnet-mcp-test-dir2-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workingDir1);
        Directory.CreateDirectory(workingDir2);

        try
        {
            // Manually acquire lock for test operation in workingDir1
            var normalizedDir1 = Path.GetFullPath(workingDir1);
            _concurrencyManager.TryAcquireOperation("test", normalizedDir1, out _);

            // Act: Try to run test in workingDir2 (different directory)
            var result = await _tools.DotnetProject(
                action: DotnetProjectAction.Test,
                project: null,
                workingDirectory: workingDir2,
                machineReadable: false);

            // Assert: Should not get concurrency conflict since directories are different
            Assert.NotNull(result);
            Assert.DoesNotContain("CONCURRENCY_CONFLICT", result);
            Assert.DoesNotContain("conflicting operation", result);
            
            // Should get MSB1003 error about missing project instead
            Assert.Contains("MSB1003", result, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            // Cleanup
            _concurrencyManager.Clear();
            try
            {
                Directory.Delete(workingDir1, recursive: true);
                Directory.Delete(workingDir2, recursive: true);
            }
            catch (Exception)
            {
                // Best-effort cleanup - ignore errors
            }
        }
    }

    [Fact]
    public async Task DotnetProject_Test_WithSameWorkingDirectory_ShouldConflict()
    {
        // Arrange: Create one temporary directory
        var workingDir = Path.Join(Path.GetTempPath(), "dotnet-mcp-test-dir-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workingDir);

        try
        {
            // Manually acquire lock for test operation in workingDir
            var normalizedDir = Path.GetFullPath(workingDir);
            _concurrencyManager.TryAcquireOperation("test", normalizedDir, out _);

            // Act: Try to run test in the same directory
            var result = await _tools.DotnetProject(
                action: DotnetProjectAction.Test,
                project: null,
                workingDirectory: workingDir,
                machineReadable: false);

            // Assert: Should get concurrency conflict
            Assert.NotNull(result);
            Assert.Contains("Error:", result);
            Assert.Contains("conflicting operation", result);
            Assert.Contains("test", result);
        }
        finally
        {
            // Cleanup
            _concurrencyManager.Clear();
            try
            {
                Directory.Delete(workingDir, recursive: true);
            }
            catch (Exception)
            {
                // Best-effort cleanup - ignore errors
            }
        }
    }

    [Fact]
    public async Task DotnetProject_Build_WithDifferentWorkingDirectories_ShouldNotConflict()
    {
        // Arrange: Create two different temporary directories
        var workingDir1 = Path.Join(Path.GetTempPath(), "dotnet-mcp-build-dir1-" + Guid.NewGuid().ToString("N"));
        var workingDir2 = Path.Join(Path.GetTempPath(), "dotnet-mcp-build-dir2-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workingDir1);
        Directory.CreateDirectory(workingDir2);

        try
        {
            // Manually acquire lock for build operation in workingDir1
            var normalizedDir1 = Path.GetFullPath(workingDir1);
            _concurrencyManager.TryAcquireOperation("build", normalizedDir1, out _);

            // Act: Try to build in workingDir2 (different directory)
            var result = await _tools.DotnetProject(
                action: DotnetProjectAction.Build,
                project: null,
                workingDirectory: workingDir2,
                machineReadable: false);

            // Assert: Should not get concurrency conflict
            Assert.NotNull(result);
            Assert.DoesNotContain("CONCURRENCY_CONFLICT", result);
            Assert.DoesNotContain("conflicting operation", result);
        }
        finally
        {
            // Cleanup
            _concurrencyManager.Clear();
            try
            {
                Directory.Delete(workingDir1, recursive: true);
                Directory.Delete(workingDir2, recursive: true);
            }
            catch (Exception)
            {
                // Best-effort cleanup - ignore errors
            }
        }
    }

    [Fact]
    public async Task DotnetProject_Run_WithDifferentWorkingDirectories_ShouldNotConflict()
    {
        // Arrange: Create two different temporary directories
        var workingDir1 = Path.Join(Path.GetTempPath(), "dotnet-mcp-run-dir1-" + Guid.NewGuid().ToString("N"));
        var workingDir2 = Path.Join(Path.GetTempPath(), "dotnet-mcp-run-dir2-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workingDir1);
        Directory.CreateDirectory(workingDir2);

        try
        {
            // Manually acquire lock for run operation in workingDir1
            var normalizedDir1 = Path.GetFullPath(workingDir1);
            _concurrencyManager.TryAcquireOperation("run", normalizedDir1, out _);

            // Act: Try to run in workingDir2 (different directory)
            var result = await _tools.DotnetProject(
                action: DotnetProjectAction.Run,
                project: null,
                workingDirectory: workingDir2,
                machineReadable: false);

            // Assert: Should not get concurrency conflict
            Assert.NotNull(result);
            Assert.DoesNotContain("CONCURRENCY_CONFLICT", result);
            Assert.DoesNotContain("conflicting operation", result);
        }
        finally
        {
            // Cleanup
            _concurrencyManager.Clear();
            try
            {
                Directory.Delete(workingDir1, recursive: true);
                Directory.Delete(workingDir2, recursive: true);
            }
            catch (Exception)
            {
                // Best-effort cleanup - ignore errors
            }
        }
    }

    [Fact]
    public async Task DotnetProject_Publish_WithDifferentWorkingDirectories_ShouldNotConflict()
    {
        // Arrange: Create two different temporary directories
        var workingDir1 = Path.Join(Path.GetTempPath(), "dotnet-mcp-publish-dir1-" + Guid.NewGuid().ToString("N"));
        var workingDir2 = Path.Join(Path.GetTempPath(), "dotnet-mcp-publish-dir2-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workingDir1);
        Directory.CreateDirectory(workingDir2);

        try
        {
            // Manually acquire lock for publish operation in workingDir1
            var normalizedDir1 = Path.GetFullPath(workingDir1);
            _concurrencyManager.TryAcquireOperation("publish", normalizedDir1, out _);

            // Act: Try to publish in workingDir2 (different directory)
            var result = await _tools.DotnetProject(
                action: DotnetProjectAction.Publish,
                project: null,
                workingDirectory: workingDir2,
                machineReadable: false);

            // Assert: Should not get concurrency conflict
            Assert.NotNull(result);
            Assert.DoesNotContain("CONCURRENCY_CONFLICT", result);
            Assert.DoesNotContain("conflicting operation", result);
        }
        finally
        {
            // Cleanup
            _concurrencyManager.Clear();
            try
            {
                Directory.Delete(workingDir1, recursive: true);
                Directory.Delete(workingDir2, recursive: true);
            }
            catch (Exception)
            {
                // Best-effort cleanup - ignore errors
            }
        }
    }

    [Fact]
    public async Task DotnetProject_Test_WithWorkingDirectory_MachineReadable_ConflictReturnsStructuredError()
    {
        // Arrange: Create temporary directory
        var workingDir = Path.Join(Path.GetTempPath(), "dotnet-mcp-test-conflict-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workingDir);

        try
        {
            // Manually acquire lock for test operation in workingDir
            var normalizedDir = Path.GetFullPath(workingDir);
            _concurrencyManager.TryAcquireOperation("test", normalizedDir, out _);

            // Act: Try to run test in the same directory with machine-readable output
            var result = await _tools.DotnetProject(
                action: DotnetProjectAction.Test,
                project: null,
                workingDirectory: workingDir,
                machineReadable: true);

            // Assert: Should get structured concurrency conflict error
            Assert.NotNull(result);
            Assert.Contains("\"code\": \"CONCURRENCY_CONFLICT\"", result);
            Assert.Contains("\"success\": false", result);
            Assert.Contains("\"category\": \"Concurrency\"", result);
        }
        finally
        {
            // Cleanup
            _concurrencyManager.Clear();
            try
            {
                Directory.Delete(workingDir, recursive: true);
            }
            catch (Exception)
            {
                // Best-effort cleanup - ignore errors
            }
        }
    }

    [Fact]
    public async Task DotnetProject_Test_WithProjectParameter_IgnoresWorkingDirectory_ForConcurrency()
    {
        // Arrange: Create temporary directories
        var workingDir = Path.Join(Path.GetTempPath(), "dotnet-mcp-working-" + Guid.NewGuid().ToString("N"));
        var projectPath = Path.Join(Path.GetTempPath(), "test-project-" + Guid.NewGuid().ToString("N") + ".csproj");
        Directory.CreateDirectory(workingDir);

        try
        {
            // Manually acquire lock for test operation on different working directory
            var normalizedDir = Path.GetFullPath(workingDir);
            _concurrencyManager.TryAcquireOperation("test", normalizedDir, out _);

            // Act: Run test with explicit project path (should lock on project, not working directory)
            var result = await _tools.DotnetProject(
                action: DotnetProjectAction.Test,
                project: projectPath,
                workingDirectory: workingDir,
                machineReadable: false);

            // Assert: Should not get concurrency conflict because it locks on project path, not working directory
            Assert.NotNull(result);
            Assert.DoesNotContain("conflicting operation", result);
        }
        finally
        {
            // Cleanup
            _concurrencyManager.Clear();
            try
            {
                Directory.Delete(workingDir, recursive: true);
            }
            catch (Exception)
            {
                // Best-effort cleanup - ignore errors
            }
        }
    }

    [Fact]
    public async Task DotnetProject_Test_WithoutProjectOrWorkingDirectory_UsesCurrentDirectory()
    {
        // Arrange: Pre-acquire lock on current directory
        var currentDir = Directory.GetCurrentDirectory();
        _concurrencyManager.TryAcquireOperation("test", currentDir, out _);

        try
        {
            // Act: Run test without project or workingDirectory
            var result = await _tools.DotnetProject(
                action: DotnetProjectAction.Test,
                project: null,
                workingDirectory: null,
                machineReadable: false);

            // Assert: Should get concurrency conflict on current directory
            Assert.NotNull(result);
            Assert.Contains("Error:", result);
            Assert.Contains("conflicting operation", result);
            Assert.Contains("test", result);
        }
        finally
        {
            // Cleanup
            _concurrencyManager.Clear();
        }
    }

    [Fact]
    public async Task DotnetProject_Test_NormalizedPaths_WithWorkingDirectory_DetectsConflict()
    {
        // Arrange: Create temporary directory
        var workingDir = Path.Join(Path.GetTempPath(), "dotnet-mcp-normalized-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workingDir);
        var absolutePath = Path.GetFullPath(workingDir);

        try
        {
            // Acquire lock with normalized absolute path
            _concurrencyManager.TryAcquireOperation("test", absolutePath, out _);

            // Act: Try to run test with same path (GetFullPath should normalize it to same target)
            // The GetOperationTarget method normalizes with Path.GetFullPath
            var result = await _tools.DotnetProject(
                action: DotnetProjectAction.Test,
                project: null,
                workingDirectory: workingDir,  // Same path, should normalize to same target
                machineReadable: false);

            // Assert: Should detect conflict due to path normalization
            Assert.NotNull(result);
            Assert.Contains("conflicting operation", result);
            Assert.Contains("test", result);
        }
        finally
        {
            // Cleanup
            _concurrencyManager.Clear();
            try
            {
                Directory.Delete(workingDir, recursive: true);
            }
            catch (Exception)
            {
                // Best-effort cleanup - ignore errors
            }
        }
    }
}
