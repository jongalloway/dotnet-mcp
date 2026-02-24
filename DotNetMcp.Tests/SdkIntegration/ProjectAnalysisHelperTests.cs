using System.Text.Json;
using DotNetMcp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotNetMcp.Tests;

public class ProjectAnalysisHelperTests
{
    private readonly ILogger<DotNetCliTools> _logger;

    public ProjectAnalysisHelperTests()
    {
        _logger = NullLogger<DotNetCliTools>.Instance;
    }

    [Fact]
    public async Task AnalyzeProjectAsync_WithNonExistentFile_ReturnsErrorJson()
    {
        // Arrange
        var nonExistentPath = "/tmp/nonexistent.csproj";

        // Act
        var result = await ProjectAnalysisHelper.AnalyzeProjectAsync(nonExistentPath, _logger);

        // Assert
        Assert.NotNull(result);
        var json = JsonDocument.Parse(result);
        Assert.False(json.RootElement.GetProperty("success").GetBoolean());
        Assert.Contains("not found", json.RootElement.GetProperty("error").GetString()!);
    }

    [Fact]
    public async Task AnalyzeDependenciesAsync_WithNonExistentFile_ReturnsErrorJson()
    {
        // Arrange
        var nonExistentPath = "/tmp/nonexistent.csproj";

        // Act
        var result = await ProjectAnalysisHelper.AnalyzeDependenciesAsync(nonExistentPath, _logger);

        // Assert
        Assert.NotNull(result);
        var json = JsonDocument.Parse(result);
        Assert.False(json.RootElement.GetProperty("success").GetBoolean());
        Assert.Contains("not found", json.RootElement.GetProperty("error").GetString()!);
    }

    [Fact]
    public async Task ValidateProjectAsync_WithNonExistentFile_ReturnsErrorJson()
    {
        // Arrange
        var nonExistentPath = "/tmp/nonexistent.csproj";

        // Act
        var result = await ProjectAnalysisHelper.ValidateProjectAsync(nonExistentPath, _logger);

        // Assert
        Assert.NotNull(result);
        var json = JsonDocument.Parse(result);
        Assert.False(json.RootElement.GetProperty("success").GetBoolean());
        Assert.Contains("not found", json.RootElement.GetProperty("error").GetString()!);
    }

    [Fact]
    public async Task AnalyzeProjectAsync_WithValidProject_ReturnsSuccessJson()
    {
        // Arrange - use the test project itself
        var testProjectPath = FindTestProjectPath();
        if (testProjectPath == null)
        {
            // Skip test if we can't find the project file
            return;
        }

        // Act
        var result = await ProjectAnalysisHelper.AnalyzeProjectAsync(testProjectPath, _logger);

        // Assert
        Assert.NotNull(result);
        var json = JsonDocument.Parse(result);
        Assert.True(json.RootElement.GetProperty("success").GetBoolean());
        Assert.True(json.RootElement.TryGetProperty("projectPath", out _));
        Assert.True(json.RootElement.TryGetProperty("targetFrameworks", out _));
        Assert.True(json.RootElement.TryGetProperty("outputType", out _));
        Assert.True(json.RootElement.TryGetProperty("packageReferences", out _));
    }

    [Fact]
    public async Task AnalyzeDependenciesAsync_WithValidProject_ReturnsSuccessJson()
    {
        // Arrange - use the test project itself
        var testProjectPath = FindTestProjectPath();
        if (testProjectPath == null)
        {
            // Skip test if we can't find the project file
            return;
        }

        // Act
        var result = await ProjectAnalysisHelper.AnalyzeDependenciesAsync(testProjectPath, _logger);

        // Assert
        Assert.NotNull(result);
        var json = JsonDocument.Parse(result);
        Assert.True(json.RootElement.GetProperty("success").GetBoolean());
        Assert.True(json.RootElement.TryGetProperty("directPackageDependencies", out _));
        Assert.True(json.RootElement.TryGetProperty("directProjectDependencies", out _));
        Assert.True(json.RootElement.TryGetProperty("totalDirectDependencies", out _));
    }

    [Fact]
    public async Task ValidateProjectAsync_WithValidProject_ReturnsSuccessJson()
    {
        // Arrange - use the test project itself
        var testProjectPath = FindTestProjectPath();
        if (testProjectPath == null)
        {
            // Skip test if we can't find the project file
            return;
        }

        // Act
        var result = await ProjectAnalysisHelper.ValidateProjectAsync(testProjectPath, _logger);

        // Assert
        Assert.NotNull(result);
        var json = JsonDocument.Parse(result);
        Assert.True(json.RootElement.GetProperty("success").GetBoolean());
        Assert.True(json.RootElement.GetProperty("isValid").GetBoolean());
        Assert.True(json.RootElement.TryGetProperty("errors", out _));
        Assert.True(json.RootElement.TryGetProperty("warnings", out _));
        Assert.True(json.RootElement.TryGetProperty("recommendations", out _));
    }

    private string? FindTestProjectPath()
    {
        // Try to find the test project file
        var currentDir = Directory.GetCurrentDirectory();
        
        // Common patterns where the test project might be
        var possiblePaths = new[] 
        {
            Path.Join(currentDir, "DotNetMcp.Tests.csproj"),
            Path.Join(currentDir, "..", "DotNetMcp.Tests", "DotNetMcp.Tests.csproj"),
            Path.Join(currentDir, "..", "..", "DotNetMcp.Tests", "DotNetMcp.Tests.csproj"),
        };

        return possiblePaths
            .Where(File.Exists)
            .Select(Path.GetFullPath)
            .FirstOrDefault();
    }

    private static string CreateTempProject(string content)
    {
        var tempDir = Path.GetFullPath(Path.Join(Path.GetTempPath(), "dotnet-mcp-pah-" + Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(tempDir);
        var projectFile = Path.Join(tempDir, "Test.csproj");
        File.WriteAllText(projectFile, content);
        return projectFile;
    }

    [Fact]
    public async Task SetPropertyAsync_WithNonExistentFile_ReturnsErrorJson()
    {
        var result = await ProjectAnalysisHelper.SetPropertyAsync("/tmp/nonexistent.csproj", "OutputType", "Exe", _logger);

        Assert.NotNull(result);
        var json = JsonDocument.Parse(result);
        Assert.False(json.RootElement.GetProperty("success").GetBoolean());
        Assert.Contains("not found", json.RootElement.GetProperty("error").GetString()!);
    }

    [Fact]
    public async Task SetPropertyAsync_AddsNewProperty()
    {
        var projectFile = CreateTempProject("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);

        try
        {
            var result = await ProjectAnalysisHelper.SetPropertyAsync(projectFile, "OutputType", "Exe", _logger);

            var json = JsonDocument.Parse(result);
            Assert.True(json.RootElement.GetProperty("success").GetBoolean());
            Assert.Equal("OutputType", json.RootElement.GetProperty("propertyName").GetString());
            Assert.Equal("Exe", json.RootElement.GetProperty("propertyValue").GetString());

            // Verify the file was actually modified
            var content = await File.ReadAllTextAsync(projectFile);
            Assert.Contains("OutputType", content);
            Assert.Contains("Exe", content);
        }
        finally
        {
            try { Directory.Delete(Path.GetDirectoryName(projectFile)!, recursive: true); } catch { /* best-effort */ }
        }
    }

    [Fact]
    public async Task SetPropertyAsync_UpdatesExistingProperty()
    {
        var projectFile = CreateTempProject("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <Nullable>disable</Nullable>
              </PropertyGroup>
            </Project>
            """);

        try
        {
            var result = await ProjectAnalysisHelper.SetPropertyAsync(projectFile, "Nullable", "enable", _logger);

            var json = JsonDocument.Parse(result);
            Assert.True(json.RootElement.GetProperty("success").GetBoolean());
            Assert.Equal("Nullable", json.RootElement.GetProperty("propertyName").GetString());
            Assert.Equal("enable", json.RootElement.GetProperty("propertyValue").GetString());
        }
        finally
        {
            try { Directory.Delete(Path.GetDirectoryName(projectFile)!, recursive: true); } catch { /* best-effort */ }
        }
    }

    [Fact]
    public async Task GetPropertyAsync_WithNonExistentFile_ReturnsErrorJson()
    {
        var result = await ProjectAnalysisHelper.GetPropertyAsync("/tmp/nonexistent.csproj", "OutputType", _logger);

        Assert.NotNull(result);
        var json = JsonDocument.Parse(result);
        Assert.False(json.RootElement.GetProperty("success").GetBoolean());
        Assert.Contains("not found", json.RootElement.GetProperty("error").GetString()!);
    }

    [Fact]
    public async Task GetPropertyAsync_ReturnsPropertyValue()
    {
        var projectFile = CreateTempProject("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <Nullable>enable</Nullable>
              </PropertyGroup>
            </Project>
            """);

        try
        {
            var result = await ProjectAnalysisHelper.GetPropertyAsync(projectFile, "Nullable", _logger);

            var json = JsonDocument.Parse(result);
            Assert.True(json.RootElement.GetProperty("success").GetBoolean());
            Assert.Equal("Nullable", json.RootElement.GetProperty("propertyName").GetString());
            Assert.Equal("enable", json.RootElement.GetProperty("propertyValue").GetString());
            Assert.True(json.RootElement.GetProperty("isSet").GetBoolean());
        }
        finally
        {
            try { Directory.Delete(Path.GetDirectoryName(projectFile)!, recursive: true); } catch { /* best-effort */ }
        }
    }

    [Fact]
    public async Task GetPropertyAsync_UnsetProperty_ReturnsEmptyValueAndIsSetFalse()
    {
        var projectFile = CreateTempProject("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);

        try
        {
            var result = await ProjectAnalysisHelper.GetPropertyAsync(projectFile, "OutputType", _logger);

            var json = JsonDocument.Parse(result);
            Assert.True(json.RootElement.GetProperty("success").GetBoolean());
            Assert.Equal("OutputType", json.RootElement.GetProperty("propertyName").GetString());
            Assert.False(json.RootElement.GetProperty("isSet").GetBoolean());
        }
        finally
        {
            try { Directory.Delete(Path.GetDirectoryName(projectFile)!, recursive: true); } catch { /* best-effort */ }
        }
    }

    [Fact]
    public async Task RemovePropertyAsync_WithNonExistentFile_ReturnsErrorJson()
    {
        var result = await ProjectAnalysisHelper.RemovePropertyAsync("/tmp/nonexistent.csproj", "Nullable", _logger);

        Assert.NotNull(result);
        var json = JsonDocument.Parse(result);
        Assert.False(json.RootElement.GetProperty("success").GetBoolean());
        Assert.Contains("not found", json.RootElement.GetProperty("error").GetString()!);
    }

    [Fact]
    public async Task RemovePropertyAsync_RemovesExistingProperty()
    {
        var projectFile = CreateTempProject("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <Nullable>enable</Nullable>
              </PropertyGroup>
            </Project>
            """);

        try
        {
            var result = await ProjectAnalysisHelper.RemovePropertyAsync(projectFile, "Nullable", _logger);

            var json = JsonDocument.Parse(result);
            Assert.True(json.RootElement.GetProperty("success").GetBoolean());
            Assert.Equal("Nullable", json.RootElement.GetProperty("propertyName").GetString());
            Assert.True(json.RootElement.GetProperty("removed").GetBoolean());

            // Verify the property was actually removed from the file
            var content = await File.ReadAllTextAsync(projectFile);
            Assert.DoesNotContain("<Nullable>", content);
        }
        finally
        {
            try { Directory.Delete(Path.GetDirectoryName(projectFile)!, recursive: true); } catch { /* best-effort */ }
        }
    }

    [Fact]
    public async Task RemovePropertyAsync_NonExistentProperty_ReturnsSuccessWithRemovedFalse()
    {
        var projectFile = CreateTempProject("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);

        try
        {
            var result = await ProjectAnalysisHelper.RemovePropertyAsync(projectFile, "OutputType", _logger);

            var json = JsonDocument.Parse(result);
            Assert.True(json.RootElement.GetProperty("success").GetBoolean());
            Assert.Equal("OutputType", json.RootElement.GetProperty("propertyName").GetString());
            Assert.False(json.RootElement.GetProperty("removed").GetBoolean());
        }
        finally
        {
            try { Directory.Delete(Path.GetDirectoryName(projectFile)!, recursive: true); } catch { /* best-effort */ }
        }
    }
}
