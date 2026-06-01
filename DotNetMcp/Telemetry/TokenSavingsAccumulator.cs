using System.Collections.Concurrent;

namespace DotNetMcp;

/// <summary>
/// Thread-safe accumulator for token savings estimates grouped by workflow id.
/// </summary>
public sealed class TokenSavingsAccumulator
{
    private readonly ConcurrentDictionary<string, TokenSavingsWorkflowEstimate> _workflows = new(StringComparer.OrdinalIgnoreCase);

    public void RecordWorkflow(TokenSavingsWorkflowEstimate estimate)
    {
        _workflows[estimate.WorkflowId] = estimate;
    }

    public IReadOnlyDictionary<string, TokenSavingsWorkflowEstimate> GetSnapshot()
        => _workflows.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);

    public void Reset() => _workflows.Clear();
}