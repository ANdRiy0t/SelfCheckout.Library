using SelfCheckout.HardwareBridge.Abstractions.Enums;

namespace SelfCheckout.HardwareBridge.Devices.Mocks.Options;

public class MockScaleOptions
{

    public decimal DefaultWeight { get; set; } = 1.5m;

    public string Unit { get; set; } = "kg";

    public bool IsStable { get; set; } = true;

    public TimeSpan ReadDelay { get; set; } = TimeSpan.FromMilliseconds(50);

    public bool ShouldFailOnRead { get; set; } = false;

    public ErrorCode FailureErrorCode { get; set; } = ErrorCode.HardwareFailure;

    public TimeSpan ConnectDelay { get; set; } = TimeSpan.FromMilliseconds(50);
}
