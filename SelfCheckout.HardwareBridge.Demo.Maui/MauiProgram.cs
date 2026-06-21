using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using SelfCheckout.HardwareBridge.Abstractions.Interfaces;
using SelfCheckout.HardwareBridge.Abstractions.Models;
using SelfCheckout.HardwareBridge.Core;
using SelfCheckout.HardwareBridge.Demo.Maui.ViewModels;
using SelfCheckout.HardwareBridge.Demo.Maui.Views;
using SelfCheckout.HardwareBridge.Devices.Mocks;
using SelfCheckout.HardwareBridge.Devices.Mocks.Options;
using SelfCheckout.HardwareBridge.Devices.Real;
using SelfCheckout.HardwareBridge.Devices.Real.Options;
using SelfCheckout.HardwareBridge.Devices.Receipts;
using SelfCheckout.HardwareBridge.Transports.Mocks;
using SelfCheckout.HardwareBridge.Transports.Options;
using ZXing.Net.Maui.Controls;

using DeviceType = SelfCheckout.HardwareBridge.Abstractions.Enums.DeviceType;

namespace SelfCheckout.HardwareBridge.Demo.Maui;

public static class MauiProgram
{
    // ← set false to drive real serial hardware instead of mocks. Requires a rebuild.
    // When false, PaymentTerminal STILL stays Mock (real terminals need a merchant account + PCI scope).
    // Leave true for the Android emulator demo — real adapters would try to open non-existent COM ports.
    private const bool UseMockDevices = true;

    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseBarcodeReader()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        var scannerOptions  = new MockScannerOptions
        {
            ScanDelay = TimeSpan.FromMilliseconds(300),
            ConnectDelay = TimeSpan.FromMilliseconds(300),
        };
        var scaleOptions    = new MockScaleOptions
        {
            ReadDelay = TimeSpan.FromMilliseconds(300),
            ConnectDelay = TimeSpan.FromMilliseconds(300),
        };
        var terminalOptions = new MockPaymentTerminalOptions
        {
            ProcessDelay = TimeSpan.FromMilliseconds(400),
            ConnectDelay = TimeSpan.FromMilliseconds(300),
        };
        var printerOptions  = new MockPrinterOptions
        {
            PrintDelay = TimeSpan.FromMilliseconds(300),
            ConnectDelay = TimeSpan.FromMilliseconds(300),
        };

        builder.Services.AddSingleton(scannerOptions);
        builder.Services.AddSingleton(scaleOptions);
        builder.Services.AddSingleton(terminalOptions);
        builder.Services.AddSingleton(printerOptions);

        // Concrete Mock singletons ALWAYS stay registered regardless of the toggle:
        // DevicesViewModel injects the concrete MockScanner (to call SimulateScan) and
        // SettingsViewModel injects the concrete Mock*Options (to mutate error-injection flags live).
        // The toggle below ONLY swaps which type backs the IBarcodeScanner/IWeightScale/IPrinter forwards.
        builder.Services.AddSingleton(_ => new MockScanner(
            new DeviceDescriptor("mock-scanner-001", DeviceType.Scanner, "MockScanner", "mock"),
            scannerOptions));
        builder.Services.AddSingleton(_ => new MockPrinter(
            new DeviceDescriptor("mock-printer-001", DeviceType.Printer, "MockPrinter", "mock"),
            printerOptions));

        // PaymentTerminal is ALWAYS Mock — real terminals require a merchant account + PCI compliance (out of scope).
        builder.Services.AddSingleton<IPaymentTerminal>(_ => new MockPaymentTerminal(
            new DeviceDescriptor("mock-terminal-001", DeviceType.PaymentTerminal, "MockPaymentTerminal", "mock"),
            terminalOptions));

        if (UseMockDevices)
        {
            builder.Services.AddSingleton<IBarcodeScanner>(sp => sp.GetRequiredService<MockScanner>());
            builder.Services.AddSingleton<IWeightScale>(_ => new MockScale(
                new DeviceDescriptor("mock-scale-001", DeviceType.Scale, "MockScale", "mock"),
                scaleOptions));
            builder.Services.AddSingleton<IPrinter>(sp => sp.GetRequiredService<MockPrinter>());
        }
        else
        {
            // Real-hardware path now flows through the published fluent extension methods.
            // COM ports below MUST be set to the operator's actual ports before running on real hardware.
            builder.Services
                .AddSerialScanner(t => t.PortName = "COM3")
                .AddSerialScale(t => t.PortName = "COM4")
                .AddEscPosPrinter(t => t.PortName = "COM5");
        }

        // IDeviceManager + PDF receipt renderer now come from the published extension methods.
        // (The custom-delay Mock*Options + concrete Mock singletons above stay manual — D-03 deviation —
        // because SettingsViewModel mutates them live and DevicesViewModel injects the concrete MockScanner.)
        // The injected IReceiptRenderer now produces PDF bytes via SkiaSharp (Android-compatible),
        // feeding BOTH the Download Receipt PDF flow (Share sheet) AND the existing Print Receipt
        // path (MockPrinter accepts any byte[], so the thermal-print command still "succeeds").
        // SkiaSharp is used instead of QuestPDF because QuestPDF's native Skia binary is not
        // compiled for Android and throws TypeInitializationException on-device.
        builder.Services.AddSelfCheckoutDevices().AddSkiaReceiptRenderer();

        // Transports layer (quick task 260531-sba): MockTransport for the Transports tab demo
        // and a default SerialTransportOptions instance for the info-only readout. We do NOT
        // register SerialTransport itself — it would try to open a non-existent COM port on Android.
        builder.Services.AddSingleton<MockTransport>();
        builder.Services.AddSingleton<SerialTransportOptions>(_ => new SerialTransportOptions());

        // Tab ViewModels + Pages (singletons mirror the original MainViewModel/MainPage lifetimes).
        builder.Services.AddSingleton<DevicesViewModel>();
        builder.Services.AddSingleton<CheckoutViewModel>();
        builder.Services.AddSingleton<TransportsViewModel>();
        builder.Services.AddSingleton<SettingsViewModel>();

        builder.Services.AddSingleton<DevicesPage>();
        builder.Services.AddSingleton<CheckoutPage>();
        builder.Services.AddSingleton<TransportsPage>();
        builder.Services.AddSingleton<SettingsPage>();

        builder.Services.AddTransient<CameraScanViewModel>();
        builder.Services.AddTransient<CameraScanPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}
