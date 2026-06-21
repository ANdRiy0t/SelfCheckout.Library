namespace SelfCheckout.HardwareBridge.Abstractions.Interfaces;

public interface ITransport : IAsyncDisposable
{

    Task OpenAsync(CancellationToken ct = default);

    Task CloseAsync(CancellationToken ct = default);

    Task WriteAsync(ReadOnlyMemory<byte> data, CancellationToken ct = default);

    IAsyncEnumerable<ReadOnlyMemory<byte>> ReadAllAsync(CancellationToken ct = default);
}
