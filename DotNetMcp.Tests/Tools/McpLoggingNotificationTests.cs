using DotNetMcp;
using DotNetMcp.Actions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotNetMcp.Tests;

/// <summary>
/// Tests for MCP logging notification support.
/// Verifies that:
/// - Key tool operations proceed normally when McpServer is null (no notifications sent, no errors)
/// - The server capability flags advertise MCP logging support
/// - The server info text mentions MCP logging
/// </summary>
public class McpLoggingNotificationTests
{
    private readonly DotNetCliTools _tools;

    public McpLoggingNotificationTests()
    {
        _tools = new DotNetCliTools(
            NullLogger<DotNetCliTools>.Instance,
            new ConcurrencyManager(),
            new ProcessSessionManager());
    }

    #region DotnetProject - Null Server Fallback (no exceptions)

    [Fact]
    public async Task DotnetProject_RestoreAction_WithNullServer_ProceedsNormally()
    {
        // Arrange: null server means no MCP logging notifications, but tool should still run
        var result = (await _tools.DotnetProject(
            action: DotnetProjectAction.Restore,
            project: "nonexistent.csproj",
            server: null)).GetText();

        // Assert: tool ran without error from the logging layer; actual dotnet error is expected
        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet restore");
    }

    [Fact]
    public async Task DotnetProject_BuildAction_WithNullServer_ProceedsNormally()
    {
        var result = (await _tools.DotnetProject(
            action: DotnetProjectAction.Build,
            project: "nonexistent.csproj",
            server: null)).GetText();

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet build");
    }

    [Fact]
    public async Task DotnetProject_TestAction_WithNullServer_ProceedsNormally()
    {
        var result = (await _tools.DotnetProject(
            action: DotnetProjectAction.Test,
            project: "nonexistent.csproj",
            server: null)).GetText();

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet test");
    }

    [Fact]
    public async Task DotnetProject_PublishAction_WithNullServer_ProceedsNormally()
    {
        var result = (await _tools.DotnetProject(
            action: DotnetProjectAction.Publish,
            project: "nonexistent.csproj",
            server: null)).GetText();

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet publish");
    }

    #endregion

    #region DotnetPackage - Null Server Fallback (no exceptions)

    [Fact]
    public async Task DotnetPackage_AddAction_WithNullServer_ProceedsNormally()
    {
        var result = (await _tools.DotnetPackage(
            action: DotnetPackageAction.Add,
            packageId: "Newtonsoft.Json",
            project: "nonexistent.csproj",
            server: null)).GetText();

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet add");
    }

    [Fact]
    public async Task DotnetPackage_UpdateAction_WithNullServer_ProceedsNormally()
    {
        var result = (await _tools.DotnetPackage(
            action: DotnetPackageAction.Update,
            packageId: "Newtonsoft.Json",
            project: "nonexistent.csproj",
            server: null)).GetText();

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet add");
    }

    #endregion

    #region Server Capabilities - MCP Logging Flag

    [Fact]
    public async Task DotnetServerCapabilities_AdvertisesMcpLoggingSupport()
    {
        var result = (await _tools.DotnetServerCapabilities()).GetText();

        Assert.NotNull(result);
        using var json = System.Text.Json.JsonDocument.Parse(result);
        var supports = json.RootElement.GetProperty("supports");

        Assert.True(supports.TryGetProperty("mcpLogging", out var mcpLoggingProp),
            "Server capabilities should include 'mcpLogging' field");
        Assert.True(mcpLoggingProp.GetBoolean(),
            "Server capabilities should advertise mcpLogging = true");
    }

    #endregion

    #region Server Info - MCP Logging Documentation

    [Fact]
    public async Task DotnetServerInfo_IncludesMcpLoggingSection()
    {
        var result = (await _tools.DotnetServerInfo()).GetText();

        Assert.NotNull(result);
        Assert.Contains("MCP logging", result, StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}
