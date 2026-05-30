using System.Text.Json;
using DotNetMcp;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotNetMcp.Tests;

/// <summary>
/// Unit tests for <see cref="SolutionAnalysisHelper"/> using minimal on-disk fixtures
/// and an injectable executor to avoid real dotnet CLI invocations.
///
/// These tests validate the parsing and aggregation logic without requiring
/// a real solution file or network access.
/// </summary>
public class SolutionAnalysisHelperTests : IDisposable
{
    private readonly List<string> _tempDirectories = [];

    public void Dispose()
    {
        foreach (var dir in _tempDirectories)
        {
            try { Directory.Delete(dir, recursive: true); }
            catch (IOException) { /* best-effort */ }
            catch (UnauthorizedAccessException) { /* best-effort */ }
        }
    }

    // ---------------------------------------------------------------------------
    // GetProjectPaths parsing (via AnalyzeSolutionAsync with empty project files)
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task AnalyzeSolutionAsync_WhenExecutorReturnsEmptyOutput_ReturnsNoProjectsError()
    {
        Task<string> executor(string _) => Task.FromResult("");

        var result = await SolutionAnalysisHelper.AnalyzeSolutionAsync(
            "/fake/MySolution.sln",
            executor,
            NullLogger<DotNetCliTools>.Instance);

        Assert.Contains("No projects found", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("MySolution.sln", result);
    }

    [Fact]
    public async Task AnalyzeSolutionAsync_WhenExecutorReturnsOnlyHeaderLines_ReturnsNoProjectsError()
    {
        // Typical dotnet sln list header — no project lines
        const string headerOnlyOutput = """
            Project(s)
            ----------
            """;

        Task<string> executor(string _) => Task.FromResult(headerOnlyOutput);

        var result = await SolutionAnalysisHelper.AnalyzeSolutionAsync(
            "/fake/MySolution.sln",
            executor,
            NullLogger<DotNetCliTools>.Instance);

        Assert.Contains("No projects found", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AnalyzeSolutionAsync_WhenExecutorReturnsErrorLine_ReturnsNoProjectsError()
    {
        const string errorOutput = "Error: The specified solution file doesn't exist.";

        Task<string> executor(string _) => Task.FromResult(errorOutput);

        var result = await SolutionAnalysisHelper.AnalyzeSolutionAsync(
            "/fake/Missing.sln",
            executor,
            NullLogger<DotNetCliTools>.Instance);

        Assert.Contains("No projects found", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AnalyzeSolutionAsync_WhenSolutionPathIsEmpty_ReturnsError()
    {
        Task<string> executor(string _) => Task.FromResult("");

        var result = await SolutionAnalysisHelper.AnalyzeSolutionAsync(
            "",
            executor,
            NullLogger<DotNetCliTools>.Instance);

        Assert.Contains("Error", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("solution path", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AnalyzeSolutionAsync_WithSingleMinimalProject_ReturnsAnalysis()
    {
        var (solutionDir, projectFile) = CreateTempSolutionWithProjects(
            ("App", """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net8.0</TargetFramework>
                    <OutputType>Exe</OutputType>
                    <Nullable>enable</Nullable>
                  </PropertyGroup>
                </Project>
                """));

        // Return a relative path as dotnet sln list would
        var relativePath = Path.GetRelativePath(solutionDir, projectFile).Replace('\\', '/');
        var slnListOutput = $"Project(s)\n----------\n{relativePath}";

        Task<string> executor(string _) => Task.FromResult(slnListOutput);

        var fakeSolutionPath = Path.Join(solutionDir, "MySolution.sln");
        var result = await SolutionAnalysisHelper.AnalyzeSolutionAsync(
            fakeSolutionPath,
            executor,
            NullLogger<DotNetCliTools>.Instance);

        Assert.Contains("Solution Analysis: MySolution.sln", result);
        Assert.Contains("Projects: 1", result);
        Assert.Contains("App", result);
        Assert.Contains("Total Projects: 1", result);
    }

    [Fact]
    public async Task AnalyzeSolutionAsync_WithPackageReferences_ListsPackagesInSummary()
    {
        var (solutionDir, projectFile) = CreateTempSolutionWithProjects(
            ("WebApi", """
                <Project Sdk="Microsoft.NET.Sdk.Web">
                  <PropertyGroup>
                    <TargetFramework>net8.0</TargetFramework>
                  </PropertyGroup>
                  <ItemGroup>
                    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
                    <PackageReference Include="Serilog" Version="3.1.1" />
                  </ItemGroup>
                </Project>
                """));

        var relativePath = Path.GetRelativePath(solutionDir, projectFile).Replace('\\', '/');
        var slnListOutput = $"Project(s)\n----------\n{relativePath}";

        Task<string> executor(string _) => Task.FromResult(slnListOutput);

        var fakeSolutionPath = Path.Join(solutionDir, "WebSolution.sln");
        var result = await SolutionAnalysisHelper.AnalyzeSolutionAsync(
            fakeSolutionPath,
            executor,
            NullLogger<DotNetCliTools>.Instance);

        Assert.Contains("WebApi", result);
        Assert.Contains("Packages: 2", result);
    }

    // ---------------------------------------------------------------------------
    // GetSolutionDependenciesAsync
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task GetSolutionDependenciesAsync_WhenNoProjects_ReturnsNoProjectsError()
    {
        Task<string> executor(string _) => Task.FromResult("Project(s)\n----------");

        var result = await SolutionAnalysisHelper.GetSolutionDependenciesAsync(
            "/fake/MySolution.sln",
            executor,
            NullLogger<DotNetCliTools>.Instance);

        Assert.Contains("No projects found", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetSolutionDependenciesAsync_WithProjectHavingNoRefs_ReportsNoProjectRefs()
    {
        var (solutionDir, projectFile) = CreateTempSolutionWithProjects(
            ("Lib", """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net8.0</TargetFramework>
                  </PropertyGroup>
                </Project>
                """));

        var relativePath = Path.GetRelativePath(solutionDir, projectFile).Replace('\\', '/');
        var slnListOutput = $"Project(s)\n----------\n{relativePath}";

        Task<string> executor(string _) => Task.FromResult(slnListOutput);

        var fakeSolutionPath = Path.Join(solutionDir, "MySolution.sln");
        var result = await SolutionAnalysisHelper.GetSolutionDependenciesAsync(
            fakeSolutionPath,
            executor,
            NullLogger<DotNetCliTools>.Instance);

        Assert.Contains("Solution Dependencies: MySolution.sln", result);
        Assert.Contains("no project-to-project references found", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no shared packages found", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetSolutionDependenciesAsync_WithSharedPackage_ReportsSharedPackage()
    {
        var projectXml = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
              </ItemGroup>
            </Project>
            """;

        var (solutionDir, projectFile1, projectFile2) = CreateTempSolutionWithTwoProjects(
            ("ProjectA", projectXml), ("ProjectB", projectXml));

        var rel1 = Path.GetRelativePath(solutionDir, projectFile1).Replace('\\', '/');
        var rel2 = Path.GetRelativePath(solutionDir, projectFile2).Replace('\\', '/');
        var slnListOutput = $"Project(s)\n----------\n{rel1}\n{rel2}";

        Task<string> executor(string _) => Task.FromResult(slnListOutput);

        var fakeSolutionPath = Path.Join(solutionDir, "MySolution.sln");
        var result = await SolutionAnalysisHelper.GetSolutionDependenciesAsync(
            fakeSolutionPath,
            executor,
            NullLogger<DotNetCliTools>.Instance);

        Assert.Contains("Newtonsoft.Json", result);
        Assert.Contains("Shared NuGet Packages", result);
    }

    // ---------------------------------------------------------------------------
    // ValidateSolutionAsync
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task ValidateSolutionAsync_WhenNoProjects_ReturnsNoProjectsError()
    {
        Task<string> executor(string _) => Task.FromResult("Project(s)\n----------");

        var result = await SolutionAnalysisHelper.ValidateSolutionAsync(
            "/fake/MySolution.sln",
            executor,
            NullLogger<DotNetCliTools>.Instance);

        Assert.Contains("No projects found", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidateSolutionAsync_WithHealthyProject_ReportsNoIssues()
    {
        var (solutionDir, projectFile) = CreateTempSolutionWithProjects(
            ("App", """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net8.0</TargetFramework>
                    <OutputType>Exe</OutputType>
                    <Nullable>enable</Nullable>
                  </PropertyGroup>
                </Project>
                """));

        var relativePath = Path.GetRelativePath(solutionDir, projectFile).Replace('\\', '/');
        var slnListOutput = $"Project(s)\n----------\n{relativePath}";

        Task<string> executor(string _) => Task.FromResult(slnListOutput);

        var fakeSolutionPath = Path.Join(solutionDir, "MySolution.sln");
        var result = await SolutionAnalysisHelper.ValidateSolutionAsync(
            fakeSolutionPath,
            executor,
            NullLogger<DotNetCliTools>.Instance);

        Assert.Contains("Solution Validation: MySolution.sln", result);
        Assert.Contains("Projects Analyzed: 1", result);
    }

    [Fact]
    public async Task ValidateSolutionAsync_WithTwoProjectsDifferentFrameworks_ReportsFrameworkMismatch()
    {
        var (solutionDir, projectFile1, projectFile2) = CreateTempSolutionWithTwoProjects(
            ("LibNet8", """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net8.0</TargetFramework>
                    <Nullable>enable</Nullable>
                  </PropertyGroup>
                </Project>
                """),
            ("LibNet9", """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net9.0</TargetFramework>
                    <Nullable>enable</Nullable>
                  </PropertyGroup>
                </Project>
                """));

        var rel1 = Path.GetRelativePath(solutionDir, projectFile1).Replace('\\', '/');
        var rel2 = Path.GetRelativePath(solutionDir, projectFile2).Replace('\\', '/');
        var slnListOutput = $"Project(s)\n----------\n{rel1}\n{rel2}";

        Task<string> executor(string _) => Task.FromResult(slnListOutput);

        var fakeSolutionPath = Path.Join(solutionDir, "MySolution.sln");
        var result = await SolutionAnalysisHelper.ValidateSolutionAsync(
            fakeSolutionPath,
            executor,
            NullLogger<DotNetCliTools>.Instance);

        Assert.Contains("Multiple target frameworks detected", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidateSolutionAsync_WithPackageVersionConflict_ReportsConflict()
    {
        var (solutionDir, projectFile1, projectFile2) = CreateTempSolutionWithTwoProjects(
            ("ProjectA", """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net8.0</TargetFramework>
                    <Nullable>enable</Nullable>
                  </PropertyGroup>
                  <ItemGroup>
                    <PackageReference Include="Newtonsoft.Json" Version="12.0.3" />
                  </ItemGroup>
                </Project>
                """),
            ("ProjectB", """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net8.0</TargetFramework>
                    <Nullable>enable</Nullable>
                  </PropertyGroup>
                  <ItemGroup>
                    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
                  </ItemGroup>
                </Project>
                """));

        var rel1 = Path.GetRelativePath(solutionDir, projectFile1).Replace('\\', '/');
        var rel2 = Path.GetRelativePath(solutionDir, projectFile2).Replace('\\', '/');
        var slnListOutput = $"Project(s)\n----------\n{rel1}\n{rel2}";

        Task<string> executor(string _) => Task.FromResult(slnListOutput);

        var fakeSolutionPath = Path.Join(solutionDir, "MySolution.sln");
        var result = await SolutionAnalysisHelper.ValidateSolutionAsync(
            fakeSolutionPath,
            executor,
            NullLogger<DotNetCliTools>.Instance);

        Assert.Contains("Package version conflicts detected", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Newtonsoft.Json", result);
    }

    // ---------------------------------------------------------------------------
    // dotnet sln list output parsing edge cases
    // ---------------------------------------------------------------------------

    [Theory]
    [InlineData("Project(s)\n----------\nSrc/App.csproj", "Src/App.csproj")]
    [InlineData("Project(s)\n----------\nSrc/App.csproj\nExit Code: 0", "Src/App.csproj")]
    [InlineData("Project(s)\n----------\nApp.fsproj", "App.fsproj")]
    [InlineData("Project(s)\n----------\nApp.vbproj", "App.vbproj")]
    public async Task AnalyzeSolutionAsync_ProjectPathParsing_CorrectlyFiltersProjectLines(
        string slnListOutput, string expectedRelativePath)
    {
        // We only need the executor to return the given output; project analysis will fail
        // for non-existent paths, so we check the error output instead.
        Task<string> executor(string _) => Task.FromResult(slnListOutput);

        var fakeSolutionPath = "/tmp/fake/MySolution.sln";
        var result = await SolutionAnalysisHelper.AnalyzeSolutionAsync(
            fakeSolutionPath,
            executor);

        // The project should be attempted (its name should appear in the output)
        var expectedProjectName = Path.GetFileNameWithoutExtension(expectedRelativePath);
        Assert.Contains(expectedProjectName, result);
    }

    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Creates a temp directory with one project file and returns (solutionDir, projectFilePath).
    /// </summary>
    private (string solutionDir, string projectFile) CreateTempSolutionWithProjects(
        (string name, string xml) project)
    {
        var solutionDir = Path.GetFullPath(Path.Join(Path.GetTempPath(), "dotnet-mcp-sah-" + Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(solutionDir);
        _tempDirectories.Add(solutionDir);

        var projectFile = Path.Join(solutionDir, $"{project.name}.csproj");
        File.WriteAllText(projectFile, project.xml);

        return (solutionDir, projectFile);
    }

    /// <summary>
    /// Creates a temp directory with two project files and returns (solutionDir, projectFile1, projectFile2).
    /// </summary>
    private (string solutionDir, string projectFile1, string projectFile2) CreateTempSolutionWithTwoProjects(
        (string name, string xml) project1, (string name, string xml) project2)
    {
        var solutionDir = Path.GetFullPath(Path.Join(Path.GetTempPath(), "dotnet-mcp-sah-" + Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(solutionDir);
        _tempDirectories.Add(solutionDir);

        var projectFile1 = Path.Join(solutionDir, $"{project1.name}.csproj");
        File.WriteAllText(projectFile1, project1.xml);

        var projectFile2 = Path.Join(solutionDir, $"{project2.name}.csproj");
        File.WriteAllText(projectFile2, project2.xml);

        return (solutionDir, projectFile1, projectFile2);
    }
}
