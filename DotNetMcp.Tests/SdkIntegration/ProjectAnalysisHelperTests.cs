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
}
