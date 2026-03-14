using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using DotNetMcp;
using DotNetMcp.Actions;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol;
using Xunit;

namespace DotNetMcp.Tests.Tools;

/// <summary>
/// Tests that verify IProgress&lt;ProgressNotificationValue&gt; support on long-running tools.
/// These tests confirm that the progress parameter is accepted and that at least one
/// progress notification is emitted for long-running actions.
/// </summary>
public class ProgressReportingTests
{
    private readonly DotNetCliTools _tools;

    /// <summary>
    /// A non-existent directory used to make the dotnet executor return an error immediately,
    /// without performing real network or file-system operations. Progress notifications are
    /// still emitted because they are sent before (start) and after (complete) the executor call.
    /// </summary>
    private static readonly string NonExistentDir = "/tmp/dotnet-mcp-nonexistent-progress-test";

    public ProgressReportingTests()
    {
        _tools = new DotNetCliTools(
            NullLogger<DotNetCliTools>.Instance,
            new ConcurrencyManager(),
            new ProcessSessionManager());
    }

    /// <summary>
    /// Synchronous IProgress&lt;T&gt; implementation for capturing reports in tests.
    /// Uses ConcurrentBag for thread-safe report collection.
    /// </summary>
    private sealed class CapturingProgress<T> : IProgress<T>
    {
        private readonly ConcurrentBag<T> _reports = new();
        public IReadOnlyList<T> Reports => _reports.ToArray();
        public void Report(T value) => _reports.Add(value);
    }

    // ── DotnetProject ────────────────────────────────────────────────────────

    [Fact]
    public async Task DotnetProject_Build_ReportsProgress()
    {
        var progress = new CapturingProgress<ProgressNotificationValue>();

        var result = (await _tools.DotnetProject(
            action: DotnetProjectAction.Build,
            workingDirectory: NonExistentDir,
            progress: progress)).GetText();

        Assert.NotNull(result);
        Assert.NotEmpty(progress.Reports);
        Assert.Contains(progress.Reports, r => r.Progress == 0 && r.Total == 1);
    }

    [Fact]
    public async Task DotnetProject_Test_ReportsProgress()
    {
        var progress = new CapturingProgress<ProgressNotificationValue>();

        var result = (await _tools.DotnetProject(
            action: DotnetProjectAction.Test,
            workingDirectory: NonExistentDir,
            progress: progress)).GetText();

        Assert.NotNull(result);
        Assert.NotEmpty(progress.Reports);
        Assert.Contains(progress.Reports, r => r.Progress == 0 && r.Total == 1);
    }

    [Fact]
    public async Task DotnetProject_Publish_ReportsProgress()
    {
        var progress = new CapturingProgress<ProgressNotificationValue>();

        var result = (await _tools.DotnetProject(
            action: DotnetProjectAction.Publish,
            workingDirectory: NonExistentDir,
            progress: progress)).GetText();

        Assert.NotNull(result);
        Assert.NotEmpty(progress.Reports);
        Assert.Contains(progress.Reports, r => r.Progress == 0 && r.Total == 1);
    }

    [Fact]
    public async Task DotnetProject_Clean_ReportsProgress()
    {
        var progress = new CapturingProgress<ProgressNotificationValue>();

        var result = (await _tools.DotnetProject(
            action: DotnetProjectAction.Clean,
            workingDirectory: NonExistentDir,
            progress: progress)).GetText();

        Assert.NotNull(result);
        Assert.NotEmpty(progress.Reports);
        Assert.Contains(progress.Reports, r => r.Progress == 0 && r.Total == 1);
    }

    [Fact]
    public async Task DotnetProject_Restore_ReportsProgress()
    {
        var progress = new CapturingProgress<ProgressNotificationValue>();

        var result = (await _tools.DotnetProject(
            action: DotnetProjectAction.Restore,
            workingDirectory: NonExistentDir,
            progress: progress)).GetText();

        Assert.NotNull(result);
        Assert.NotEmpty(progress.Reports);
        Assert.Contains(progress.Reports, r => r.Progress == 0 && r.Total == 1);
    }

    [Fact]
    public async Task DotnetProject_Pack_ReportsProgress()
    {
        var progress = new CapturingProgress<ProgressNotificationValue>();

        var result = (await _tools.DotnetProject(
            action: DotnetProjectAction.Pack,
            workingDirectory: NonExistentDir,
            progress: progress)).GetText();

        Assert.NotNull(result);
        Assert.NotEmpty(progress.Reports);
        Assert.Contains(progress.Reports, r => r.Progress == 0 && r.Total == 1);
    }

    [Fact]
    public async Task DotnetProject_WithNullProgress_WorksCorrectly()
    {
        var result = (await _tools.DotnetProject(
            action: DotnetProjectAction.Build,
            workingDirectory: NonExistentDir,
            progress: null)).GetText();

        Assert.NotNull(result);
    }

    // ── DotnetPackage ────────────────────────────────────────────────────────

    [Fact]
    public async Task DotnetPackage_Add_ReportsProgress()
    {
        var progress = new CapturingProgress<ProgressNotificationValue>();

        var result = (await _tools.DotnetPackage(
            action: DotnetPackageAction.Add,
            packageId: "Newtonsoft.Json",
            workingDirectory: NonExistentDir,
            progress: progress)).GetText();

        Assert.NotNull(result);
        Assert.NotEmpty(progress.Reports);
        Assert.Contains(progress.Reports, r => r.Progress == 0 && r.Total == 1);
    }

    [Fact]
    public async Task DotnetPackage_Update_ReportsProgress()
    {
        var progress = new CapturingProgress<ProgressNotificationValue>();

        var result = (await _tools.DotnetPackage(
            action: DotnetPackageAction.Update,
            workingDirectory: NonExistentDir,
            progress: progress)).GetText();

        Assert.NotNull(result);
        Assert.NotEmpty(progress.Reports);
        Assert.Contains(progress.Reports, r => r.Progress == 0 && r.Total == 1);
    }

    [Fact]
    public async Task DotnetPackage_WithNullProgress_WorksCorrectly()
    {
        var result = (await _tools.DotnetPackage(
            action: DotnetPackageAction.Add,
            packageId: "Newtonsoft.Json",
            workingDirectory: NonExistentDir,
            progress: null)).GetText();

        Assert.NotNull(result);
    }

    // ── DotnetTool ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DotnetTool_Install_ReportsProgress()
    {
        var progress = new CapturingProgress<ProgressNotificationValue>();

        var result = (await _tools.DotnetTool(
            action: DotnetToolAction.Install,
            packageId: "dotnet-ef",
            workingDirectory: NonExistentDir,
            progress: progress)).GetText();

        Assert.NotNull(result);
        Assert.NotEmpty(progress.Reports);
        Assert.Contains(progress.Reports, r => r.Progress == 0 && r.Total == 1);
    }

    [Fact]
    public async Task DotnetTool_Update_ReportsProgress()
    {
        var progress = new CapturingProgress<ProgressNotificationValue>();

        var result = (await _tools.DotnetTool(
            action: DotnetToolAction.Update,
            workingDirectory: NonExistentDir,
            progress: progress)).GetText();

        Assert.NotNull(result);
        Assert.NotEmpty(progress.Reports);
        Assert.Contains(progress.Reports, r => r.Progress == 0 && r.Total == 1);
    }

    [Fact]
    public async Task DotnetTool_WithNullProgress_WorksCorrectly()
    {
        var result = (await _tools.DotnetTool(
            action: DotnetToolAction.List,
            progress: null)).GetText();

        Assert.NotNull(result);
    }

    // ── DotnetWorkload ───────────────────────────────────────────────────────

    [Fact]
    public async Task DotnetWorkload_Install_ReportsProgress()
    {
        var progress = new CapturingProgress<ProgressNotificationValue>();

        var result = (await _tools.DotnetWorkload(
            action: DotnetWorkloadAction.Install,
            workloadIds: ["maui"],
            workingDirectory: NonExistentDir,
            progress: progress)).GetText();

        Assert.NotNull(result);
        Assert.NotEmpty(progress.Reports);
        Assert.Contains(progress.Reports, r => r.Progress == 0 && r.Total == 1);
    }

    [Fact]
    public async Task DotnetWorkload_Update_ReportsProgress()
    {
        var progress = new CapturingProgress<ProgressNotificationValue>();

        var result = (await _tools.DotnetWorkload(
            action: DotnetWorkloadAction.Update,
            workingDirectory: NonExistentDir,
            progress: progress)).GetText();

        Assert.NotNull(result);
        Assert.NotEmpty(progress.Reports);
        Assert.Contains(progress.Reports, r => r.Progress == 0 && r.Total == 1);
    }

    [Fact]
    public async Task DotnetWorkload_WithNullProgress_WorksCorrectly()
    {
        var result = (await _tools.DotnetWorkload(
            action: DotnetWorkloadAction.List,
            progress: null)).GetText();

        Assert.NotNull(result);
    }

    // ── Progress message content ──────────────────────────────────────────────

    [Fact]
    public async Task DotnetProject_Build_ProgressMessages_AreMeaningful()
    {
        var progress = new CapturingProgress<ProgressNotificationValue>();

        await _tools.DotnetProject(
            action: DotnetProjectAction.Build,
            workingDirectory: NonExistentDir,
            progress: progress);

        // Start message should mention building
        var startReport = progress.Reports.FirstOrDefault(r => r.Progress == 0);
        Assert.NotNull(startReport);
        Assert.False(string.IsNullOrWhiteSpace(startReport.Message));

        // Complete message should be non-empty
        var completeReport = progress.Reports.FirstOrDefault(r => r.Progress == 1);
        Assert.NotNull(completeReport);
        Assert.False(string.IsNullOrWhiteSpace(completeReport.Message));
    }
}
