using SelfCheckout.HardwareBridge.Transports.Mocks;

namespace SelfCheckout.HardwareBridge.Tests.Transports;

public class MockTransportTests
{
    [Fact]
    public async Task OpenAsync_SetsIsOpenTrue_AndIncrementsCount()
    {
        await using var transport = new MockTransport();

        await transport.OpenAsync();

        Assert.True(transport.IsOpen);
        Assert.Equal(1, transport.OpenCount);
    }

    [Fact]
    public async Task OpenAsync_WhenAlreadyOpen_ThrowsInvalidOperationException()
    {
        await using var transport = new MockTransport();
        await transport.OpenAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => transport.OpenAsync());
    }

    [Fact]
    public async Task WriteAsync_WhenNotOpen_ThrowsInvalidOperationException()
    {
        await using var transport = new MockTransport();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => transport.WriteAsync(new ReadOnlyMemory<byte>(new byte[] { 1, 2, 3 })));
    }

    [Fact]
    public async Task WriteAsync_CapturesBytesInWrittenChunks()
    {
        await using var transport = new MockTransport();
        await transport.OpenAsync();

        await transport.WriteAsync(new ReadOnlyMemory<byte>(new byte[] { 1, 2, 3 }));

        Assert.Single(transport.WrittenChunks);
        Assert.Equal(new byte[] { 1, 2, 3 }, transport.WrittenChunks[0]);
    }

    [Fact]
    public async Task ReadAllAsync_YieldsEnqueuedBytes()
    {
        await using var transport = new MockTransport();
        await transport.OpenAsync();

        await transport.EnqueueReadAsync(new byte[] { 0xAA, 0xBB });
        transport.Complete();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var received = new List<byte[]>();
        await foreach (var chunk in transport.ReadAllAsync(cts.Token))
        {
            received.Add(chunk.ToArray());
        }

        Assert.Single(received);
        Assert.Equal(new byte[] { 0xAA, 0xBB }, received[0]);
    }

    [Fact]
    public async Task CloseAsync_SetsIsOpenFalse_AndIncrementsCount()
    {
        await using var transport = new MockTransport();
        await transport.OpenAsync();

        await transport.CloseAsync();

        Assert.False(transport.IsOpen);
        Assert.Equal(1, transport.CloseCount);
    }

    [Fact]
    public async Task DisposeAsync_TerminatesReader()
    {
        var transport = new MockTransport();
        await transport.OpenAsync();

        await transport.DisposeAsync();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var received = new List<byte[]>();
        await foreach (var chunk in transport.ReadAllAsync(cts.Token))
        {
            received.Add(chunk.ToArray());
        }

        Assert.Empty(received);
    }
}
