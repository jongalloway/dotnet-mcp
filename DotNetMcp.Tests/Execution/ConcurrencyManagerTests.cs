using DotNetMcp;
using Xunit;

namespace DotNetMcp.Tests;

public class ConcurrencyManagerTests
{
    private readonly ConcurrencyManager _manager;

    public ConcurrencyManagerTests()
    {
        _manager = new ConcurrencyManager();
    }

    [Fact]
    public void TryAcquireOperation_WithNoConflict_ShouldSucceed()
    {
        // Arrange
        var operationType = "build";
        var target = "/path/to/project.csproj";
        var operationId = Guid.NewGuid().ToString();

        // Act
        var result = _manager.TryAcquireOperation(operationType, target, out var conflictingOperation);

        // Assert
        Assert.True(result);
        Assert.Null(conflictingOperation);
        Assert.Equal(1, _manager.ActiveOperationCount);
    }

    [Fact]
    public void TryAcquireOperation_WithSameTargetAndOperation_ShouldFail()
    {
        // Arrange
        var operationType = "build";
        var target = "/path/to/project.csproj";
        var operationId1 = Guid.NewGuid().ToString();
        var operationId2 = Guid.NewGuid().ToString();

        _manager.TryAcquireOperation(operationType, target, out _);

        // Act
        var result = _manager.TryAcquireOperation(operationType, target, out var conflictingOperation);

        // Assert
        Assert.False(result);
        Assert.NotNull(conflictingOperation);
        Assert.Contains("build", conflictingOperation);
        // Check for normalized, lowercased full path
        var normalizedTarget = System.IO.Path.GetFullPath(target).ToLowerInvariant();
        Assert.Contains(normalizedTarget, conflictingOperation);
        Assert.Equal(1, _manager.ActiveOperationCount);
    }

    [Fact]
    public void TryAcquireOperation_WithDifferentTargets_ShouldSucceed()
    {
        // Arrange
        var operationType = "build";
        var target1 = "/path/to/project1.csproj";
        var target2 = "/path/to/project2.csproj";
        var operationId1 = Guid.NewGuid().ToString();
        var operationId2 = Guid.NewGuid().ToString();

        _manager.TryAcquireOperation(operationType, target1, out _);

        // Act
        var result = _manager.TryAcquireOperation(operationType, target2, out var conflictingOperation);

        // Assert
        Assert.True(result);
        Assert.Null(conflictingOperation);
        Assert.Equal(2, _manager.ActiveOperationCount);
    }

    [Fact]
    public void TryAcquireOperation_MutatingOperationsOnSameTarget_ShouldFail()
    {
        // Arrange
        var target = "/path/to/project.csproj";
        var operationId1 = Guid.NewGuid().ToString();
        var operationId2 = Guid.NewGuid().ToString();

        _manager.TryAcquireOperation("build", target, out _);

        // Act - try a different mutating operation on the same target
        var result = _manager.TryAcquireOperation("restore", target, out var conflictingOperation);

        // Assert
        Assert.False(result);
        Assert.NotNull(conflictingOperation);
        Assert.Contains("build", conflictingOperation);
    }

    [Fact]
    public void ReleaseOperation_ShouldAllowSubsequentAcquisition()
    {
        // Arrange
        var operationType = "build";
        var target = "/path/to/project.csproj";
        var operationId1 = Guid.NewGuid().ToString();
        var operationId2 = Guid.NewGuid().ToString();

        _manager.TryAcquireOperation(operationType, target, out _);

        // Act
        _manager.ReleaseOperation(operationType, target);
        var result = _manager.TryAcquireOperation(operationType, target, out var conflictingOperation);

        // Assert
        Assert.True(result);
        Assert.Null(conflictingOperation);
        Assert.Equal(1, _manager.ActiveOperationCount);
    }

    [Fact]
    public void TryAcquireOperation_GlobalOperation_ShouldConflictWithSameType()
    {
        // Arrange
        var operationId1 = Guid.NewGuid().ToString();
        var operationId2 = Guid.NewGuid().ToString();

        _manager.TryAcquireOperation("template_clear_cache", "", out _);

        // Act
        var result = _manager.TryAcquireOperation("template_clear_cache", "", out var conflictingOperation);

        // Assert
        Assert.False(result);
        Assert.NotNull(conflictingOperation);
        Assert.Contains("template_clear_cache", conflictingOperation);
    }

    [Fact]
    public void TryAcquireOperation_NormalizedPaths_ShouldDetectConflicts()
    {
        // Arrange
        var target1 = "/path/to/project.csproj";
        var target2 = "/PATH/TO/PROJECT.CSPROJ"; // Different case
        var operationId1 = Guid.NewGuid().ToString();
        var operationId2 = Guid.NewGuid().ToString();

        _manager.TryAcquireOperation("build", target1, out _);

        // Act - should detect as same target due to normalization
        var result = _manager.TryAcquireOperation("build", target2, out var conflictingOperation);

        // Assert - paths should be normalized to lowercase
        Assert.False(result);
        Assert.NotNull(conflictingOperation);
    }

    [Fact]
    public void Clear_ShouldRemoveAllOperations()
    {
        // Arrange
        _manager.TryAcquireOperation("build", "/path1", out _);
        _manager.TryAcquireOperation("test", "/path2", out _);
        Assert.Equal(2, _manager.ActiveOperationCount);

        // Act
        _manager.Clear();

        // Assert
        Assert.Equal(0, _manager.ActiveOperationCount);
    }

    [Fact]
    public void TryAcquireOperation_EmptyTarget_ShouldWork()
    {
        // Arrange
        var operationType = "build";
        var target = "";
        var operationId = Guid.NewGuid().ToString();

        // Act
        var result = _manager.TryAcquireOperation(operationType, target, out var conflictingOperation);

        // Assert
        Assert.True(result);
        Assert.Null(conflictingOperation);
        Assert.Equal(1, _manager.ActiveOperationCount);
    }

    [Fact]
    public void TryAcquireOperation_MultipleReleasesAndAcquisitions_ShouldWork()
    {
        // Arrange
        var operationType = "build";
        var target = "/path/to/project.csproj";

        // Act & Assert - Acquire, Release, Acquire pattern
        for (int i = 0; i < 5; i++)
        {
            var operationId = Guid.NewGuid().ToString();
            var acquireResult = _manager.TryAcquireOperation(operationType, target, out _);
            Assert.True(acquireResult);
            Assert.Equal(1, _manager.ActiveOperationCount);

            _manager.ReleaseOperation(operationType, target);
            Assert.Equal(0, _manager.ActiveOperationCount);
        }
    }

    [Fact]
    public void TryAcquireOperation_DifferentMutatingOperations_ShouldConflict()
    {
        // Arrange
        var target = "/path/to/project.csproj";
        var operations = new[] { "build", "restore", "publish", "test", "package_add", "format" };

        // Act & Assert - Each operation should conflict with others on same target
        foreach (var firstOp in operations)
        {
            _manager.Clear();
            _manager.TryAcquireOperation(firstOp, target, out _);

            foreach (var secondOp in operations)
            {
                if (firstOp == secondOp)
                    continue;

                var result = _manager.TryAcquireOperation(secondOp, target, out var conflict);
                Assert.False(result);
                Assert.NotNull(conflict);
                Assert.Contains(firstOp, conflict);
            }
        }
    }
}
