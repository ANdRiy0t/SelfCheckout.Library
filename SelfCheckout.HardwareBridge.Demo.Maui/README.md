# SelfCheckout.HardwareBridge.Demo.Maui

A .NET MAUI Android demo that consumes the **SelfCheckout.HardwareBridge.Devices** library and demonstrates the full self-checkout flow end-to-end: **scan -> weigh -> pay** with a live event log, colored device-state badges, and UI-driven error injection.

This is a **sample/showcase app**, not a packaged library. The library remains library-only; this project proves the library can be dropped into any .NET 8+ application, including a MAUI mobile app.

## Prerequisites

| Tool | Version | Purpose |
|------|---------|---------|
| .NET SDK | 9.0.x | Build/run |
| MAUI Android workload | (any 9.0-compatible) | `dotnet workload install maui-android` |
| Microsoft OpenJDK | 17+ | Android build |
| Android SDK | Platform 35 + Build Tools | Compile |
| Android emulator (AVD) OR USB device | API 24+ | Run target |

Verify with:

```powershell
dotnet --version
dotnet workload list
java -version
adb devices
```

If `maui-android` is missing:

```powershell
dotnet workload install maui-android
```

## Build & Run

From the repository root:

```powershell
# Build APK only
dotnet build SelfCheckout.HardwareBridge.Demo.Maui\SelfCheckout.HardwareBridge.Demo.Maui.csproj -f net9.0-android

# Build and deploy to the connected device/emulator
dotnet build SelfCheckout.HardwareBridge.Demo.Maui\SelfCheckout.HardwareBridge.Demo.Maui.csproj -t:Run -f net9.0-android
```

From Visual Studio / JetBrains Rider: open `SelfCheckoutLibrary.sln`, set `SelfCheckout.HardwareBridge.Demo.Maui` as the startup project, select an Android emulator from the run target dropdown, then press **F5**.

## What the demo shows

The single `MainPage` displays three device cards (Scanner, Scale, Payment Terminal). Each card shows:

- A **colored badge + status text** indicating the current `DeviceState` (Disconnected = gray, Connecting = yellow, Ready = green, Busy = blue, Error = red).
- The most recent operation result (last barcode, current weight, etc.).
- A per-device action button (Scan / Weigh / Pay).

Two global buttons at the bottom:

- **Connect All** -- connects all three devices in sequence.
- **Run Full Checkout** -- executes the full flow (Connect -> Scan -> Weigh -> Pay) with short delays so the state transitions are visible during a live demo.

Below the cards, an **event log** (`CollectionView`) shows `[HH:mm:ss] [LEVEL] text` entries, newest on top, capped at the most recent 50.

A toolbar **Settings** button opens a modal `SettingsPage` with six switches that flip live error-injection flags on the mock devices:

- Scanner: fail on scan, simulate disconnect on scan
- Scale: fail on weight read
- Payment Terminal: decline, fail on process, simulate disconnect on process

Toggle any switch, close the settings page, then tap the relevant action button -- the mock device now exhibits the injected behavior. Errors surface as **Error-level log entries AND a Snackbar** at the bottom of the screen (per `CommunityToolkit.Maui`).

## Architecture

- **MVVM** via [`CommunityToolkit.Mvvm`](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/) -- `[ObservableProperty]` and `[RelayCommand]` source generators throughout.
- **DI** via `Microsoft.Extensions.DependencyInjection` in `MauiProgram.cs`. Singletons for mocks, options, the device manager, ViewModels, and Pages.
- **UI-thread marshaling** via `MainThread.BeginInvokeOnMainThread` inside every device-event handler that mutates `ObservableCollection<LogEntry>` or `[ObservableProperty]` fields.
- **Snackbars** via `CommunityToolkit.Maui` `Snackbar.Make(...).Show()` for high-visibility error events during the live demo.

## DI registration: a note on the "advanced control" pattern

The library's standard one-line DI registration is:

```csharp
services.AddSelfCheckoutDevices();
```

This is what the library `README.md` documents and what consumers should use in production. **However**, `AddSelfCheckoutDevices()` constructs the mocks with `new MockScannerOptions()` (etc.) inside the factory and stores them in a private field, so the consumer cannot mutate the option toggles at runtime.

This demo's settings page requires live mutation of those options (to flip `ShouldFailOnScan`, `ShouldDecline`, etc. via the on-screen switches). Therefore the demo registers the mocks **manually**, capturing each `*Options` singleton so the `SettingsViewModel` can flip flags on the same instance the mock is using:

```csharp
var scannerOptions  = new MockScannerOptions  { ScanDelay = TimeSpan.FromMilliseconds(300) };
var scaleOptions    = new MockScaleOptions    { ReadDelay = TimeSpan.FromMilliseconds(300) };
var terminalOptions = new MockPaymentTerminalOptions { ProcessDelay = TimeSpan.FromMilliseconds(400) };

builder.Services.AddSingleton(scannerOptions);
builder.Services.AddSingleton(scaleOptions);
builder.Services.AddSingleton(terminalOptions);

builder.Services.AddSingleton<IBarcodeScanner>(_ => new MockScanner(
    new DeviceDescriptor("mock-scanner-001", DeviceType.Scanner, "MockScanner", "mock"),
    scannerOptions));
// ... mock scale + terminal registered the same way ...

builder.Services.AddSingleton<IDeviceManager, DeviceManager>();
```

**Use `AddSelfCheckoutDevices()` for production** when you want the zero-configuration default. **Use the manual pattern shown above only when you need runtime control over mock behavior** -- e.g., for demos, integration tests, or harnesses that inject failures.

## Project layout

```
SelfCheckout.HardwareBridge.Demo.Maui/
  MauiProgram.cs              # DI composition root (manual registration)
  App.xaml / App.xaml.cs      # converters + styles registration
  AppShell.xaml / .cs
  Views/
    MainPage.xaml / .cs       # device cards + action buttons + event log
    SettingsPage.xaml / .cs   # error-injection switches (modal)
  ViewModels/
    MainViewModel.cs          # checkout flow + log management + event handlers
    SettingsViewModel.cs      # toggle bindings -> live option mutation
    LogEntry.cs               # log row record + LogLevel enum
  Converters/
    DeviceStateToColorConverter.cs
    DeviceStateToDisplayStringConverter.cs
    LogLevelToColorConverter.cs
  Platforms/Android/          # Android-only platform code
  Resources/                  # default MAUI assets (fonts, icons, splash)
```

## Limitations

- **Android only.** No iOS, MacCatalyst, or Windows targets. The `.csproj` declares `<TargetFramework>net9.0-android</TargetFramework>` (single TFM).
- **Mock devices only.** Real hardware adapters (Serial/USB barcode scanners, scale RS-232, EFTPOS terminals) are out of scope for v1 of the library.
- **No persistence.** The event log is in-memory and resets on app restart.
- **No authentication, no networking.** The demo runs entirely offline.

## License

MIT (matches the library).
