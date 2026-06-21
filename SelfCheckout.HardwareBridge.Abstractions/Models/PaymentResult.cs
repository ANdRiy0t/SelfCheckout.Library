namespace SelfCheckout.HardwareBridge.Abstractions.Models;

public record PaymentResult(bool IsApproved, string? TransactionId, string? DeclineReason, DateTimeOffset Timestamp);
