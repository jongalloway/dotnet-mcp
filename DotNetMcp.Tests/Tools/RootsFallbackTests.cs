using DotNetMcp;
using DotNetMcp.Actions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotNetMcp.Tests.Tools;

/// <summary>
/// Tests that verify roots-based auto-detection falls back gracefully when:
/// <list type="bullet">
///   <item><description><c>McpServer</c> is <see langword="null"/> (client does not inject it)</description></item>
///   <item><description>No workspace root contains a matching project or solution file</description></item>
/// </list>
/// The tests do not exercise a live <see cref="ModelContextProtocol.Server.McpServer"/> because
/// <c>McpServer</c> is sealed and cannot be mocked; those paths require integration tests.
/// </summary>
[Collection("ProcessWideStateTests")]
public class RootsFallbackTests
{
    private readonly DotNetCliTools _tools;

    public RootsFallbackTests()
    {
        _tools = new DotNetCliTools(
            NullLogger<DotNetCliTools>.Instance,
            new ConcurrencyManager(),
            new ProcessSessionManager());
    }

    // ===== DotnetProject — null server =====

    [Fact]
    public async Task DotnetProject_RestoreAction_WithNullServer_ProceedsWithoutAutoDetection()
    {
        // When server is null the tool should still run (fall through to dotnet restore).
        var result = (await _tools.DotnetProject(
            action: DotnetProjectAction.Restore,
            project: "nonexistent.csproj",
            server: null)).GetText();

        Assert.NotNull(result);
        // Should have attempted the command, not short-circuited
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet restore \"nonexistent.csproj\"");
    }

    [Fact]
    public async Task DotnetProject_BuildAction_WithNullServer_ProceedsWithoutAutoDetection()
    {
        var result = (await _tools.DotnetProject(
            action: DotnetProjectAction.Build,
            project: "nonexistent.csproj",
            server: null)).GetText();

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet build \"nonexistent.csproj\"");
    }

    // ===== DotnetSolution — null server =====

    [Fact]
    public async Task DotnetSolution_ListAction_WithNullServer_RequiresSolutionParameter()
    {
        // Without a server providing roots, solution must be specified explicitly.
        var result = (await _tools.DotnetSolution(
            action: DotnetSolutionAction.List,
            server: null)).GetText();

        // No server → no auto-detection → missing required parameter → error
        Assert.Contains("Error", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("solution", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetSolution_AddAction_WithNullServer_RequiresSolutionParameter()
    {
        var result = (await _tools.DotnetSolution(
            action: DotnetSolutionAction.Add,
            projects: ["MyProject.csproj"],
            server: null)).GetText();

        Assert.Contains("Error", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("solution", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetSolution_ListAction_WithExplicitSolution_IgnoresServer()
    {
        // Even when server is null, an explicit solution path should work normally.
        var result = (await _tools.DotnetSolution(
            action: DotnetSolutionAction.List,
            solution: "MySolution.sln",
            server: null)).GetText();

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet solution \"MySolution.sln\" list");
    }

    // ===== DotnetPackage — null server =====

    [Fact]
    public async Task DotnetPackage_ListAction_WithNullServer_ProceedsWithoutProject()
    {
        // With no server and no project, the command is attempted without a project arg.
        var result = (await _tools.DotnetPackage(
            action: DotnetPackageAction.List,
            server: null)).GetText();

        // Command is attempted; may succeed or fail depending on environment
        Assert.NotNull(result);
    }

    [Fact]
    public async Task DotnetPackage_SearchAction_NeverUsesRoots_WorksWithoutServer()
    {
        // Search does not use a project path at all, so it should work regardless.
        var result = (await _tools.DotnetPackage(
            action: DotnetPackageAction.Search,
            searchTerm: "Newtonsoft.Json",
            server: null)).GetText();

        Assert.NotNull(result);
        // Assert the command was attempted regardless of NuGet connectivity
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet package search Newtonsoft.Json");
    }

    [Fact]
    public async Task DotnetPackage_AddAction_WithExplicitProject_IgnoresServer()
    {
        var result = (await _tools.DotnetPackage(
            action: DotnetPackageAction.Add,
            packageId: "Newtonsoft.Json",
            project: "nonexistent.csproj",
            server: null)).GetText();

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet add \"nonexistent.csproj\" package Newtonsoft.Json");
    }
}
