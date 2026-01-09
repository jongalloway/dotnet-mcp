using DotNetMcp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotNetMcp.Tests;

public class WorkingDirectoryTests
{
    private readonly ILogger _logger;

    public WorkingDirectoryTests()
    {
        _logger = NullLogger.Instance;
    }

    [Fact]
    public async Task ExecuteCommandAsync_WithExistingWorkingDirectory_Succeeds()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "dotnet-mcp-wd-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            // Act
            var result = await DotNetCommandExecutor.ExecuteCommandAsync(
                "--version",
                _logger,
                machineReadable: false,
                unsafeOutput: false,
                cancellationToken: TestContext.Current.CancellationToken,
                workingDirectory: tempDir);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.Contains("Exit Code: 0", result);
        }
        finally
        {
            try
            {
                Directory.Delete(tempDir, recursive: true);
            }
            catch
            {
                // Best-effort cleanup
            }
        }
    }

    [Fact]
    public async Task ExecuteCommandAsync_WithMissingWorkingDirectory_ReturnsValidationErrorPlainText()
    {
        // Arrange
        var missingDir = Path.Combine(Path.GetTempPath(), "dotnet-mcp-missing-" + Guid.NewGuid().ToString("N"));

        // Act
        var result = await DotNetCommandExecutor.ExecuteCommandAsync(
            "--version",
            _logger,
            machineReadable: false,
            unsafeOutput: false,
            cancellationToken: TestContext.Current.CancellationToken,
            workingDirectory: missingDir);

        // Assert
        Assert.Contains("Error:", result);
        Assert.Contains("workingDirectory", result);
        Assert.Contains("Directory not found", result);
    }

    [Fact]
    public async Task ExecuteCommandAsync_WithMissingWorkingDirectory_ReturnsValidationErrorJson()
    {
        // Arrange
        var missingDir = Path.Combine(Path.GetTempPath(), "dotnet-mcp-missing-" + Guid.NewGuid().ToString("N"));

        // Act
        var result = await DotNetCommandExecutor.ExecuteCommandAsync(
            "--version",
            _logger,
            machineReadable: true,
            unsafeOutput: false,
            cancellationToken: TestContext.Current.CancellationToken,
            workingDirectory: missingDir);

        // Assert
        Assert.Contains("\"success\": false", result);
        Assert.Contains("INVALID_PARAMS", result);
        Assert.Contains("workingDirectory", result);
    }
}
