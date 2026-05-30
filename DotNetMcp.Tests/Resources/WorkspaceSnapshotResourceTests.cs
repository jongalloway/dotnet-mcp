using System.Text.Json;
using DotNetMcp;
using Xunit;

namespace DotNetMcp.Tests;

public class WorkspaceSnapshotResourceTests
{
    [Fact]
    public void BuildWorkspaceSnapshot_UsesSolutionAndExtractsProjectMetadata()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var appProject = Path.Join(tempDir, "src", "MyApp", "MyApp.csproj");
            var testProject = Path.Join(tempDir, "tests", "MyApp.Tests", "MyApp.Tests.csproj");
            Directory.CreateDirectory(Path.GetDirectoryName(appProject)!);
            Directory.CreateDirectory(Path.GetDirectoryName(testProject)!);

            File.WriteAllText(appProject, """
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Serilog" Version="3.0.0" />
    <PackageReference Include="Polly" Version="8.0.0" />
  </ItemGroup>
</Project>
""");

            File.WriteAllText(testProject, """
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net10.0;net9.0</TargetFrameworks>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="xunit" Version="2.8.0" />
  </ItemGroup>
</Project>
""");

            var solutionPath = Path.Join(tempDir, "MyApp.sln");
            File.WriteAllText(solutionPath, """
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "MyApp", "src/MyApp/MyApp.csproj", "{11111111-1111-1111-1111-111111111111}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "MyApp.Tests", "tests/MyApp.Tests/MyApp.Tests.csproj", "{22222222-2222-2222-2222-222222222222}"
EndProject
Global
EndGlobal
""");

            var snapshot = DotNetResources.BuildWorkspaceSnapshot(tempDir);

            Assert.Equal("MyApp.sln", snapshot.Solution);
            Assert.Equal(2, snapshot.Projects.Count);

            var app = snapshot.Projects.Single(p => p.Name == "MyApp");
            Assert.Equal("src/MyApp/MyApp.csproj", app.Path);
            Assert.Contains("net10.0", app.TargetFrameworks);
            Assert.Equal(2, app.PackageCount);
            Assert.False(app.IsTestProject);

            var tests = snapshot.Projects.Single(p => p.Name == "MyApp.Tests");
            Assert.Equal("tests/MyApp.Tests/MyApp.Tests.csproj", tests.Path);
            Assert.Contains("net10.0", tests.TargetFrameworks);
            Assert.Contains("net9.0", tests.TargetFrameworks);
            Assert.Equal(1, tests.PackageCount);
            Assert.True(tests.IsTestProject);

            Assert.True(DateTimeOffset.TryParse(snapshot.GeneratedAt, out _));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void BuildWorkspaceSnapshot_WithoutSolution_FallsBackToRecursiveProjectDiscovery()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var projectPath = Path.Join(tempDir, "src", "OnlyProject", "OnlyProject.csproj");
            Directory.CreateDirectory(Path.GetDirectoryName(projectPath)!);
            File.WriteAllText(projectPath, """
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
</Project>
""");

            var ignoredObjProject = Path.Join(tempDir, "obj", "Generated", "Generated.csproj");
            Directory.CreateDirectory(Path.GetDirectoryName(ignoredObjProject)!);
            File.WriteAllText(ignoredObjProject, "<Project Sdk=\"Microsoft.NET.Sdk\" />");

            var snapshot = DotNetResources.BuildWorkspaceSnapshot(tempDir);

            Assert.Null(snapshot.Solution);
            Assert.Single(snapshot.Projects);
            Assert.Equal("OnlyProject", snapshot.Projects[0].Name);
            Assert.Equal("src/OnlyProject/OnlyProject.csproj", snapshot.Projects[0].Path);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task GetWorkspaceSnapshot_ReturnsCachedEnvelopeWithWorkspaceData()
    {
        var tempDir = CreateTempDirectory();
        var originalCurrentDirectory = Directory.GetCurrentDirectory();

        try
        {
            var projectPath = Path.Join(tempDir, "App", "App.csproj");
            Directory.CreateDirectory(Path.GetDirectoryName(projectPath)!);
            File.WriteAllText(projectPath, """
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
</Project>
""");

            Directory.SetCurrentDirectory(tempDir);
            var resources = new DotNetResources(Microsoft.Extensions.Logging.Abstractions.NullLogger<DotNetResources>.Instance);
            var json = await resources.GetWorkspaceSnapshot();

            using var doc = JsonDocument.Parse(json);
            Assert.True(doc.RootElement.TryGetProperty("data", out var data));
            Assert.True(data.TryGetProperty("projects", out var projects));
            Assert.True(projects.GetArrayLength() >= 1);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCurrentDirectory);
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void BuildWorkspaceSnapshot_WithAbsoluteSolutionProjectPath_IncludesProject()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var projectPath = Path.Join(tempDir, "src", "AbsolutePathProject", "AbsolutePathProject.csproj");
            Directory.CreateDirectory(Path.GetDirectoryName(projectPath)!);
            File.WriteAllText(projectPath, """
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
</Project>
""");

            var solutionPath = Path.Join(tempDir, "AbsolutePathProject.sln");
            var slnProjectPath = projectPath.Replace('\\', '/');
            File.WriteAllText(solutionPath, $$"""
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "AbsolutePathProject", "{{slnProjectPath}}", "{33333333-3333-3333-3333-333333333333}"
EndProject
Global
EndGlobal
""");

            var snapshot = DotNetResources.BuildWorkspaceSnapshot(tempDir);

            Assert.Single(snapshot.Projects);
            Assert.Equal("src/AbsolutePathProject/AbsolutePathProject.csproj", snapshot.Projects[0].Path);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        var tempDir = Path.Join(Path.GetTempPath(), "dotnet-mcp-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        return tempDir;
    }
}
