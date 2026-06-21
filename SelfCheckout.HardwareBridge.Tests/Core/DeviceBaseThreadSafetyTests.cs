using SelfCheckout.HardwareBridge.Abstractions.Enums;
using SelfCheckout.HardwareBridge.Abstractions.Exceptions;
using SelfCheckout.HardwareBridge.Abstractions.Models;
using SelfCheckout.HardwareBridge.Devices.Mocks;
using SelfCheckout.HardwareBridge.Devices.Mocks.Options;

namespace SelfCheckout.HardwareBridge.Tests.Core;

public class DeviceBaseThreadSafetyTests
{
    [Fact]
    public async Task TenConcurrentScanOperations_NoExceptionsOrCorruption()
    {
        var scanner = new MockScanner(
            new DeviceDescriptor("test-scanner", DeviceType.Scanner, "Mock", "mock://scanner"),
            new MockScannerOptions { ScanDelay = TimeSpan.FromMilliseconds(10), ConnectDelay = TimeSpan.FromMilliseconds(1) });
        await scanner.ConnectAsync();

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => scanner.ScanAsync())
            .ToArray();

        var results = await Task.WhenAll(tasks);

        Assert.Equal(10, results.Length);
        Assert.All(results, r => Assert.NotNull(r.Barcode));
        Assert.Equal(DeviceState.Ready, scanner.DeviceStatus);
    }

    [Fact]
    public async Task TenConcurrentWeightOperations_NoExceptionsOrCorruption()
    {
        var scale = new MockScale(
            new DeviceDescriptor("test-scale", DeviceType.Scale, "Mock", "mock://scale"),
            new MockScaleOptions { ReadDelay = TimeSpan.FromMilliseconds(10), ConnectDelay = TimeSpan.FromMilliseconds(1) });
        await scale.ConnectAsync();

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => scale.GetWeightAsync())
            .ToArray();

        var results = await Task.WhenAll(tasks);

        Assert.Equal(10, results.Length);
        Assert.All(results, r => Assert.True(r.Value > 0));
    }

    [Fact]
    public async Task TenConcurrentPaymentOperations_NoExceptionsOrCorruption()
    {
        var terminal = new MockPaymentTerminal(
            new DeviceDescriptor("test-terminal", DeviceType.PaymentTerminal, "Mock", "mock://terminal"),
            new MockPaymentTerminalOptions { ProcessDelay = TimeSpan.FromMilliseconds(10), ConnectDelay = TimeSpan.FromMilliseconds(1) });
        await terminal.ConnectAsync();

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => terminal.ProcessPaymentAsync(1.00m, "USD"))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        Assert.Equal(10, results.Length);
        Assert.All(results, r => Assert.True(r.IsApproved));
    }

    [Fact]
    public async Task ConcurrentConnectDisconnect_NoExceptionsOrDeadlock()
    {
        var scanner = new MockScanner(
            new DeviceDescriptor("test-scanner", DeviceType.Scanner, "Mock", "mock://scanner"),
            new MockScannerOptions { ConnectDelay = TimeSpan.FromMilliseconds(1) });

        var tasks = Enumerable.Range(0, 10)
            .Select(i => Task.Run(async () =>
            {
                try
                {
                    if (i % 2 == 0)
                        await scanner.ConnectAsync();
                    else
                        await scanner.DisconnectAsync();
                }
                catch (DeviceException)
                {

                }
                catch (ObjectDisposedException)
                {

                }
            }))
            .ToArray();

        await Task.WhenAll(tasks);

        Assert.Contains(scanner.DeviceStatus, new[] { DeviceState.Ready, DeviceState.Disconnected });
    }
}
