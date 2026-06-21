using System.Threading.Channels;
using SelfCheckout.HardwareBridge.Abstractions.Interfaces;

namespace SelfCheckout.HardwareBridge.Transports.Mocks;

/// <summary>
/// In-memory <see cref="ITransport"/> double used by unit tests of serial-protocol device adapters.
/// Captures every <see cref="WriteAsync"/> payload in <see cref="WrittenChunks"/> and lets tests
/// inject inbound bytes via <see cref="EnqueueReadAsync"/>. <see cref="CloseAsync"/> does NOT complete
/// the read channel so a single instance can be reopened in a test; call <see cref="Complete"/> or
/// <see cref="DisposeAsync"/> to terminate active readers.
/// </summary>
public sealed class MockTransport : ITransport
{
    private readonly Channel<ReadOnlyMemory<byte>> _channel = Channel.CreateUnbounded<ReadOnlyMemory<byte>>();
    private bool _disposed;

    /// <summary>True while the transport is in the open state.</summary>
    public bool IsOpen { get; private set; }

    /// <summary>Total number of times <see cref="OpenAsync"/> has been called successfully.</summary>
    public int OpenCount { get; private set; }

    /// <summary>Total number of times <see cref="CloseAsync"/> has actually transitioned from open to closed.</summary>
    public int CloseCount { get; private set; }

    /// <summary>Every payload passed to <see cref="WriteAsync"/>, in order, copied to its own array.</summary>
    public List<byte[]> WrittenChunks { get; } = new();

    /// <inheritdoc />
    public Task OpenAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (IsOpen)
        {
            throw new InvalidOperationException("MockTransport is already open.");
        }

        IsOpen = true;
        OpenCount++;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task CloseAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!IsOpen) return Task.CompletedTask;

        IsOpen = false;
        CloseCount++;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task WriteAsync(ReadOnlyMemory<byte> data, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!IsOpen)
        {
            throw new InvalidOperationException("MockTransport is not open.");
        }

        WrittenChunks.Add(data.ToArray());
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public IAsyncEnumerable<ReadOnlyMemory<byte>> ReadAllAsync(CancellationToken ct = default)
        => _channel.Reader.ReadAllAsync(ct);

    /// <summary>Test helper: enqueue a chunk of bytes to be yielded from <see cref="ReadAllAsync"/>.</summary>
    public ValueTask EnqueueReadAsync(byte[] bytes, CancellationToken ct = default)
        => _channel.Writer.WriteAsync(bytes, ct);

    /// <summary>Test helper: complete the read channel so active <see cref="ReadAllAsync"/> iterators end.</summary>
    public void Complete() => _channel.Writer.TryComplete();

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (_disposed) return ValueTask.CompletedTask;
        _disposed = true;
        Complete();
        IsOpen = false;
        return ValueTask.CompletedTask;
    }
}
