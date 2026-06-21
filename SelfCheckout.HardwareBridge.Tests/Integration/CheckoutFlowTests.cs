using SelfCheckout.HardwareBridge.Abstractions.Enums;
using SelfCheckout.HardwareBridge.Abstractions.Models;
using SelfCheckout.HardwareBridge.Core;
using SelfCheckout.HardwareBridge.Devices.Mocks;
using SelfCheckout.HardwareBridge.Devices.Mocks.Options;

namespace SelfCheckout.HardwareBridge.Tests.Integration;

public class CheckoutFlowTests
{
    [Fact]
    public async Task FullCheckoutFlow_ScanWeighPay_Succeeds()
    {

        var scanner = new MockScanner(
            new DeviceDescriptor("scanner-1", DeviceType.Scanner, "Mock", "mock://scanner"),
            new MockScannerOptions { ScanDelay = TimeSpan.FromMilliseconds(1), ConnectDelay = TimeSpan.FromMilliseconds(1) });
        var scale = new MockScale(
            new DeviceDescriptor("scale-1", DeviceType.Scale, "Mock", "mock://scale"),
            new MockScaleOptions { DefaultWeight = 0.5m, ReadDelay = TimeSpan.FromMilliseconds(1), ConnectDelay = TimeSpan.FromMilliseconds(1) });
        var terminal = new MockPaymentTerminal(
            new DeviceDescriptor("terminal-1", DeviceType.PaymentTerminal, "Mock", "mock://terminal"),
            new MockPaymentTerminalOptions { ProcessDelay = TimeSpan.FromMilliseconds(1), ConnectDelay = TimeSpan.FromMilliseconds(1) });

        await scanner.ConnectAsync();
        await scale.ConnectAsync();
        await terminal.ConnectAsync();

        var scanResult = await scanner.ScanAsync();
        Assert.NotNull(scanResult.Barcode);
        Assert.Equal("1234567890123", scanResult.Barcode);

        var weight = await scale.GetWeightAsync();
        Assert.Equal(0.5m, weight.Value);
        Assert.True(weight.IsStable);

        var payment = await terminal.ProcessPaymentAsync(9.99m, "USD");
        Assert.True(payment.IsApproved);
        Assert.NotNull(payment.TransactionId);
        Assert.NotEmpty(payment.TransactionId!);
    }

    [Fact]
    public async Task FullCheckoutFlow_WithDeviceManager_AllDevicesTracked()
    {
        using var manager = new DeviceManager();

        var scanner = new MockScanner(
            new DeviceDescriptor("scanner-1", DeviceType.Scanner, "Mock", "mock://scanner"),
            new MockScannerOptions { ConnectDelay = TimeSpan.FromMilliseconds(1), ScanDelay = TimeSpan.FromMilliseconds(1) });
        var scale = new MockScale(
            new DeviceDescriptor("scale-1", DeviceType.Scale, "Mock", "mock://scale"),
            new MockScaleOptions { ConnectDelay = TimeSpan.FromMilliseconds(1), ReadDelay = TimeSpan.FromMilliseconds(1) });
        var terminal = new MockPaymentTerminal(
            new DeviceDescriptor("terminal-1", DeviceType.PaymentTerminal, "Mock", "mock://terminal"),
            new MockPaymentTerminalOptions { ConnectDelay = TimeSpan.FromMilliseconds(1), ProcessDelay = TimeSpan.FromMilliseconds(1) });

        await scanner.ConnectAsync();
        await scale.ConnectAsync();
        await terminal.ConnectAsync();

        manager.RegisterDevice(scanner);
        manager.RegisterDevice(scale);
        manager.RegisterDevice(terminal);

        Assert.Equal(3, manager.GetDevices().Count);
        Assert.NotNull(manager.GetDevice<MockScanner>());
        Assert.NotNull(manager.GetDevice<MockScale>());
        Assert.NotNull(manager.GetDevice<MockPaymentTerminal>());

        var trackedScanner = manager.GetDevice<MockScanner>()!;
        var scanResult = await trackedScanner.ScanAsync();
        Assert.NotNull(scanResult.Barcode);

        var trackedScale = manager.GetDevice<MockScale>()!;
        var weight = await trackedScale.GetWeightAsync();
        Assert.True(weight.Value > 0);

        var trackedTerminal = manager.GetDevice<MockPaymentTerminal>()!;
        var payment = await trackedTerminal.ProcessPaymentAsync(9.99m, "USD");
        Assert.True(payment.IsApproved);
    }

    [Fact]
    public async Task CheckoutFlow_PaymentDeclined_ReturnsDeclineReason()
    {
        var scanner = new MockScanner(
            new DeviceDescriptor("scanner-1", DeviceType.Scanner, "Mock", "mock://scanner"),
            new MockScannerOptions { ConnectDelay = TimeSpan.FromMilliseconds(1), ScanDelay = TimeSpan.FromMilliseconds(1) });
        var scale = new MockScale(
            new DeviceDescriptor("scale-1", DeviceType.Scale, "Mock", "mock://scale"),
            new MockScaleOptions { ConnectDelay = TimeSpan.FromMilliseconds(1), ReadDelay = TimeSpan.FromMilliseconds(1) });
        var terminal = new MockPaymentTerminal(
            new DeviceDescriptor("terminal-1", DeviceType.PaymentTerminal, "Mock", "mock://terminal"),
            new MockPaymentTerminalOptions
            {
                ConnectDelay = TimeSpan.FromMilliseconds(1),
                ProcessDelay = TimeSpan.FromMilliseconds(1),
                ShouldDecline = true,
                DeclineReason = "Card expired"
            });

        await scanner.ConnectAsync();
        await scale.ConnectAsync();
        await terminal.ConnectAsync();

        var scanResult = await scanner.ScanAsync();
        Assert.NotNull(scanResult.Barcode);

        var weight = await scale.GetWeightAsync();
        Assert.True(weight.Value > 0);

        var payment = await terminal.ProcessPaymentAsync(9.99m, "USD");
        Assert.False(payment.IsApproved);
        Assert.Equal("Card expired", payment.DeclineReason);
    }
}
