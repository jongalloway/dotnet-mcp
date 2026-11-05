using System.Text;

namespace DotNetMcp.Tests;

public sealed class SdkHelperProtocolTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(8192)]
    [InlineData(8193)]
    public async Task ReadMessage_PreservesBytesAtLimit(int length)
    {
        var bytes = Enumerable.Repeat((byte)42, length).ToArray();
        using var stream = new MemoryStream(bytes);
        Assert.Equal(bytes, await SdkHelperProtocol.ReadMessageAsync(stream, length, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ReadMessage_EnforcesByteLimitRatherThanCharacterCount()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("\u00fc\u00fc"));
        await Assert.ThrowsAsync<InvalidDataException>(() =>
            SdkHelperProtocol.ReadMessageAsync(stream, 3, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ReadMessage_PreservesCancellation()
    {
        using var stream = new MemoryStream([1]);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            SdkHelperProtocol.ReadMessageAsync(stream, 1, cancellation.Token));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task WaitForProcessAndStreams_PropagatesEitherReaderFailureBeforeExit(bool failStdout)
    {
        using var stream = new MemoryStream([1, 2]);
        var failedReader = SdkHelperProtocol.ReadMessageAsync(stream, 1, TestContext.Current.CancellationToken);
        var processExit = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var otherReader = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var completion = SdkHelperProtocol.WaitForProcessAndStreamsAsync(
            processExit.Task, failStdout ? failedReader : otherReader.Task, failStdout ? otherReader.Task : failedReader);
        await Assert.ThrowsAsync<InvalidDataException>(() => completion.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        Assert.False(processExit.Task.IsCompleted);
    }

    [Fact]
    public async Task WaitForProcessAndStreams_ObservesFailureAfterAnotherReaderCompletes()
    {
        var processExit = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var stderr = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var completion = SdkHelperProtocol.WaitForProcessAndStreamsAsync(processExit.Task, Task.CompletedTask, stderr.Task);
        var failure = new InvalidDataException("stderr exceeded its limit");
        stderr.SetException(failure);

        var actual = await Assert.ThrowsAsync<InvalidDataException>(
            () => completion.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        Assert.Same(failure, actual);
        Assert.False(processExit.Task.IsCompleted);
    }

    [Fact]
    public async Task WaitForProcessAndStreams_PreservesCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var processExit = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var stderr = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            SdkHelperProtocol.WaitForProcessAndStreamsAsync(
                processExit.Task, Task.FromCanceled(cancellation.Token), stderr.Task));
        Assert.False(processExit.Task.IsCompleted);
    }

    [Fact]
    public async Task WaitForProcessAndStreams_WaitsForAllSuccessfulOperations()
    {
        var processExit = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var stdout = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var stderr = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var completion = SdkHelperProtocol.WaitForProcessAndStreamsAsync(processExit.Task, stdout.Task, stderr.Task);
        stdout.SetResult();
        processExit.SetResult();
        Assert.False(completion.IsCompleted);
        stderr.SetResult();
        await completion.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
    }
}
