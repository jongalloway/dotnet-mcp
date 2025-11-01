using DotNetMcp;
using FluentAssertions;
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
        var task = DotNetCommandExecutor.ExecuteCommandAsync(arguments, _loggerMock.Object, machineReadable: false, cts.Token);
        
        // Cancel after a short delay
        await Task.Delay(100);
        cts.Cancel();

        var result = await task;

        // Assert
        result.Should().Contain("cancelled", because: "operation was cancelled");
        result.Should().Contain("Exit Code: -1");
    }

    [Fact(Skip = "Integration test - requires actual dotnet CLI")]
    public async Task ExecuteCommandAsync_WhenCancelledWithMachineReadable_ShouldReturnStructuredError()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var arguments = "run --project NonExistentProject.csproj";

        // Act
        var task = DotNetCommandExecutor.ExecuteCommandAsync(arguments, _loggerMock.Object, machineReadable: true, cts.Token);
        
        // Cancel after a short delay
        await Task.Delay(100);
        cts.Cancel();

        var result = await task;

        // Assert
        result.Should().Contain("OPERATION_CANCELLED");
        result.Should().Contain("\"success\": false");
        result.Should().Contain("\"exitCode\": -1");
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
        var result = await DotNetCommandExecutor.ExecuteCommandAsync(arguments, _loggerMock.Object, machineReadable: false, cts.Token);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().NotContain("cancelled");
    }

    [Fact]
    public async Task ExecuteCommandAsync_WithoutCancellationToken_ShouldStillWork()
    {
        // Arrange
        var arguments = "--version";

        // Act - using default cancellation token
        var result = await DotNetCommandExecutor.ExecuteCommandAsync(arguments, _loggerMock.Object, machineReadable: false);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("Exit Code: 0");
    }
}
