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
}
