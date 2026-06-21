using SelfCheckout.HardwareBridge.Abstractions.Enums;

namespace SelfCheckout.HardwareBridge.Abstractions.Models;

public record DeviceDescriptor(string Id, DeviceType Type, string Model, string Connection);
