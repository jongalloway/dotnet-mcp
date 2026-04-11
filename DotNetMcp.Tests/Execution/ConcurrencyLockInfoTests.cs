using System.Text.Json;
using DotNetMcp;
using DotNetMcp.Actions;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Protocol;
using Xunit;

namespace DotNetMcp.Tests.Execution;

/// <summary>
/// Tests verifying that lock metadata (lockScope, lockKey, lockContended, lockWaitedMs)
/// is present in the StructuredContent of machine-readable output for operations that
/// use concurrency control (Build, Run, Test, Publish).
/// </summary>
public class ConcurrencyLockInfoTests
{
    private readonly DotNetCliTools _tools;
    private readonly ConcurrencyManager _concurrencyManager;

    public ConcurrencyLockInfoTests()
    {
        _concurrencyManager = new ConcurrencyManager();
        _tools = new DotNetCliTools(NullLogger<DotNetCliTools>.Instance, _concurrencyManager, new ProcessSessionManager());
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Build action
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DotnetProject_Build_StructuredContent_ContainsLockInfo()
    {
        // Act
        var result = await _tools.DotnetProject(
            action: DotnetProjectAction.Build,
            project: "MyProject.csproj");

        // Assert: structured content must be present and contain lockInfo
        Assert.True(result.StructuredContent.HasValue, "Build should always return structured content");
        var structured = result.StructuredContent!.Value;
        Assert.True(structured.TryGetProperty("lockInfo", out var lockInfoProp), "BuildResult should contain lockInfo");

        // lockScope should be "project" because a .csproj was specified
        Assert.True(lockInfoProp.TryGetProperty("lockScope", out var scopeProp));
        Assert.Equal("project", scopeProp.GetString());

        // lockKey should be non-empty
        Assert.True(lockInfoProp.TryGetProperty("lockKey", out var keyProp));
        Assert.False(string.IsNullOrWhiteSpace(keyProp.GetString()), "lockKey should be non-empty");

        // lockContended should be absent (null) since there was no conflict
        Assert.False(lockInfoProp.TryGetProperty("lockContended", out _), "lockContended should be absent on success");
    }

    [Fact]
    public async Task DotnetProject_Build_WithSolutionFile_LockScopeIsSolution()
    {
        var result = await _tools.DotnetProject(
            action: DotnetProjectAction.Build,
            project: "MySolution.sln");

        Assert.True(result.StructuredContent.HasValue);
        var structured = result.StructuredContent!.Value;
        Assert.True(structured.TryGetProperty("lockInfo", out var lockInfoProp));
        Assert.True(lockInfoProp.TryGetProperty("lockScope", out var scopeProp));
        Assert.Equal("solution", scopeProp.GetString());
    }

    [Fact]
    public async Task DotnetProject_Build_WithWorkingDirectory_LockScopeIsWorkingDirectory()
    {
        var workingDir = Path.Join(Path.GetTempPath(), "dotnet-mcp-lockinfo-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workingDir);
        try
        {
            // No project specified — scope falls back to workingDirectory
            var result = await _tools.DotnetProject(
                action: DotnetProjectAction.Build,
                project: null,
                workingDirectory: workingDir);

            Assert.True(result.StructuredContent.HasValue);
            var structured = result.StructuredContent!.Value;
            Assert.True(structured.TryGetProperty("lockInfo", out var lockInfoProp));
            Assert.True(lockInfoProp.TryGetProperty("lockScope", out var scopeProp));
            Assert.Equal("workingDirectory", scopeProp.GetString());
        }
        finally
        {
            try { Directory.Delete(workingDir, recursive: true); } catch { /* best effort */ }
        }
    }

    [Fact]
    public async Task DotnetProject_Build_LockKey_IsNormalizedLowercasePath()
    {
        var projectPath = "MyProject.csproj";

        var result = await _tools.DotnetProject(
            action: DotnetProjectAction.Build,
            project: projectPath);

        Assert.True(result.StructuredContent.HasValue);
        var lockInfoProp = result.StructuredContent!.Value.GetProperty("lockInfo");
        var lockKey = lockInfoProp.GetProperty("lockKey").GetString()!;

        // lockKey should be all lowercase
        Assert.Equal(lockKey, lockKey.ToLowerInvariant());

        // lockKey should be an absolute path (or contain the project name)
        Assert.Contains("myproject.csproj", lockKey, StringComparison.OrdinalIgnoreCase);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Concurrency conflict — Build
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DotnetProject_Build_WhenConflicted_StructuredContent_HasLockContendedTrue()
    {
        var projectPath = "ConflictedProject.csproj";
        var normalizedPath = Path.GetFullPath(projectPath);

        // Pre-acquire lock to simulate an ongoing operation
        _concurrencyManager.TryAcquireOperation("build", normalizedPath, out _);

        try
        {
            var result = await _tools.DotnetProject(
                action: DotnetProjectAction.Build,
                project: projectPath);

            // Text should contain the conflict error
            var text = result.GetText();
            Assert.Contains("Cannot execute 'build'", text, StringComparison.Ordinal);

            // StructuredContent must have lockInfo with lockContended = true
            Assert.True(result.StructuredContent.HasValue, "Conflict should still produce structured content");
            var lockInfoProp = result.StructuredContent!.Value.GetProperty("lockInfo");

            Assert.True(lockInfoProp.TryGetProperty("lockContended", out var contendedProp),
                "lockContended should be present when conflict occurred");
            Assert.True(contendedProp.GetBoolean(), "lockContended should be true");

            Assert.True(lockInfoProp.TryGetProperty("lockWaitedMs", out var waitedProp),
                "lockWaitedMs should be present when conflict occurred");
            Assert.Equal(0, waitedProp.GetInt64());
        }
        finally
        {
            _concurrencyManager.Clear();
        }
    }

    [Fact]
    public async Task DotnetProject_Build_WhenConflicted_LockScope_IsProject()
    {
        var projectPath = "LockScopeProject.csproj";
        _concurrencyManager.TryAcquireOperation("build", Path.GetFullPath(projectPath), out _);

        try
        {
            var result = await _tools.DotnetProject(
                action: DotnetProjectAction.Build,
                project: projectPath);

            Assert.True(result.StructuredContent.HasValue);
            var lockInfoProp = result.StructuredContent!.Value.GetProperty("lockInfo");
            Assert.Equal("project", lockInfoProp.GetProperty("lockScope").GetString());
        }
        finally
        {
            _concurrencyManager.Clear();
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Test action
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DotnetProject_Test_StructuredContent_ContainsLockInfo()
    {
        var workingDir = Path.Join(Path.GetTempPath(), "dotnet-mcp-test-lockinfo-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workingDir);
        try
        {
            var result = await _tools.DotnetProject(
                action: DotnetProjectAction.Test,
                project: null,
                workingDirectory: workingDir);

            Assert.True(result.StructuredContent.HasValue, "Test action should return structured content with lock info");
            var structured = result.StructuredContent!.Value;
            Assert.True(structured.TryGetProperty("lockInfo", out _), "Test result should contain lockInfo");
        }
        finally
        {
            try { Directory.Delete(workingDir, recursive: true); } catch { /* best effort */ }
        }
    }

    [Fact]
    public async Task DotnetProject_Test_WithProjectFile_LockScopeIsProject()
    {
        var result = await _tools.DotnetProject(
            action: DotnetProjectAction.Test,
            project: "TestProject.csproj");

        Assert.True(result.StructuredContent.HasValue);
        var lockInfoProp = result.StructuredContent!.Value.GetProperty("lockInfo");
        Assert.Equal("project", lockInfoProp.GetProperty("lockScope").GetString());
    }

    [Fact]
    public async Task DotnetProject_Test_WhenConflicted_HasLockContendedTrue()
    {
        var workingDir = Path.Join(Path.GetTempPath(), "dotnet-mcp-test-conflict-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workingDir);

        try
        {
            var normalizedDir = Path.GetFullPath(workingDir);
            _concurrencyManager.TryAcquireOperation("test", normalizedDir, out _);

            var result = await _tools.DotnetProject(
                action: DotnetProjectAction.Test,
                project: null,
                workingDirectory: workingDir);

            // Confirm conflict in text
            var text = result.GetText();
            Assert.Contains("Cannot execute 'test'", text, StringComparison.Ordinal);

            // StructuredContent has lockContended
            Assert.True(result.StructuredContent.HasValue);
            var lockInfoProp = result.StructuredContent!.Value.GetProperty("lockInfo");
            Assert.True(lockInfoProp.TryGetProperty("lockContended", out var contendedProp));
            Assert.True(contendedProp.GetBoolean());
        }
        finally
        {
            _concurrencyManager.Clear();
            try { Directory.Delete(workingDir, recursive: true); } catch { /* best effort */ }
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Publish action
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DotnetProject_Publish_StructuredContent_ContainsLockInfo()
    {
        var result = await _tools.DotnetProject(
            action: DotnetProjectAction.Publish,
            project: "PublishProject.csproj");

        Assert.True(result.StructuredContent.HasValue, "Publish action should return structured content with lock info");
        Assert.True(result.StructuredContent!.Value.TryGetProperty("lockInfo", out _),
            "Publish result should contain lockInfo");
    }

    [Fact]
    public async Task DotnetProject_Publish_LockScope_IsProject()
    {
        var result = await _tools.DotnetProject(
            action: DotnetProjectAction.Publish,
            project: "App.csproj");

        Assert.True(result.StructuredContent.HasValue);
        var lockInfoProp = result.StructuredContent!.Value.GetProperty("lockInfo");
        Assert.Equal("project", lockInfoProp.GetProperty("lockScope").GetString());
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Run action
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DotnetProject_Run_StructuredContent_ContainsLockInfo()
    {
        var result = await _tools.DotnetProject(
            action: DotnetProjectAction.Run,
            project: "RunProject.csproj");

        Assert.True(result.StructuredContent.HasValue, "Run action should return structured content with lock info");
        Assert.True(result.StructuredContent!.Value.TryGetProperty("lockInfo", out _),
            "Run result should contain lockInfo");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // LockScope serialisation
    // ──────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("project.csproj", "project")]
    [InlineData("solution.sln", "solution")]
    [InlineData("solution.slnx", "solution")]
    [InlineData("project.fsproj", "project")]
    [InlineData("project.vbproj", "project")]
    public async Task DotnetProject_Build_LockScope_MatchesFileExtension(string projectArg, string expectedScope)
    {
        var result = await _tools.DotnetProject(
            action: DotnetProjectAction.Build,
            project: projectArg);

        Assert.True(result.StructuredContent.HasValue);
        var lockInfoProp = result.StructuredContent!.Value.GetProperty("lockInfo");
        Assert.Equal(expectedScope, lockInfoProp.GetProperty("lockScope").GetString());
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Non-concurrency-gated actions should NOT expose lock info
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DotnetProject_New_DoesNotContainLockInfo()
    {
        // The New action does not use concurrency control, so no lockInfo expected.
        var result = await _tools.DotnetProject(
            action: DotnetProjectAction.New,
            template: "console",
            name: "TestApp");

        // New returns plain text (no structured content) — just verify no crash and no lockInfo
        if (result.StructuredContent.HasValue)
        {
            Assert.False(result.StructuredContent!.Value.TryGetProperty("lockInfo", out _),
                "New action should not expose lock metadata");
        }
    }
}
