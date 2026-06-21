# SelfCheckout.HardwareBridge

A unified, extensible .NET library for integrating self-checkout hardware -- barcode scanners, weight scales, and payment terminals -- into any .NET application.

## Features

- **Typed async interfaces** for barcode scanners, weight scales, and payment terminals
- **Simple DI registration** with `services.AddSelfCheckoutDevices().AddMockDevices()`
- **Mock implementations** included for testing and development
- **Thread-safe** device operations with async/await
- **Extensible** -- implement `IDevice` to add custom hardware adapters

## Quick Start

### Installation

```bash
dotnet add package SelfCheckout.HardwareBridge.Devices
```

### Register Services

```csharp
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddSelfCheckoutDevices().AddMockDevices();
var provider = services.BuildServiceProvider();
```

### Use Devices

```csharp
using SelfCheckout.HardwareBridge.Abstractions.Interfaces;

// Resolve devices from DI
var scanner = provider.GetRequiredService<IBarcodeScanner>();
var scale = provider.GetRequiredService<IWeightScale>();
var terminal = provider.GetRequiredService<IPaymentTerminal>();

// Connect
await scanner.ConnectAsync();
await scale.ConnectAsync();
await terminal.ConnectAsync();

// Scan a barcode
var scanResult = await scanner.ScanAsync();
Console.WriteLine($"Scanned: {scanResult.Barcode}");

// Read weight
var weight = await scale.GetWeightAsync();
Console.WriteLine($"Weight: {weight.Value} {weight.Unit}");

// Process payment
var payment = await terminal.ProcessPaymentAsync(9.99m, "USD");
Console.WriteLine($"Payment approved: {payment.IsApproved}");
```

## Packages

| Package | Description |
|---------|-------------|
| `SelfCheckout.HardwareBridge.Abstractions` | Interfaces, DTOs, enums, and exception types |
| `SelfCheckout.HardwareBridge.Core` | Base classes and device management |
| `SelfCheckout.HardwareBridge.Devices` | Mock implementations and DI integration |

## Requirements

- .NET 8.0 or later

## License

MIT
