# SelfCheckout.HardwareBridge.Abstractions

Interfaces, DTOs, enums, and exception types for the SelfCheckout.HardwareBridge hardware abstraction library.

## What's in this package

- `IDevice`, `IBarcodeScanner`, `IWeightScale`, `IPaymentTerminal`, `IPrinter` — typed async device contracts
- DTOs: `BarcodeReading`, `WeightReading`, `PaymentRequest`, `PaymentResult`, `DeviceDescriptor`
- Enums: `DeviceStatus`, `DeviceType`, `PaymentMethod`
- `DeviceException` — unified hardware error type

## Usage

This is a contracts-only package — depend on it from libraries that target self-checkout hardware abstractions without pulling in implementations. For ready-to-use mocks + DI helpers, install `SelfCheckout.HardwareBridge.Devices` instead.

## Repository

https://github.com/and-kozmenchuk/SelfCheckout.Library
