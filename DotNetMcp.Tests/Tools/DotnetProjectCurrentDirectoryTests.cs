using DotNetMcp;
using DotNetMcp.Actions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotNetMcp.Tests.Tools;

/// <summary>
/// Tests for DotnetProject behavior when no project or workingDirectory is provided,
/// specifically testing that the current directory is used for test runner detection.
/// </summary>
/// <remarks>
/// This class is in the ProcessWideStateTests collection because it uses
/// Directory.SetCurrentDirectory, which is process-wide state that can cause
/// race conditions with other parallel tests.
/// </remarks>
[Collection("ProcessWideStateTests")]
public class DotnetProjectCurrentDirectoryTests
{
    private readonly DotNetCliTools _tools;

    public DotnetProjectCurrentDirectoryTests()
    {
        var concurrencyManager = new ConcurrencyManager();
        _tools = new DotNetCliTools(NullLogger<DotNetCliTools>.Instance, concurrencyManager, new ProcessSessionManager());
    }

    [Fact]
    public async Task DotnetProject_Test_NoProjectOrWorkingDir_UsesCurrentDirectory()
    {
        // Arrange: Create temp directory with global.json and change to that directory
        var tempRootDir = Path.Join(Path.GetTempPath(), "dotnet-mcp-current-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRootDir);
        var globalJsonPath = Path.Join(tempRootDir, "global.json");
        var currentDir = Directory.GetCurrentDirectory();

        try
        {
            // Create global.json with MTP configuration
            File.WriteAllText(globalJsonPath, """
            {
                "test": {
                    "runner": "Microsoft.Testing.Platform"
                }
            }
            """);

            // Change to the temp directory so it becomes the current directory
            Directory.SetCurrentDirectory(tempRootDir);

            // Act: Call test without project or workingDirectory
            // Detection should use current directory and find global.json
            var result = await _tools.DotnetProject(
                action: DotnetProjectAction.Test,
                project: null,
                testRunner: TestRunner.Auto,
                workingDirectory: null,
                machineReadable: true);

            // Assert
            Assert.NotNull(result);

            // Verify metadata indicates MTP was detected from global.json
            Assert.Contains("\"selectedTestRunner\": \"microsoft-testing-platform\"", result);
            Assert.Contains("\"selectionSource\": \"global.json\"", result);
        }
        finally
        {
            // Restore original directory
            Directory.SetCurrentDirectory(currentDir);

            try
            {
                Directory.Delete(tempRootDir, recursive: true);
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
