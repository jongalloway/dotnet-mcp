using System.Text.Json;
using Xunit;

namespace DotNetMcp.Tests.Scenarios;

public class PackageAndReferenceScenarioTests
{
    [ScenarioFact]
    public async Task Scenario_DotnetPackage_AddInvalidPackage_ReturnsMachineReadableError()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var tempRoot = ScenarioHelpers.CreateTempDirectory(nameof(Scenario_DotnetPackage_AddInvalidPackage_ReturnsMachineReadableError));

        // Create a throwaway project via CLI to avoid mutating repo projects.
        var (exitCode, _, stderr) = await ScenarioHelpers.RunDotNetAsync(
            $"new classlib -n TempProj -o \"{tempRoot.Path}\"",
            workingDirectory: tempRoot.Path,
            cancellationToken);

        Assert.True(exitCode == 0, $"dotnet new failed: {stderr}");

        var projectPath = Path.Join(tempRoot.Path, "TempProj.csproj");
        Assert.True(File.Exists(projectPath), $"Expected TempProj.csproj to exist at {projectPath}");

        await using var client = await McpScenarioClient.CreateAsync(cancellationToken);

        var jsonText = await client.CallToolTextAsync(
            toolName: "dotnet_package",
            args: new Dictionary<string, object?>
            {
                ["action"] = "Add",
                ["project"] = projectPath,
                ["packageId"] = "NotAReal.Package.Id.12345",
                ["source"] = "https://api.nuget.org/v3/index.json",
                ["machineReadable"] = true
            },
            cancellationToken);

        using var json = ScenarioHelpers.ParseJson(jsonText);
        ScenarioHelpers.AssertMachineReadableFailure(json.RootElement);

        Assert.True(json.RootElement.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Array);
        Assert.NotEqual(0, errors.GetArrayLength());
    }

    [ScenarioFact]
    public async Task Scenario_ProjectReferenceFlow_AddReferenceAndBuildSolution_Release()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var tempRoot = ScenarioHelpers.CreateTempDirectory(nameof(Scenario_ProjectReferenceFlow_AddReferenceAndBuildSolution_Release));

        var libADir = Path.Join(tempRoot.Path, "LibA");
        var libBDir = Path.Join(tempRoot.Path, "LibB");

        var (aExit, _, aErr) = await ScenarioHelpers.RunDotNetAsync($"new classlib -n LibA -o \"{libADir}\"", tempRoot.Path, cancellationToken);
        Assert.True(aExit == 0, $"dotnet new LibA failed: {aErr}");

        var (bExit, _, bErr) = await ScenarioHelpers.RunDotNetAsync($"new classlib -n LibB -o \"{libBDir}\"", tempRoot.Path, cancellationToken);
        Assert.True(bExit == 0, $"dotnet new LibB failed: {bErr}");

        var libAProj = Path.Join(libADir, "LibA.csproj");
        var libBProj = Path.Join(libBDir, "LibB.csproj");
        Assert.True(File.Exists(libAProj));
        Assert.True(File.Exists(libBProj));

        await using var client = await McpScenarioClient.CreateAsync(cancellationToken);

        // Create solution via MCP.
        var slnCreateText = await client.CallToolTextAsync(
            toolName: "dotnet_solution",
            args: new Dictionary<string, object?>
            {
                ["action"] = "Create",
                ["name"] = "RefDemo",
                ["output"] = tempRoot.Path,
                ["machineReadable"] = true
            },
            cancellationToken);

        using var slnCreateJson = ScenarioHelpers.ParseJson(slnCreateText);
        ScenarioHelpers.AssertMachineReadableSuccess(slnCreateJson.RootElement);

        var slnPath = Path.Join(tempRoot.Path, "RefDemo.sln");
        Assert.True(File.Exists(slnPath), "Expected solution file to be created");

        // Add projects to solution.
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

        // Add project reference LibB -> LibA.
        var addRefText = await client.CallToolTextAsync(
            toolName: "dotnet_package",
            args: new Dictionary<string, object?>
            {
                ["action"] = "AddReference",
                ["project"] = libBProj,
                ["referencePath"] = libAProj,
                ["machineReadable"] = true
            },
            cancellationToken);

        using var addRefJson = ScenarioHelpers.ParseJson(addRefText);
        ScenarioHelpers.AssertMachineReadableSuccess(addRefJson.RootElement);

        // Build solution in Release.
        var buildText = await client.CallToolTextAsync(
            toolName: "dotnet_project",
            args: new Dictionary<string, object?>
            {
                ["action"] = "Build",
                ["project"] = slnPath,
                ["configuration"] = "Release",
                ["machineReadable"] = true
            },
            cancellationToken);

        using var buildJson = ScenarioHelpers.ParseJson(buildText);
        ScenarioHelpers.AssertMachineReadableSuccess(buildJson.RootElement);
    }
}
