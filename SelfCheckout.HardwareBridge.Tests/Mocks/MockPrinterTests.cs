using System.Diagnostics;
using SelfCheckout.HardwareBridge.Abstractions.Enums;
using SelfCheckout.HardwareBridge.Abstractions.Exceptions;
using SelfCheckout.HardwareBridge.Abstractions.Models;
using SelfCheckout.HardwareBridge.Devices.Mocks;
using SelfCheckout.HardwareBridge.Devices.Mocks.Options;

namespace SelfCheckout.HardwareBridge.Tests.Mocks;

public class MockPrinterTests
{
    private static DeviceDescriptor PrinterDescriptor() =>
        new("test-printer", DeviceType.Printer, "Mock", "mock://printer");

    private static MockPrinterOptions FastOptions() =>
        new() { PrintDelay = TimeSpan.FromMilliseconds(1), ConnectDelay = TimeSpan.FromMilliseconds(1) };

    [Fact]
    public async Task PrintRawAsync_HappyPath_StoresPayloadAndRaisesStateTransitions()
    {
        var printer = new MockPrinter(PrinterDescriptor(), FastOptions());
        var states = new List<DeviceState>();
        printer.StateChanged += (_, s) => states.Add(s);
        await printer.ConnectAsync();

        var payload = new byte[] { 1, 2, 3 };
        await printer.PrintRawAsync(payload);

        Assert.Single(printer.PrintedPayloads);
        Assert.Equal(payload, printer.PrintedPayloads[0]);
        Assert.Contains(DeviceState.Busy, states);
        Assert.Contains(DeviceState.Ready, states);
        Assert.Equal(DeviceState.Ready, printer.DeviceStatus);
    }

    [Fact]
    public async Task PrintRawAsync_ShouldFailOnPrint_ThrowsDeviceExceptionWithConfiguredErrorCode()
    {
        var options = FastOptions();
        options.ShouldFailOnPrint = true;
        options.FailureErrorCode = ErrorCode.HardwareFailure;
        var printer = new MockPrinter(PrinterDescriptor(), options);
        await printer.ConnectAsync();

        var ex = await Assert.ThrowsAsync<DeviceException>(() => printer.PrintRawAsync(new byte[] { 0 }));

        Assert.Equal(ErrorCode.HardwareFailure, ex.ErrorCode);
        Assert.Equal(DeviceState.Error, printer.DeviceStatus);
    }

    [Fact]
    public async Task PrintRawAsync_SimulateOutOfPaper_TransitionsToErrorState()
    {

        var options = FastOptions();
        options.SimulateOutOfPaper = true;
        var printer = new MockPrinter(PrinterDescriptor(), options);
        await printer.ConnectAsync();

        var ex = await Assert.ThrowsAsync<DeviceException>(() => printer.PrintRawAsync(new byte[] { 0 }));

        Assert.Equal(ErrorCode.HardwareFailure, ex.ErrorCode);
        Assert.Equal(DeviceState.Error, printer.DeviceStatus);
    }

    [Fact]
    public async Task PrintRawAsync_WhenNotConnected_ThrowsDeviceException()
    {
        var printer = new MockPrinter(PrinterDescriptor(), FastOptions());

        var ex = await Assert.ThrowsAsync<DeviceException>(() => printer.PrintRawAsync(new byte[] { 0 }));

        Assert.Equal(ErrorCode.ConnectionFailed, ex.ErrorCode);
    }

    [Fact]
    public async Task PrintRawAsync_CancellationToken_RespectsCancellation()
    {
        var options = new MockPrinterOptions
        {
            ConnectDelay = TimeSpan.FromMilliseconds(1),
            PrintDelay = TimeSpan.FromMilliseconds(500),
        };
        var printer = new MockPrinter(PrinterDescriptor(), options);
        await printer.ConnectAsync();

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(10));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => printer.PrintRawAsync(new byte[] { 0 }, cts.Token));
    }

    [Fact]
    public async Task CutAsync_HappyPath_CompletesAndRemainsReady()
    {
        var printer = new MockPrinter(PrinterDescriptor(), FastOptions());
        await printer.ConnectAsync();

        await printer.CutAsync();

        Assert.Equal(DeviceState.Ready, printer.DeviceStatus);
    }

    [Fact]
    public async Task CutAsync_ShouldFailOnPrint_ThrowsDeviceException()
    {
        var options = FastOptions();
        options.ShouldFailOnPrint = true;
        var printer = new MockPrinter(PrinterDescriptor(), options);
        await printer.ConnectAsync();

        await Assert.ThrowsAsync<DeviceException>(() => printer.CutAsync());
    }

    [Fact]
    public async Task Concurrent_TenParallelPrints_AllComplete_NoStateCorruption()
    {
        var printer = new MockPrinter(PrinterDescriptor(), FastOptions());
        await printer.ConnectAsync();

        var tasks = Enumerable.Range(0, 10)
            .Select(i => printer.PrintRawAsync(new byte[] { (byte)i }))
            .ToArray();

        await Task.WhenAll(tasks);

        Assert.Equal(10, printer.PrintedPayloads.Count);
        Assert.Equal(DeviceState.Ready, printer.DeviceStatus);
    }

    [Fact]
    public async Task ConnectAsync_HonorsConnectDelay()
    {
        var options = new MockPrinterOptions
        {
            ConnectDelay = TimeSpan.FromMilliseconds(50),
            PrintDelay = TimeSpan.FromMilliseconds(1),
        };
        var printer = new MockPrinter(PrinterDescriptor(), options);

        var sw = Stopwatch.StartNew();
        await printer.ConnectAsync();
        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds >= 40,
            $"Expected ConnectDelay >= 40ms, but elapsed was {sw.ElapsedMilliseconds}ms.");
    }

    [Fact]
    public void Dispose_DoesNotDoubleDispose()
    {
        var printer = new MockPrinter(PrinterDescriptor(), FastOptions());

        printer.Dispose();
        var ex = Record.Exception(() => printer.Dispose());

        Assert.Null(ex);
    }
}
