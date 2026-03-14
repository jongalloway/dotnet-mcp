using System.Collections.Generic;
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

    public ProgressReportingTests()
    {
        _tools = new DotNetCliTools(
            NullLogger<DotNetCliTools>.Instance,
            new ConcurrencyManager(),
            new ProcessSessionManager());
    }

    /// <summary>
    /// Synchronous IProgress&lt;T&gt; implementation for capturing reports in tests.
    /// Unlike the BCL Progress&lt;T&gt;, this calls the callback directly on the reporting thread.
    /// </summary>
    private sealed class CapturingProgress<T> : IProgress<T>
    {
        public List<T> Reports { get; } = new();
        public void Report(T value) => Reports.Add(value);
    }

    // ── DotnetProject ────────────────────────────────────────────────────────

    [Fact]
    public async Task DotnetProject_Build_ReportsProgress()
    {
        var progress = new CapturingProgress<ProgressNotificationValue>();

        var result = (await _tools.DotnetProject(
            action: DotnetProjectAction.Build,
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
            progress: progress);

        // Start message should mention restoring or building
        var startReport = progress.Reports.Find(r => r.Progress == 0);
        Assert.NotNull(startReport);
        Assert.False(string.IsNullOrWhiteSpace(startReport.Message));

        // Complete message should mention build
        var completeReport = progress.Reports.Find(r => r.Progress == 1);
        Assert.NotNull(completeReport);
        Assert.False(string.IsNullOrWhiteSpace(completeReport.Message));
    }
}
