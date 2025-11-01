using DotNetMcp;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DotNetMcp.Tests;

public class ConcurrencyControlTests
{
    private readonly DotNetCliTools _tools;
    private readonly ConcurrencyManager _concurrencyManager;
    private readonly Mock<ILogger<DotNetCliTools>> _loggerMock;

    public ConcurrencyControlTests()
    {
        _loggerMock = new Mock<ILogger<DotNetCliTools>>();
        _concurrencyManager = new ConcurrencyManager();
        _tools = new DotNetCliTools(_loggerMock.Object, _concurrencyManager);
    }

    [Fact(Skip = "Integration test - requires actual dotnet CLI and valid project")]
    public async Task DotnetProjectBuild_WhenCalledTwiceOnSameProject_ShouldReturnConcurrencyError()
    {
        // Arrange
        var projectPath = "TestProject.csproj";
        
        // Start first build (will likely fail due to missing project, but that's ok)
        var firstBuildTask = _tools.DotnetProjectBuild(project: projectPath, machineReadable: false);

        // Try to start second build immediately
        var secondBuildResult = await _tools.DotnetProjectBuild(project: projectPath, machineReadable: false);

        // Assert - Second build should get concurrency error
        secondBuildResult.Should().Contain("CONCURRENCY_CONFLICT", "second build should be blocked");
        secondBuildResult.Should().Contain("build", "error should mention the operation type");

        // Cleanup - wait for first build to complete
        await firstBuildTask;
    }

    [Fact(Skip = "Integration test - requires actual dotnet CLI")]
    public async Task DotnetProjectBuild_OnDifferentProjects_ShouldAllowParallelExecution()
    {
        // Arrange
        var project1 = "Project1.csproj";
        var project2 = "Project2.csproj";

        // Act - Start builds on different projects
        var task1 = _tools.DotnetProjectBuild(project: project1, machineReadable: false);
        var task2 = _tools.DotnetProjectBuild(project: project2, machineReadable: false);

        // Wait for both
        var results = await Task.WhenAll(task1, task2);

        // Assert - Neither should have concurrency errors (they may fail for other reasons though)
        results[0].Should().NotContain("CONCURRENCY_CONFLICT");
        results[1].Should().NotContain("CONCURRENCY_CONFLICT");
    }

    [Fact(Skip = "Integration test - requires actual dotnet CLI and valid project")]
    public async Task DotnetProjectTest_AfterBuildCompletes_ShouldSucceed()
    {
        // Arrange
        var projectPath = "TestProject.csproj";
        
        // First operation
        await _tools.DotnetProjectBuild(project: projectPath, machineReadable: false);
        
        // Second operation on same target should work after first completes
        var testResult = await _tools.DotnetProjectTest(project: projectPath, machineReadable: false);

        // Assert
        testResult.Should().NotContain("CONCURRENCY_CONFLICT", "test should proceed after build completes");
    }

    [Fact]
    public async Task DotnetProjectBuild_WithMachineReadable_ShouldReturnStructuredConcurrencyError()
    {
        // Arrange
        var projectPath = "TestProject.csproj";
        
        // Manually acquire lock to simulate ongoing operation
        _concurrencyManager.TryAcquireOperation("build", Path.GetFullPath(projectPath), out _);

        // Act
        var result = await _tools.DotnetProjectBuild(project: projectPath, machineReadable: true);

        // Assert
        result.Should().Contain("\"code\": \"CONCURRENCY_CONFLICT\"");
        result.Should().Contain("\"success\": false");
        result.Should().Contain("\"category\": \"Concurrency\"");
        result.Should().Contain("\"exitCode\": -1");

        // Cleanup
        _concurrencyManager.Clear();
    }

    [Fact]
    public async Task DotnetProjectRun_ConcurrentCallsOnSameProject_ShouldBlock()
    {
        // Arrange
        var projectPath = "TestProject.csproj";
        
        // Manually acquire lock to simulate ongoing operation
        _concurrencyManager.TryAcquireOperation("run", Path.GetFullPath(projectPath), out _);

        // Act
        var result = await _tools.DotnetProjectRun(project: projectPath, machineReadable: false);

        // Assert
        result.Should().Contain("Error:");
        result.Should().Contain("conflicting operation");
        result.Should().Contain("run");

        // Cleanup
        _concurrencyManager.Clear();
    }

    [Fact]
    public async Task DotnetProjectPublish_ConcurrentCallsOnSameProject_ShouldBlock()
    {
        // Arrange
        var projectPath = "TestProject.csproj";
        
        // Manually acquire lock
        _concurrencyManager.TryAcquireOperation("publish", Path.GetFullPath(projectPath), out _);

        // Act
        var result = await _tools.DotnetProjectPublish(project: projectPath, machineReadable: false);

        // Assert
        result.Should().Contain("Error:");
        result.Should().Contain("conflicting operation");
        result.Should().Contain("publish");

        // Cleanup
        _concurrencyManager.Clear();
    }

    [Fact]
    public async Task DotnetProjectTest_ConcurrentCallsOnSameProject_ShouldBlock()
    {
        // Arrange
        var projectPath = "TestProject.csproj";
        
        // Manually acquire lock
        _concurrencyManager.TryAcquireOperation("test", Path.GetFullPath(projectPath), out _);

        // Act
        var result = await _tools.DotnetProjectTest(project: projectPath, machineReadable: false);

        // Assert
        result.Should().Contain("Error:");
        result.Should().Contain("conflicting operation");
        result.Should().Contain("test");

        // Cleanup
        _concurrencyManager.Clear();
    }

    [Fact]
    public void ConcurrencyManager_AfterOperationCompletes_ShouldAllowNewOperation()
    {
        // Arrange
        var operationType = "build";
        var target = "/path/to/project.csproj";
        var id1 = Guid.NewGuid().ToString();
        var id2 = Guid.NewGuid().ToString();

        // Act
        var firstAcquire = _concurrencyManager.TryAcquireOperation(operationType, target, out _);
        _concurrencyManager.ReleaseOperation(operationType, target);
        var secondAcquire = _concurrencyManager.TryAcquireOperation(operationType, target, out _);

        // Assert
        firstAcquire.Should().BeTrue();
        secondAcquire.Should().BeTrue();
        
        // Cleanup
        _concurrencyManager.Clear();
    }
}
