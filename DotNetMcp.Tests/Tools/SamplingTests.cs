using DotNetMcp;
using DotNetMcp.Actions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotNetMcp.Tests;

/// <summary>
/// Tests for sampling support in tool error-handling paths.
/// Verifies that:
/// - Build and Test operations proceed normally when McpServer is null (no sampling supported)
/// - The server capability flags correctly advertise sampling support
/// - Fallback behavior works correctly when clients don't support sampling
/// </summary>
public class SamplingTests
{
    private readonly DotNetCliTools _tools;

    public SamplingTests()
    {
        _tools = new DotNetCliTools(
            NullLogger<DotNetCliTools>.Instance,
            new ConcurrencyManager(),
            new ProcessSessionManager());
    }

    #region DotnetProject Build - Fallback Behavior

    [Fact]
    public async Task DotnetProject_BuildAction_WithNullServer_ProceedsWithoutSampling()
    {
        // When no McpServer is provided (server == null), Build should execute without sampling.
        // The command will fail because the project doesn't exist, but no AI analysis should appear.

        var result = (await _tools.DotnetProject(
            action: DotnetProjectAction.Build,
            project: "nonexistent.csproj",
            server: null)).GetText();

        Assert.NotNull(result);
        Assert.DoesNotContain("AI Analysis", result);
    }

    [Fact]
    public async Task DotnetProject_BuildAction_WithNullServer_AttemptsDotnetBuild()
    {
        // Verify the command is attempted even without sampling support

        var result = (await _tools.DotnetProject(
            action: DotnetProjectAction.Build,
            project: "test.csproj",
            server: null)).GetText();

        Assert.NotNull(result);
        // The command should have been run, not silently skipped
        Assert.DoesNotContain("AI Analysis", result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet build \"test.csproj\"");
    }

    #endregion

    #region DotnetProject Test - Fallback Behavior

    [Fact]
    public async Task DotnetProject_TestAction_WithNullServer_ProceedsWithoutSampling()
    {
        // When no McpServer is provided (server == null), Test should execute without sampling.

        var result = (await _tools.DotnetProject(
            action: DotnetProjectAction.Test,
            project: "nonexistent.csproj",
            server: null)).GetText();

        Assert.NotNull(result);
        Assert.DoesNotContain("AI Analysis", result);
    }

    [Fact]
    public async Task DotnetProject_TestAction_WithNullServer_AttemptsDotnetTest()
    {
        // Verify the test command is attempted even without sampling support

        var result = (await _tools.DotnetProject(
            action: DotnetProjectAction.Test,
            project: "test.csproj",
            server: null)).GetText();

        Assert.NotNull(result);
        Assert.DoesNotContain("AI Analysis", result);
        // Should have attempted to run dotnet test
        Assert.Contains("dotnet test", result);
    }

    #endregion

    #region Server Capabilities - Sampling Flag

    [Fact]
    public async Task DotnetServerCapabilities_AdvertisesSamplingSupport()
    {
        // The server capabilities should advertise that sampling is supported

        var result = (await _tools.DotnetServerCapabilities()).GetText();

        Assert.NotNull(result);

        var json = System.Text.Json.JsonDocument.Parse(result);
        var supports = json.RootElement.GetProperty("supports");

        Assert.True(supports.TryGetProperty("sampling", out var samplingProp),
            "Server capabilities should include 'sampling' field");
        Assert.True(samplingProp.GetBoolean(),
            "Server capabilities should advertise sampling = true");
    }

    #endregion

    #region DotnetServerInfo - Sampling Documentation

    [Fact]
    public async Task DotnetServerInfo_IncludesSamplingSection()
    {
        // The server info should mention sampling in its output

        var result = (await _tools.DotnetServerInfo()).GetText();

        Assert.NotNull(result);
        Assert.Contains("SAMPLING", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Build", result);
        Assert.Contains("Test", result);
    }

    #endregion
}
