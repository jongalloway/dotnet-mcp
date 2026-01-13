using System.Text.Json;
using Xunit;

namespace DotNetMcp.Tests.Scenarios;

/// <summary>
/// End-to-end scenario tests for solution management operations (dotnet_solution tool).
/// These tests validate the complete workflow of creating solutions and managing project membership,
/// ensuring the MCP tool provides full parity with 'dotnet sln' / 'dotnet solution' CLI commands.
/// </summary>
public class SolutionScenarioTests
{
    [ScenarioFact]
    public async Task Scenario_Solution_CreateAddList_VerifiesProjectMembership()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var tempRoot = ScenarioHelpers.CreateTempDirectory(nameof(Scenario_Solution_CreateAddList_VerifiesProjectMembership));

        // Create two throwaway projects via CLI to avoid mutating repo projects.
        var libADir = Path.Join(tempRoot.Path, "LibA");
        var libBDir = Path.Join(tempRoot.Path, "LibB");

        var (aExit, _, aErr) = await ScenarioHelpers.RunDotNetAsync($"new classlib -n LibA -o \"{libADir}\"", tempRoot.Path, cancellationToken);
        Assert.True(aExit == 0, $"dotnet new LibA failed: {aErr}");

        var (bExit, _, bErr) = await ScenarioHelpers.RunDotNetAsync($"new classlib -n LibB -o \"{libBDir}\"", tempRoot.Path, cancellationToken);
        Assert.True(bExit == 0, $"dotnet new LibB failed: {bErr}");

        var libAProj = Path.Join(libADir, "LibA.csproj");
        var libBProj = Path.Join(libBDir, "LibB.csproj");
        Assert.True(File.Exists(libAProj), $"Expected LibA.csproj to exist at {libAProj}");
        Assert.True(File.Exists(libBProj), $"Expected LibB.csproj to exist at {libBProj}");

        await using var client = await McpScenarioClient.CreateAsync(cancellationToken);

        // Step 1: Create solution via MCP
        var slnCreateText = await client.CallToolTextAsync(
            toolName: "dotnet_solution",
            args: new Dictionary<string, object?>
            {
                ["action"] = "Create",
                ["name"] = "TestSolution",
                ["output"] = tempRoot.Path,
                ["machineReadable"] = true
            },
            cancellationToken);

        using var slnCreateJson = ScenarioHelpers.ParseJson(slnCreateText);
        ScenarioHelpers.AssertMachineReadableSuccess(slnCreateJson.RootElement);

        var slnPath = Path.Join(tempRoot.Path, "TestSolution.sln");
        Assert.True(File.Exists(slnPath), "Expected solution file to be created");

        // Step 2: Add projects to solution via MCP
        var slnAddText = await client.CallToolTextAsync(
            toolName: "dotnet_solution",
            args: new Dictionary<string, object?>
            {
                ["action"] = "Add",
                ["solution"] = slnPath,
                ["projects"] = new[] { libAProj, libBProj },
                ["machineReadable"] = true
            },
            cancellationToken);

        using var slnAddJson = ScenarioHelpers.ParseJson(slnAddText);
        ScenarioHelpers.AssertMachineReadableSuccess(slnAddJson.RootElement);

        // Step 3: List projects in solution to verify they were added
        var slnListText = await client.CallToolTextAsync(
            toolName: "dotnet_solution",
            args: new Dictionary<string, object?>
            {
                ["action"] = "List",
                ["solution"] = slnPath,
                ["machineReadable"] = true
            },
            cancellationToken);

        using var slnListJson = ScenarioHelpers.ParseJson(slnListText);
        ScenarioHelpers.AssertMachineReadableSuccess(slnListJson.RootElement);

        // Verify that the output contains references to both projects
        var listOutput = slnListJson.RootElement.GetProperty("output").GetString();
        Assert.NotNull(listOutput);
        Assert.Contains("LibA.csproj", listOutput);
        Assert.Contains("LibB.csproj", listOutput);
    }

    [ScenarioFact]
    public async Task Scenario_Solution_CreateAddListRemoveList_VerifiesProjectRemoval()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var tempRoot = ScenarioHelpers.CreateTempDirectory(nameof(Scenario_Solution_CreateAddListRemoveList_VerifiesProjectRemoval));

        // Create three throwaway projects via CLI
        var libADir = Path.Join(tempRoot.Path, "LibA");
        var libBDir = Path.Join(tempRoot.Path, "LibB");
        var libCDir = Path.Join(tempRoot.Path, "LibC");

        var (aExit, _, aErr) = await ScenarioHelpers.RunDotNetAsync($"new classlib -n LibA -o \"{libADir}\"", tempRoot.Path, cancellationToken);
        Assert.True(aExit == 0, $"dotnet new LibA failed: {aErr}");

        var (bExit, _, bErr) = await ScenarioHelpers.RunDotNetAsync($"new classlib -n LibB -o \"{libBDir}\"", tempRoot.Path, cancellationToken);
        Assert.True(bExit == 0, $"dotnet new LibB failed: {bErr}");

        var (cExit, _, cErr) = await ScenarioHelpers.RunDotNetAsync($"new classlib -n LibC -o \"{libCDir}\"", tempRoot.Path, cancellationToken);
        Assert.True(cExit == 0, $"dotnet new LibC failed: {cErr}");

        var libAProj = Path.Join(libADir, "LibA.csproj");
        var libBProj = Path.Join(libBDir, "LibB.csproj");
        var libCProj = Path.Join(libCDir, "LibC.csproj");
        Assert.True(File.Exists(libAProj), $"Expected LibA.csproj to exist at {libAProj}");
        Assert.True(File.Exists(libBProj), $"Expected LibB.csproj to exist at {libBProj}");
        Assert.True(File.Exists(libCProj), $"Expected LibC.csproj to exist at {libCProj}");

        await using var client = await McpScenarioClient.CreateAsync(cancellationToken);

        // Step 1: Create solution with slnx format
        var slnCreateText = await client.CallToolTextAsync(
            toolName: "dotnet_solution",
            args: new Dictionary<string, object?>
            {
                ["action"] = "Create",
                ["name"] = "TestSolution",
                ["output"] = tempRoot.Path,
                ["format"] = "slnx",
                ["machineReadable"] = true
            },
            cancellationToken);

        using var slnCreateJson = ScenarioHelpers.ParseJson(slnCreateText);
        ScenarioHelpers.AssertMachineReadableSuccess(slnCreateJson.RootElement);

        var slnPath = Path.Join(tempRoot.Path, "TestSolution.slnx");
        Assert.True(File.Exists(slnPath), "Expected solution file (slnx format) to be created");

        // Step 2: Add all three projects to solution
        var slnAddText = await client.CallToolTextAsync(
            toolName: "dotnet_solution",
            args: new Dictionary<string, object?>
            {
                ["action"] = "Add",
                ["solution"] = slnPath,
                ["projects"] = new[] { libAProj, libBProj, libCProj },
                ["machineReadable"] = true
            },
            cancellationToken);

        using var slnAddJson = ScenarioHelpers.ParseJson(slnAddText);
        ScenarioHelpers.AssertMachineReadableSuccess(slnAddJson.RootElement);

        // Step 3: List projects to verify all three were added
        var slnListBeforeText = await client.CallToolTextAsync(
            toolName: "dotnet_solution",
            args: new Dictionary<string, object?>
            {
                ["action"] = "List",
                ["solution"] = slnPath,
                ["machineReadable"] = true
            },
            cancellationToken);

        using var slnListBeforeJson = ScenarioHelpers.ParseJson(slnListBeforeText);
        ScenarioHelpers.AssertMachineReadableSuccess(slnListBeforeJson.RootElement);

        var listBeforeOutput = slnListBeforeJson.RootElement.GetProperty("output").GetString();
        Assert.NotNull(listBeforeOutput);
        Assert.Contains("LibA.csproj", listBeforeOutput);
        Assert.Contains("LibB.csproj", listBeforeOutput);
        Assert.Contains("LibC.csproj", listBeforeOutput);

        // Step 4: Remove LibB from solution
        var slnRemoveText = await client.CallToolTextAsync(
            toolName: "dotnet_solution",
            args: new Dictionary<string, object?>
            {
                ["action"] = "Remove",
                ["solution"] = slnPath,
                ["projects"] = new[] { libBProj },
                ["machineReadable"] = true
            },
            cancellationToken);

        using var slnRemoveJson = ScenarioHelpers.ParseJson(slnRemoveText);
        ScenarioHelpers.AssertMachineReadableSuccess(slnRemoveJson.RootElement);

        // Step 5: List projects again to verify LibB was removed
        var slnListAfterText = await client.CallToolTextAsync(
            toolName: "dotnet_solution",
            args: new Dictionary<string, object?>
            {
                ["action"] = "List",
                ["solution"] = slnPath,
                ["machineReadable"] = true
            },
            cancellationToken);

        using var slnListAfterJson = ScenarioHelpers.ParseJson(slnListAfterText);
        ScenarioHelpers.AssertMachineReadableSuccess(slnListAfterJson.RootElement);

        var listAfterOutput = slnListAfterJson.RootElement.GetProperty("output").GetString();
        Assert.NotNull(listAfterOutput);
        Assert.Contains("LibA.csproj", listAfterOutput);
        Assert.DoesNotContain("LibB.csproj", listAfterOutput);
        Assert.Contains("LibC.csproj", listAfterOutput);
    }
}
