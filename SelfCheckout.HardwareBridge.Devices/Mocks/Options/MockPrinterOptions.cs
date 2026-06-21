using SelfCheckout.HardwareBridge.Abstractions.Enums;

namespace SelfCheckout.HardwareBridge.Devices.Mocks.Options;

public class MockPrinterOptions
{

    public TimeSpan ConnectDelay { get; set; } = TimeSpan.FromMilliseconds(50);

    public TimeSpan PrintDelay { get; set; } = TimeSpan.FromMilliseconds(50);

    public bool ShouldFailOnPrint { get; set; } = false;

    public bool SimulateOutOfPaper { get; set; } = false;

    public ErrorCode FailureErrorCode { get; set; } = ErrorCode.HardwareFailure;
}
