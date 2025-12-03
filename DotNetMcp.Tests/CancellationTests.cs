using DotNetMcp;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DotNetMcp.Tests;

public class CancellationTests
{
    private readonly Mock<ILogger> _loggerMock;

    public CancellationTests()
    {
        _loggerMock = new Mock<ILogger>();
    }

    [Fact(Skip = "Integration test - requires actual dotnet CLI")]
    public async Task ExecuteCommandAsync_WhenCancelled_ShouldTerminateProcess()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var arguments = "run --project NonExistentProject.csproj"; // A command that would take time

        // Act
        var task = DotNetCommandExecutor.ExecuteCommandAsync(arguments, _loggerMock.Object, machineReadable: false, unsafeOutput: false, cts.Token);
        
        // Cancel after a short delay
        await Task.Delay(100);
        cts.Cancel();

        var result = await task;

        // Assert - operation was cancelled
        Assert.Contains("cancelled", result);
        Assert.Contains("Exit Code: -1", result);
    }

    [Fact(Skip = "Integration test - requires actual dotnet CLI")]
    public async Task ExecuteCommandAsync_WhenCancelledWithMachineReadable_ShouldReturnStructuredError()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var arguments = "run --project NonExistentProject.csproj";

        // Act
        var task = DotNetCommandExecutor.ExecuteCommandAsync(arguments, _loggerMock.Object, machineReadable: true, unsafeOutput: false, cts.Token);
        
        // Cancel after a short delay
        await Task.Delay(100);
        cts.Cancel();

        var result = await task;

        // Assert
        Assert.Contains("OPERATION_CANCELLED", result);
        Assert.Contains("\"success\": false", result);
        Assert.Contains("\"exitCode\": -1", result);
    }

    [Fact(Skip = "Integration test - requires actual dotnet CLI")]
    public async Task ExecuteCommandForResourceAsync_WhenCancelled_ShouldThrowOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var arguments = "--version"; // Quick command

        // Act
        var task = DotNetCommandExecutor.ExecuteCommandForResourceAsync(arguments, _loggerMock.Object, cts.Token);
        
        // Cancel immediately
        cts.Cancel();

        // Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await task);
    }

    [Fact]
    public async Task ExecuteCommandAsync_WithValidCancellationToken_ShouldAcceptIt()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var arguments = "--version"; // Quick command that should succeed

        // Act - should complete without cancellation
        var result = await DotNetCommandExecutor.ExecuteCommandAsync(arguments, _loggerMock.Object, machineReadable: false, unsafeOutput: false, cts.Token);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.DoesNotContain("cancelled", result);
    }

    [Fact]
    public async Task ExecuteCommandAsync_WithoutCancellationToken_ShouldStillWork()
    {
        // Arrange
        var arguments = "--version";

        // Act - using default cancellation token
        var result = await DotNetCommandExecutor.ExecuteCommandAsync(arguments, _loggerMock.Object, machineReadable: false);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("Exit Code: 0", result);
    }
}
