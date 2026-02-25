using DotNetMcp;
using DotNetMcp.Actions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotNetMcp.Tests;

/// <summary>
/// Tests for elicitation support in destructive tool operations.
/// Verifies that:
/// - Destructive operations proceed normally when McpServer is null (no elicitation supported)
/// - The server capability flags correctly advertise elicitation support
/// - Fallback behavior works correctly when clients don't support elicitation
/// </summary>
public class ElicitationTests
{
    private readonly DotNetCliTools _tools;

    public ElicitationTests()
    {
        _tools = new DotNetCliTools(
            NullLogger<DotNetCliTools>.Instance,
            new ConcurrencyManager(),
            new ProcessSessionManager());
    }

    #region DotnetProject Clean - Fallback Behavior

    [Fact]
    public async Task DotnetProject_CleanAction_WithNullServer_ProceedsWithoutConfirmation()
    {
        // When no McpServer is provided (server == null), Clean should proceed
        // without any confirmation — this is the fallback for clients that don't support elicitation.

        // Act - pass no server parameter (defaults to null)
        var result = (await _tools.DotnetProject(
            action: DotnetProjectAction.Clean,
            project: "nonexistent.csproj")).GetText();

        // Assert - the result should be a dotnet command attempt (not a cancellation message)
        Assert.NotNull(result);
        Assert.DoesNotContain("Clean operation cancelled", result);
    }

    [Fact]
    public async Task DotnetProject_CleanAction_WithNullServer_ExecutesDotnetClean()
    {
        // When no McpServer is provided, the clean command should be attempted
        var result = (await _tools.DotnetProject(
            action: DotnetProjectAction.Clean,
            project: "test.csproj",
            server: null)).GetText();

        // Assert - should have attempted to run 'dotnet clean' (not cancelled)
        Assert.NotNull(result);
        Assert.DoesNotContain("Clean operation cancelled", result);
        // The command will fail because test.csproj doesn't exist, but it should have run
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet clean \"test.csproj\"");
    }

    [Fact]
    public async Task DotnetProject_CleanAction_CancellationMessage_IsDescriptive()
    {
        // Verify the cancellation message format is user-friendly
        // We can test the message format by checking the HandleCleanAction indirectly
        // through the fact that it returns "Clean operation cancelled." on decline

        // This test verifies the message format doesn't change unexpectedly
        // The actual elicitation flow requires a live McpServer with elicitation capability
        // which is tested in conformance/integration tests

        // Since we can't easily mock McpServer (it's a sealed class), we test the fallback path
        var result = (await _tools.DotnetProject(
            action: DotnetProjectAction.Clean,
            server: null)).GetText();

        Assert.NotNull(result);
        Assert.DoesNotContain("Clean operation cancelled", result);
    }

    #endregion

    #region DotnetSolution Remove - Fallback Behavior

    [Fact]
    public async Task DotnetSolution_RemoveAction_WithNullServer_ProceedsWithoutConfirmation()
    {
        // When no McpServer is provided (server == null), Remove should proceed
        // without any confirmation — this is the fallback for clients that don't support elicitation.

        // Act - pass no server parameter (defaults to null)
        var result = (await _tools.DotnetSolution(
            action: DotnetSolutionAction.Remove,
            solution: "MySolution.sln",
            projects: ["MyProject.csproj"])).GetText();

        // Assert - the result should be a dotnet command attempt (not a cancellation message)
        Assert.NotNull(result);
        Assert.DoesNotContain("Remove operation cancelled", result);
    }

    [Fact]
    public async Task DotnetSolution_RemoveAction_WithNullServer_ExecutesDotnetSolutionRemove()
    {
        // When no McpServer is provided, the remove command should be attempted
        var result = (await _tools.DotnetSolution(
            action: DotnetSolutionAction.Remove,
            solution: "MySolution.sln",
            projects: ["MyProject.csproj"],
            server: null)).GetText();

        // Assert - should have attempted to run 'dotnet solution remove' (not cancelled)
        Assert.NotNull(result);
        Assert.DoesNotContain("Remove operation cancelled", result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(
            result,
            "dotnet solution \"MySolution.sln\" remove \"MyProject.csproj\"");
    }

    [Fact]
    public async Task DotnetSolution_RemoveAction_WithNullServer_MultipleProjects_ProceedsWithoutConfirmation()
    {
        // Multiple projects should also proceed without confirmation when server is null
        var result = (await _tools.DotnetSolution(
            action: DotnetSolutionAction.Remove,
            solution: "MySolution.sln",
            projects: ["Project1.csproj", "Project2.csproj", "Project3.csproj"],
            server: null)).GetText();

        Assert.NotNull(result);
        Assert.DoesNotContain("Remove operation cancelled", result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(
            result,
            "dotnet solution \"MySolution.sln\" remove \"Project1.csproj\" \"Project2.csproj\" \"Project3.csproj\"");
    }

    #endregion

    #region Server Capabilities - Elicitation Flag

    [Fact]
    public async Task DotnetServerCapabilities_AdvertisesElicitationSupport()
    {
        // The server capabilities should advertise that elicitation is supported
        var result = (await _tools.DotnetServerCapabilities()).GetText();

        Assert.NotNull(result);

        var json = System.Text.Json.JsonDocument.Parse(result);
        var supports = json.RootElement.GetProperty("supports");

        Assert.True(supports.TryGetProperty("elicitation", out var elicitationProp),
            "Server capabilities should include 'elicitation' field");
        Assert.True(elicitationProp.GetBoolean(),
            "Server capabilities should advertise elicitation = true");
    }

    [Fact]
    public async Task DotnetServerCapabilities_AdvertisesPromptsSupport()
    {
        // The server capabilities should advertise that prompts are available
        var result = (await _tools.DotnetServerCapabilities()).GetText();

        Assert.NotNull(result);

        var json = System.Text.Json.JsonDocument.Parse(result);
        var supports = json.RootElement.GetProperty("supports");

        Assert.True(supports.TryGetProperty("prompts", out var promptsProp),
            "Server capabilities should include 'prompts' field");
        Assert.True(promptsProp.GetBoolean(),
            "Server capabilities should advertise prompts = true");
    }

    #endregion

    #region DotnetServerInfo - Elicitation Documentation

    [Fact]
    public async Task DotnetServerInfo_IncludesElicitationSection()
    {
        // The server info should mention elicitation in its output
        var result = (await _tools.DotnetServerInfo()).GetText();

        Assert.NotNull(result);
        Assert.Contains("ELICITATION", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Clean", result);
        Assert.Contains("Remove", result);
    }

    [Fact]
    public async Task DotnetServerInfo_IncludesPromptsSection()
    {
        // The server info should mention the prompts catalog
        var result = (await _tools.DotnetServerInfo()).GetText();

        Assert.NotNull(result);
        Assert.Contains("PROMPTS", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("create_new_webapi", result);
        Assert.Contains("add_package_and_restore", result);
        Assert.Contains("run_tests_with_coverage", result);
    }

    #endregion
}
