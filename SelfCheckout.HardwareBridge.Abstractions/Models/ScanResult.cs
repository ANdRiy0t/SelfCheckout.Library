namespace SelfCheckout.HardwareBridge.Abstractions.Models;

public record ScanResult(string Barcode, string Symbology, DateTimeOffset Timestamp);
