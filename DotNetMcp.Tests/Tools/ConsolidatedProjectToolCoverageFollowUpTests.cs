using DotNetMcp;
using DotNetMcp.Actions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotNetMcp.Tests.Tools;

[Collection("ProcessWideStateTests")]
public class ConsolidatedProjectToolCoverageFollowUpTests
{
    private readonly DotNetCliTools _tools;
    private readonly ConcurrencyManager _concurrencyManager;

    public ConsolidatedProjectToolCoverageFollowUpTests()
    {
        _concurrencyManager = new ConcurrencyManager();
        _tools = new DotNetCliTools(NullLogger<DotNetCliTools>.Instance, _concurrencyManager, new ProcessSessionManager());
    }

    [Fact]
    public async Task DotnetProject_Test_WithMultipleParameters_AndExplicitMtp_UsesStrictCommandAssertion()
    {
        var result = (await _tools.DotnetProject(
            action: DotnetProjectAction.Test,
            project: "MyTests.csproj",
            configuration: "Release",
            noBuild: true,
            verbosity: "detailed",
            testRunner: TestRunner.MicrosoftTestingPlatform)).GetText();

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet test --project \"MyTests.csproj\" -c Release --no-build --verbosity detailed");
    }

    [Fact]
    public async Task DotnetProject_Test_WithLegacyFlag_AndConfiguration_UsesStrictCommandAssertion()
    {
        var result = (await _tools.DotnetProject(
            action: DotnetProjectAction.Test,
            project: "MyTests.csproj",
            configuration: "Release",
            useLegacyProjectArgument: true)).GetText();

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet test \"MyTests.csproj\" -c Release");
    }

    [Fact]
    public async Task DotnetProject_Test_WithLegacyFlag_MultipleParameters_UsesStrictCommandAssertion()
    {
        var result = (await _tools.DotnetProject(
            action: DotnetProjectAction.Test,
            project: "MyTests.csproj",
            configuration: "Release",
            filter: "FullyQualifiedName~MyNamespace",
            noBuild: true,
            useLegacyProjectArgument: true)).GetText();

        Assert.NotNull(result);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(result, "dotnet test \"MyTests.csproj\" -c Release --filter \"FullyQualifiedName~MyNamespace\" --no-build");
    }

    [Fact]
    public async Task DotnetProject_Build_WithInvalidConfiguration_ReturnsError()
    {
        var result = (await _tools.DotnetProject(
            action: DotnetProjectAction.Build,
            project: "MyProject.csproj",
            configuration: "InvalidConfig")).GetText();

        Assert.Contains("Error", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("configuration", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetProject_Publish_WithInvalidRuntime_ReturnsError()
    {
        var result = (await _tools.DotnetProject(
            action: DotnetProjectAction.Publish,
            project: "MyProject.csproj",
            runtime: "not-a-runtime")).GetText();

        Assert.Contains("Error", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("runtime", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetProject_Clean_WithInvalidFramework_ReturnsError()
    {
        var result = (await _tools.DotnetProject(
            action: DotnetProjectAction.Clean,
            project: "MyProject.csproj",
            framework: "not-a-framework")).GetText();

        Assert.Contains("Error", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("framework", result, StringComparison.OrdinalIgnoreCase);
    }
}