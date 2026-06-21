using SelfCheckout.HardwareBridge.Abstractions.Enums;
using SelfCheckout.HardwareBridge.Abstractions.Exceptions;
using SelfCheckout.HardwareBridge.Abstractions.Models;
using SelfCheckout.HardwareBridge.Devices.Mocks;
using SelfCheckout.HardwareBridge.Devices.Mocks.Options;

namespace SelfCheckout.HardwareBridge.Tests.Mocks;

public class MockPaymentTerminalTests
{
    private static DeviceDescriptor TerminalDescriptor() =>
        new("test-terminal", DeviceType.PaymentTerminal, "Mock", "mock://terminal");

    private static MockPaymentTerminalOptions FastOptions() =>
        new() { ProcessDelay = TimeSpan.FromMilliseconds(1), ConnectDelay = TimeSpan.FromMilliseconds(1) };

    [Fact]
    public async Task ProcessPaymentAsync_Approved_ReturnsSuccess()
    {
        var terminal = new MockPaymentTerminal(TerminalDescriptor(), FastOptions());
        await terminal.ConnectAsync();

        var result = await terminal.ProcessPaymentAsync(9.99m, "USD");

        Assert.True(result.IsApproved);
        Assert.NotNull(result.TransactionId);
        Assert.NotEmpty(result.TransactionId!);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WithShouldDecline_ReturnsDeclied()
    {
        var options = FastOptions();
        options.ShouldDecline = true;
        options.DeclineReason = "Insufficient funds";
        var terminal = new MockPaymentTerminal(TerminalDescriptor(), options);
        await terminal.ConnectAsync();

        var result = await terminal.ProcessPaymentAsync(9.99m, "USD");

        Assert.False(result.IsApproved);
        Assert.Equal("Insufficient funds", result.DeclineReason);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WhenNotConnected_ThrowsDeviceException()
    {
        var terminal = new MockPaymentTerminal(TerminalDescriptor(), FastOptions());

        var ex = await Assert.ThrowsAsync<DeviceException>(() => terminal.ProcessPaymentAsync(9.99m, "USD"));

        Assert.Equal(ErrorCode.ConnectionFailed, ex.ErrorCode);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WithShouldFail_ThrowsDeviceException()
    {
        var options = FastOptions();
        options.ShouldFailOnProcess = true;
        var terminal = new MockPaymentTerminal(TerminalDescriptor(), options);
        await terminal.ConnectAsync();

        var ex = await Assert.ThrowsAsync<DeviceException>(() => terminal.ProcessPaymentAsync(9.99m, "USD"));

        Assert.Equal(ErrorCode.HardwareFailure, ex.ErrorCode);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WithSimulateDisconnect_ThrowsAndSetsDisconnected()
    {
        var options = FastOptions();
        options.SimulateDisconnectOnProcess = true;
        var terminal = new MockPaymentTerminal(TerminalDescriptor(), options);
        await terminal.ConnectAsync();

        var ex = await Assert.ThrowsAsync<DeviceException>(() => terminal.ProcessPaymentAsync(9.99m, "USD"));

        Assert.Equal(ErrorCode.ConnectionLost, ex.ErrorCode);
        Assert.Equal(DeviceState.Disconnected, terminal.DeviceStatus);
    }
}
