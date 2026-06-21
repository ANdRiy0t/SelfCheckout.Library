using SelfCheckout.HardwareBridge.Abstractions.Enums;
using SelfCheckout.HardwareBridge.Abstractions.Exceptions;
using SelfCheckout.HardwareBridge.Abstractions.Models;
using SelfCheckout.HardwareBridge.Devices.Mocks;
using SelfCheckout.HardwareBridge.Devices.Mocks.Options;

namespace SelfCheckout.HardwareBridge.Tests.Mocks;

public class MockScannerTests
{
    private static DeviceDescriptor ScannerDescriptor() =>
        new("test-scanner", DeviceType.Scanner, "Mock", "mock://scanner");

    private static MockScannerOptions FastOptions() =>
        new() { ScanDelay = TimeSpan.FromMilliseconds(1), ConnectDelay = TimeSpan.FromMilliseconds(1) };

    [Fact]
    public async Task ConnectAsync_SetsStateToReady()
    {
        var scanner = new MockScanner(ScannerDescriptor(), FastOptions());

        await scanner.ConnectAsync();

        Assert.Equal(DeviceState.Ready, scanner.DeviceStatus);
    }

    [Fact]
    public async Task ScanAsync_ReturnsConfiguredBarcode()
    {
        var scanner = new MockScanner(ScannerDescriptor(), FastOptions());
        await scanner.ConnectAsync();

        var result = await scanner.ScanAsync();

        Assert.Equal("1234567890123", result.Barcode);
        Assert.Equal("EAN13", result.Symbology);
    }

    [Fact]
    public async Task ScanAsync_RaisesBarcodeScannedEvent()
    {
        var scanner = new MockScanner(ScannerDescriptor(), FastOptions());
        ScanResult? eventResult = null;
        scanner.BarcodeScanned += (_, r) => eventResult = r;
        await scanner.ConnectAsync();

        await scanner.ScanAsync();

        Assert.NotNull(eventResult);
        Assert.Equal("1234567890123", eventResult!.Barcode);
    }

    [Fact]
    public async Task ScanAsync_WhenNotConnected_ThrowsDeviceException()
    {
        var scanner = new MockScanner(ScannerDescriptor(), FastOptions());

        var ex = await Assert.ThrowsAsync<DeviceException>(() => scanner.ScanAsync());

        Assert.Equal(ErrorCode.ConnectionFailed, ex.ErrorCode);
    }

    [Fact]
    public async Task ScanAsync_WithShouldFailOnScan_ThrowsDeviceException()
    {
        var options = FastOptions();
        options.ShouldFailOnScan = true;
        options.FailureErrorCode = ErrorCode.HardwareFailure;
        var scanner = new MockScanner(ScannerDescriptor(), options);
        await scanner.ConnectAsync();

        var ex = await Assert.ThrowsAsync<DeviceException>(() => scanner.ScanAsync());

        Assert.Equal(ErrorCode.HardwareFailure, ex.ErrorCode);
    }

    [Fact]
    public async Task ScanAsync_WithSimulateDisconnect_ThrowsAndSetsDisconnected()
    {
        var options = FastOptions();
        options.SimulateDisconnectOnScan = true;
        var scanner = new MockScanner(ScannerDescriptor(), options);
        await scanner.ConnectAsync();

        var ex = await Assert.ThrowsAsync<DeviceException>(() => scanner.ScanAsync());

        Assert.Equal(ErrorCode.ConnectionLost, ex.ErrorCode);
        Assert.Equal(DeviceState.Disconnected, scanner.DeviceStatus);
    }

    [Fact]
    public void SimulateScan_RaisesBarcodeScannedEvent()
    {
        var scanner = new MockScanner(ScannerDescriptor(), FastOptions());
        ScanResult? eventResult = null;
        scanner.BarcodeScanned += (_, r) => eventResult = r;

        scanner.SimulateScan("TEST123");

        Assert.NotNull(eventResult);
        Assert.Equal("TEST123", eventResult!.Barcode);
    }
}
