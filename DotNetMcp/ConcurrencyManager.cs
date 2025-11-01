using System.Collections.Concurrent;

namespace DotNetMcp;

/// <summary>
/// Manages concurrency control for long-running and mutating .NET CLI operations.
/// Prevents conflicting operations from running simultaneously on the same target.
/// </summary>
public sealed class ConcurrencyManager
{
    private readonly ConcurrentDictionary<string, ActiveOperation> _activeOperations = new();
    private readonly object _lockObject = new();

    /// <summary>
    /// Attempts to acquire a lock for an operation on a specific target.
    /// Returns true if the operation can proceed, false if there's a conflict.
    /// </summary>
    /// <param name="operationType">Type of operation (e.g., "build", "restore", "test")</param>
    /// <param name="target">Target resource (project path, solution path, or empty for global)</param>
    /// <param name="operationId">Unique identifier for this operation</param>
    /// <param name="conflictingOperation">Details of the conflicting operation if a conflict exists</param>
    /// <returns>True if operation can proceed, false if there's a conflict</returns>
    public bool TryAcquireOperation(string operationType, string target, string operationId, out string? conflictingOperation)
    {
        conflictingOperation = null;
        
        // Normalize the target to handle path variations
        var normalizedTarget = NormalizeTarget(target);
        var key = GetOperationKey(operationType, normalizedTarget);

        lock (_lockObject)
        {
            // Check for conflicting operations
            if (_activeOperations.TryGetValue(key, out var existing))
            {
                conflictingOperation = $"{existing.OperationType} on {existing.Target} (started at {existing.StartTime:yyyy-MM-dd HH:mm:ss})";
                return false;
            }

            // Check for global conflicts (e.g., cache operations, certificate operations)
            if (IsGlobalOperation(operationType))
            {
                // Global operations conflict with all operations of the same type
                foreach (var kvp in _activeOperations)
                {
                    if (kvp.Value.OperationType == operationType)
                    {
                        conflictingOperation = $"{kvp.Value.OperationType} (global operation, started at {kvp.Value.StartTime:yyyy-MM-dd HH:mm:ss})";
                        return false;
                    }
                }
            }

            // Check for mutating operations that conflict
            if (IsMutatingOperation(operationType))
            {
                // Check if there are any other mutating operations on the same target
                var targetKey = normalizedTarget;
                foreach (var kvp in _activeOperations)
                {
                    if (kvp.Value.Target == targetKey && IsMutatingOperation(kvp.Value.OperationType))
                    {
                        conflictingOperation = $"{kvp.Value.OperationType} on {kvp.Value.Target} (started at {kvp.Value.StartTime:yyyy-MM-dd HH:mm:ss})";
                        return false;
                    }
                }
            }

            // No conflicts - register the operation
            _activeOperations[key] = new ActiveOperation
            {
                OperationType = operationType,
                Target = normalizedTarget,
                OperationId = operationId,
                StartTime = DateTime.UtcNow
            };

            return true;
        }
    }

    /// <summary>
    /// Releases a previously acquired operation lock.
    /// </summary>
    /// <param name="operationType">Type of operation</param>
    /// <param name="target">Target resource</param>
    public void ReleaseOperation(string operationType, string target)
    {
        var normalizedTarget = NormalizeTarget(target);
        var key = GetOperationKey(operationType, normalizedTarget);

        lock (_lockObject)
        {
            _activeOperations.TryRemove(key, out _);
        }
    }

    /// <summary>
    /// Gets the count of currently active operations.
    /// </summary>
    public int ActiveOperationCount => _activeOperations.Count;

    /// <summary>
    /// Clears all active operations. Used for testing.
    /// </summary>
    public void Clear()
    {
        lock (_lockObject)
        {
            _activeOperations.Clear();
        }
    }

    private static string GetOperationKey(string operationType, string target)
        => $"{operationType}::{target}";

    private static string NormalizeTarget(string target)
    {
        if (string.IsNullOrEmpty(target))
            return string.Empty;

        // Normalize path separators and make absolute
        try
        {
            return Path.GetFullPath(target).Replace('\\', '/').ToLowerInvariant();
        }
        catch
        {
            // If path normalization fails, use the original target
            return target.Replace('\\', '/').ToLowerInvariant();
        }
    }

    private static bool IsGlobalOperation(string operationType)
    {
        // Operations that affect global state
        return operationType is 
            "template_clear_cache" or 
            "certificate_trust" or 
            "certificate_clean" or
            "tool_install_global" or
            "tool_uninstall_global";
    }

    private static bool IsMutatingOperation(string operationType)
    {
        // Operations that modify files or state
        return operationType is 
            "build" or 
            "restore" or 
            "publish" or 
            "test" or 
            "run" or 
            "watch_run" or 
            "watch_test" or 
            "watch_build" or 
            "package_add" or 
            "package_remove" or 
            "package_update" or 
            "reference_add" or 
            "reference_remove" or 
            "solution_add" or 
            "solution_remove" or
            "project_new" or
            "solution_create" or
            "format";
    }

    private sealed class ActiveOperation
    {
        public required string OperationType { get; init; }
        public required string Target { get; init; }
        public required string OperationId { get; init; }
        public required DateTime StartTime { get; init; }
    }
}
