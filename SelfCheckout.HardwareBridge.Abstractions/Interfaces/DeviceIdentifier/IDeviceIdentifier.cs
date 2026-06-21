using SelfCheckout.HardwareBridge.Abstractions.Enums;
using SelfCheckout.HardwareBridge.Abstractions.Models;

namespace SelfCheckout.HardwareBridge.Abstractions.Interfaces.DeviceIdentifier;

public record IdentifyContext(string Path, string? PnpId, string? Vid, string? Pid);

public record IdentifyResult(bool IsMatch, string? Model = null, int Confidence = 0);

public interface IDeviceIdentifier
{

    DeviceType Type { get; }

    Task<IdentifyResult> ProbeAsync(IdentifyContext ctx, CancellationToken ct = default);

    IDevice CreateDevice(DeviceDescriptor descriptor);
}
