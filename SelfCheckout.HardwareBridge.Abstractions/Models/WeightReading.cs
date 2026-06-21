namespace SelfCheckout.HardwareBridge.Abstractions.Models;

public record WeightReading(decimal Value, string Unit, bool IsStable, DateTimeOffset Timestamp);
