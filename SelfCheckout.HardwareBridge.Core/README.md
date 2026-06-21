# SelfCheckout.HardwareBridge.Core

Core infrastructure for the SelfCheckout.HardwareBridge hardware abstraction library.

## What's in this package

- `DeviceBase` — abstract base class implementing thread-safe `ConnectAsync`/`DisconnectAsync` with `SemaphoreSlim`, `IDisposable` pattern, and `StateChanged` event handling
- `DeviceManager` — registry/orchestrator for managing multiple `IDevice` instances

## Usage

Reference this package when building custom device adapters. Most application developers should install `SelfCheckout.HardwareBridge.Devices` instead, which bundles mock implementations and one-line DI setup.

## Repository

https://github.com/and-kozmenchuk/SelfCheckout.Library
