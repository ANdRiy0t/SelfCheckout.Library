using SelfCheckout.HardwareBridge.Abstractions.Enums;

namespace SelfCheckout.HardwareBridge.Abstractions.Models;

/// <summary>Payment method and amount tendered for a receipt.</summary>
public record PaymentInfo(PaymentMethod Method, decimal Amount);
