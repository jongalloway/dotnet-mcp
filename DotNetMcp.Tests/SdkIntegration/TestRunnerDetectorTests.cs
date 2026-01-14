using System.IO;
using DotNetMcp.Actions;
using DotNetMcp.SdkIntegration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotNetMcp.Tests.SdkIntegration;

/// <summary>
/// Tests for TestRunnerDetector functionality.
/// </summary>
public class TestRunnerDetectorTests
{
    [Fact]
    public void DetectTestRunner_NoGlobalJson_DefaultsToVSTest()
    {
        // Arrange: Use a temp directory without global.json
        var tempDir = Path.Join(Path.GetTempPath(), "dotnet-mcp-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            // Act
            var (runner, source) = TestRunnerDetector.DetectTestRunner(workingDirectory: tempDir);

            // Assert
            Assert.Equal(TestRunner.VSTest, runner);
            Assert.Equal("default", source);
        }
        finally
        {
            try
            {
                Directory.Delete(tempDir, recursive: true);
            }
            catch (IOException)
            {
                // Best-effort cleanup
            }
            catch (UnauthorizedAccessException)
            {
                // Best-effort cleanup
            }
        }
    }

    [Fact]
    public void DetectTestRunner_GlobalJsonWithMTP_DetectsMTP()
    {
        // Arrange: Create temp directory with global.json containing MTP configuration
        var tempDir = Path.Join(Path.GetTempPath(), "dotnet-mcp-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var globalJsonPath = Path.Join(tempDir, "global.json");

        try
        {
            // Create global.json with MTP runner
            File.WriteAllText(globalJsonPath, """
            {
                "test": {
                    "runner": "Microsoft.Testing.Platform"
                }
            }
            """);

            // Act
            var (runner, source) = TestRunnerDetector.DetectTestRunner(workingDirectory: tempDir);

            // Assert
            Assert.Equal(TestRunner.MicrosoftTestingPlatform, runner);
            Assert.Equal("global.json", source);
        }
        finally
        {
            try
            {
                Directory.Delete(tempDir, recursive: true);
            }
            catch (IOException)
            {
                // Best-effort cleanup
            }
            catch (UnauthorizedAccessException)
            {
                // Best-effort cleanup
            }
        }
    }

    [Fact]
    public void DetectTestRunner_GlobalJsonWithoutTestSection_DefaultsToVSTest()
    {
        // Arrange: Create temp directory with global.json without test section
        var tempDir = Path.Join(Path.GetTempPath(), "dotnet-mcp-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var globalJsonPath = Path.Join(tempDir, "global.json");

        try
        {
            // Create global.json without test section
            File.WriteAllText(globalJsonPath, """
            {
                "sdk": {
                    "version": "10.0.100"
                }
            }
            """);

            // Act
            var (runner, source) = TestRunnerDetector.DetectTestRunner(workingDirectory: tempDir);

            // Assert
            Assert.Equal(TestRunner.VSTest, runner);
            Assert.Equal("default", source);
        }
        finally
        {
            try
            {
                Directory.Delete(tempDir, recursive: true);
            }
            catch (IOException)
            {
                // Best-effort cleanup
            }
            catch (UnauthorizedAccessException)
            {
                // Best-effort cleanup
            }
        }
    }

    [Fact]
    public void DetectTestRunner_GlobalJsonInParentDirectory_FindsConfig()
    {
        // Arrange: Create nested directory structure with global.json in parent
        var tempDir = Path.Join(Path.GetTempPath(), "dotnet-mcp-test-" + Guid.NewGuid().ToString("N"));
        var subDir = Path.Join(tempDir, "src", "MyProject.Tests");
        Directory.CreateDirectory(subDir);
        var globalJsonPath = Path.Join(tempDir, "global.json");

        try
        {
            // Create global.json in root directory with MTP runner
            File.WriteAllText(globalJsonPath, """
            {
                "test": {
                    "runner": "Microsoft.Testing.Platform"
                }
            }
            """);

            // Act: Search from subdirectory
            var (runner, source) = TestRunnerDetector.DetectTestRunner(workingDirectory: subDir);

            // Assert: Should find parent global.json
            Assert.Equal(TestRunner.MicrosoftTestingPlatform, runner);
            Assert.Equal("global.json", source);
        }
        finally
        {
            try
            {
                Directory.Delete(tempDir, recursive: true);
            }
            catch (IOException)
            {
                // Best-effort cleanup
            }
            catch (UnauthorizedAccessException)
            {
                // Best-effort cleanup
            }
        }
    }

    [Fact]
    public void DetectTestRunner_ProjectPathProvided_UsesProjectDirectory()
    {
        // Arrange: Create directory with global.json
        var tempDir = Path.Join(Path.GetTempPath(), "dotnet-mcp-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var globalJsonPath = Path.Join(tempDir, "global.json");
        var projectPath = Path.Join(tempDir, "MyTests.csproj");

        try
        {
            // Create global.json with MTP runner
            File.WriteAllText(globalJsonPath, """
            {
                "test": {
                    "runner": "Microsoft.Testing.Platform"
                }
            }
            """);

            // Act: Pass project path (file doesn't need to exist for path resolution)
            var (runner, source) = TestRunnerDetector.DetectTestRunner(projectPath: projectPath);

            // Assert: Should use project's directory to find global.json
            Assert.Equal(TestRunner.MicrosoftTestingPlatform, runner);
            Assert.Equal("global.json", source);
        }
        finally
        {
            try
            {
                Directory.Delete(tempDir, recursive: true);
            }
            catch (IOException)
            {
                // Best-effort cleanup
            }
            catch (UnauthorizedAccessException)
            {
                // Best-effort cleanup
            }
        }
    }

    [Fact]
    public void DetectTestRunner_NoSearchDirectory_DefaultsToVSTest()
    {
        // Act: Neither workingDirectory nor projectPath provided
        var (runner, source) = TestRunnerDetector.DetectTestRunner();

        // Assert
        Assert.Equal(TestRunner.VSTest, runner);
        Assert.Equal("default", source);
    }

    [Fact]
    public void DetectTestRunner_InvalidGlobalJson_DefaultsToVSTest()
    {
        // Arrange: Create temp directory with invalid JSON
        var tempDir = Path.Join(Path.GetTempPath(), "dotnet-mcp-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var globalJsonPath = Path.Join(tempDir, "global.json");

        try
        {
            // Create invalid global.json
            File.WriteAllText(globalJsonPath, "{ invalid json }");

            // Act
            var (runner, source) = TestRunnerDetector.DetectTestRunner(workingDirectory: tempDir);

            // Assert: Should default to VSTest on parse error
            Assert.Equal(TestRunner.VSTest, runner);
            Assert.Equal("default", source);
        }
        finally
        {
            try
            {
                Directory.Delete(tempDir, recursive: true);
            }
            catch (IOException)
            {
                // Best-effort cleanup
            }
            catch (UnauthorizedAccessException)
            {
                // Best-effort cleanup
            }
        }
    }
}
