using System.Diagnostics;
using SelfCheckout.HardwareBridge.Abstractions.Enums;
using SelfCheckout.HardwareBridge.Abstractions.Interfaces;
using SelfCheckout.HardwareBridge.Abstractions.Models;
using SelfCheckout.HardwareBridge.Core;
using SelfCheckout.HardwareBridge.Devices.Mocks;
using SelfCheckout.HardwareBridge.Devices.Mocks.Options;

namespace SelfCheckout.HardwareBridge.Tests.Core;

public class DeviceManagerTests
{
    private static DeviceDescriptor ScannerDescriptor(string id = "test-scanner") =>
        new(id, DeviceType.Scanner, "Mock", "mock://scanner");

    private static MockScannerOptions FastOptions() =>
        new() { ScanDelay = TimeSpan.FromMilliseconds(1), ConnectDelay = TimeSpan.FromMilliseconds(1) };

    [Fact]
    public void RegisterDevice_AddsDeviceToCollection()
    {
        using var manager = new DeviceManager();
        var scanner = new MockScanner(ScannerDescriptor(), FastOptions());

        manager.RegisterDevice(scanner);

        Assert.Equal(1, manager.GetDevices().Count);
        Assert.NotNull(manager.GetDevice("test-scanner"));
    }

    [Fact]
    public void RegisterDevice_FiresDeviceConnectedEvent()
    {
        using var manager = new DeviceManager();
        var scanner = new MockScanner(ScannerDescriptor(), FastOptions());
        IDevice? connectedDevice = null;
        manager.DeviceConnected += (_, device) => connectedDevice = device;

        manager.RegisterDevice(scanner);

        Assert.NotNull(connectedDevice);
        Assert.Equal("test-scanner", connectedDevice!.Descriptor.Id);
    }

    [Fact]
    public void RegisterDevice_DuplicateId_DoesNotAddTwice()
    {
        using var manager = new DeviceManager();
        var scanner = new MockScanner(ScannerDescriptor(), FastOptions());

        manager.RegisterDevice(scanner);
        manager.RegisterDevice(scanner);

        Assert.Equal(1, manager.GetDevices().Count);
    }

    [Fact]
    public void UnregisterDevice_RemovesDevice()
    {
        using var manager = new DeviceManager();
        var scanner = new MockScanner(ScannerDescriptor(), FastOptions());
        manager.RegisterDevice(scanner);

        var result = manager.UnregisterDevice("test-scanner");

        Assert.True(result);
        Assert.Empty(manager.GetDevices());
    }

    [Fact]
    public void UnregisterDevice_UnknownId_ReturnsFalse()
    {
        using var manager = new DeviceManager();

        var result = manager.UnregisterDevice("nonexistent");

        Assert.False(result);
    }

    [Fact]
    public void GetDevice_ByType_ReturnsCorrectDevice()
    {
        using var manager = new DeviceManager();
        var scanner = new MockScanner(ScannerDescriptor(), FastOptions());
        manager.RegisterDevice(scanner);

        var found = manager.GetDevice<MockScanner>();

        Assert.NotNull(found);
        Assert.Equal("test-scanner", found!.Descriptor.Id);
    }

    [Fact]
    public void GetDevice_ByType_WhenNoneRegistered_ReturnsNull()
    {
        using var manager = new DeviceManager();

        var found = manager.GetDevice<MockScanner>();

        Assert.Null(found);
    }

    [Fact]
    public async Task DeviceDisconnected_FiresWithin2Seconds()
    {
        using var manager = new DeviceManager();
        var scanner = new MockScanner(
            new DeviceDescriptor("test-scanner", DeviceType.Scanner, "Mock", "mock://scanner"),
            new MockScannerOptions { ConnectDelay = TimeSpan.FromMilliseconds(1) });
        await scanner.ConnectAsync();
        manager.RegisterDevice(scanner);

        var disconnectedTcs = new TaskCompletionSource<IDevice>();
        manager.DeviceDisconnected += (_, device) => disconnectedTcs.TrySetResult(device);

        var sw = Stopwatch.StartNew();
        await scanner.DisconnectAsync();
        var disconnectedDevice = await disconnectedTcs.Task.WaitAsync(TimeSpan.FromSeconds(2));
        sw.Stop();

        Assert.Equal("test-scanner", disconnectedDevice.Descriptor.Id);
        Assert.True(sw.ElapsedMilliseconds < 2000, $"Disconnect event took {sw.ElapsedMilliseconds}ms, expected < 2000ms");
    }

    [Fact]
    public async Task Dispose_UnsubscribesFromAllDeviceEvents()
    {
        var manager = new DeviceManager();
        var scanner = new MockScanner(
            ScannerDescriptor(),
            new MockScannerOptions { ConnectDelay = TimeSpan.FromMilliseconds(1) });
        await scanner.ConnectAsync();
        manager.RegisterDevice(scanner);

        var disconnectedFired = false;
        manager.DeviceDisconnected += (_, _) => disconnectedFired = true;

        manager.Dispose();

        await scanner.DisconnectAsync();

        Assert.False(disconnectedFired);
    }
}
