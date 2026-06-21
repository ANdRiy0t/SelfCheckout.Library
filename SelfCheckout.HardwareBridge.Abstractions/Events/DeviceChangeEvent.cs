namespace SelfCheckout.HardwareBridge.Abstractions.Events;

public enum DeviceChangeKind
{

    Arrived,

    Removed
}

public record DeviceChangeEvent(
    DeviceChangeKind Kind,
    string Path,
    string? PnpId = null,
    string? Vid = null,
    string? Pid = null
);
