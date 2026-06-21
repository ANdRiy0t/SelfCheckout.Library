using SelfCheckout.HardwareBridge.Abstractions.Enums;

namespace SelfCheckout.HardwareBridge.Devices.Mocks.Options;

public class MockScannerOptions
{

    public TimeSpan ScanDelay { get; set; } = TimeSpan.FromMilliseconds(100);

    public string DefaultBarcode { get; set; } = "1234567890123";

    public string DefaultSymbology { get; set; } = "EAN13";

    public bool ShouldFailOnScan { get; set; } = false;

    public ErrorCode FailureErrorCode { get; set; } = ErrorCode.HardwareFailure;

    public bool SimulateDisconnectOnScan { get; set; } = false;

    public TimeSpan ConnectDelay { get; set; } = TimeSpan.FromMilliseconds(50);
}
