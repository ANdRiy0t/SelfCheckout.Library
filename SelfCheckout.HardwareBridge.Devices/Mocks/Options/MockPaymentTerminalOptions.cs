using SelfCheckout.HardwareBridge.Abstractions.Enums;

namespace SelfCheckout.HardwareBridge.Devices.Mocks.Options;

public class MockPaymentTerminalOptions
{

    public TimeSpan ProcessDelay { get; set; } = TimeSpan.FromMilliseconds(200);

    public bool ShouldDecline { get; set; } = false;

    public string DeclineReason { get; set; } = "Insufficient funds";

    public bool ShouldFailOnProcess { get; set; } = false;

    public ErrorCode FailureErrorCode { get; set; } = ErrorCode.HardwareFailure;

    public bool SimulateDisconnectOnProcess { get; set; } = false;

    public TimeSpan ConnectDelay { get; set; } = TimeSpan.FromMilliseconds(50);
}
