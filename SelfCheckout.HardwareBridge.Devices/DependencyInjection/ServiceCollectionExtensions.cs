using SelfCheckout.HardwareBridge.Abstractions.Enums;
using SelfCheckout.HardwareBridge.Abstractions.Interfaces;
using SelfCheckout.HardwareBridge.Abstractions.Models;
using SelfCheckout.HardwareBridge.Core;
using SelfCheckout.HardwareBridge.Devices.Mocks;
using SelfCheckout.HardwareBridge.Devices.Real;
using SelfCheckout.HardwareBridge.Devices.Real.Options;
using SelfCheckout.HardwareBridge.Devices.Receipts;
using SelfCheckout.HardwareBridge.Transports.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Dependency-injection extension methods for registering SelfCheckout.HardwareBridge services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the core device infrastructure (<see cref="IDeviceManager"/>) only.
    /// No device implementations are registered — production consumers add the
    /// devices they need explicitly via the fluent <c>AddMock*</c>, <c>AddSerial*</c>,
    /// <c>AddEscPosPrinter</c> and <c>Add*ReceiptRenderer</c> methods.
    /// </summary>
    /// <example>
    /// <code>
    /// // Development / demo: in-memory mocks
    /// services.AddSelfCheckoutDevices()
    ///         .AddMockDevices()
    ///         .AddEscPosReceiptRenderer();
    ///
    /// // Production: real serial hardware
    /// services.AddSelfCheckoutDevices()
    ///         .AddSerialScanner(t => t.PortName = "COM3")
    ///         .AddSerialScale(t => t.PortName = "COM4")
    ///         .AddEscPosPrinter(t => t.PortName = "COM5")
    ///         .AddEscPosReceiptRenderer();
    /// </code>
    /// </example>
    /// <param name="services">The service collection to add registrations to.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for fluent chaining.</returns>
    public static IServiceCollection AddSelfCheckoutDevices(this IServiceCollection services)
    {
        services.AddSingleton<IDeviceManager, DeviceManager>();
        return services;
    }

    /// <summary>
    /// Registers all four in-memory mock devices (scanner, scale, payment terminal, printer),
    /// each behind its interface and as a resolvable concrete type.
    /// </summary>
    /// <param name="services">The service collection to add registrations to.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for fluent chaining.</returns>
    public static IServiceCollection AddMockDevices(this IServiceCollection services)
    {
        return services
            .AddMockScanner()
            .AddMockScale()
            .AddMockPaymentTerminal()
            .AddMockPrinter();
    }

    /// <summary>
    /// Registers a <see cref="MockScanner"/> as both <see cref="IBarcodeScanner"/> and its
    /// concrete type (the concrete registration lets view models inject it directly).
    /// </summary>
    /// <param name="services">The service collection to add registrations to.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for fluent chaining.</returns>
    public static IServiceCollection AddMockScanner(this IServiceCollection services)
    {
        services.AddSingleton(_ => new MockScanner(
            new DeviceDescriptor("mock-scanner-001", DeviceType.Scanner, "MockScanner", "mock")));
        services.AddSingleton<IBarcodeScanner>(sp => sp.GetRequiredService<MockScanner>());
        return services;
    }

    /// <summary>
    /// Registers a <see cref="MockScale"/> as both <see cref="IWeightScale"/> and its concrete type.
    /// </summary>
    /// <param name="services">The service collection to add registrations to.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for fluent chaining.</returns>
    public static IServiceCollection AddMockScale(this IServiceCollection services)
    {
        services.AddSingleton(_ => new MockScale(
            new DeviceDescriptor("mock-scale-001", DeviceType.Scale, "MockScale", "mock")));
        services.AddSingleton<IWeightScale>(sp => sp.GetRequiredService<MockScale>());
        return services;
    }

    /// <summary>
    /// Registers a <see cref="MockPaymentTerminal"/> as both <see cref="IPaymentTerminal"/> and its concrete type.
    /// </summary>
    /// <param name="services">The service collection to add registrations to.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for fluent chaining.</returns>
    public static IServiceCollection AddMockPaymentTerminal(this IServiceCollection services)
    {
        services.AddSingleton(_ => new MockPaymentTerminal(
            new DeviceDescriptor("mock-terminal-001", DeviceType.PaymentTerminal, "MockPaymentTerminal", "mock")));
        services.AddSingleton<IPaymentTerminal>(sp => sp.GetRequiredService<MockPaymentTerminal>());
        return services;
    }

    /// <summary>
    /// Registers a <see cref="MockPrinter"/> as both <see cref="IPrinter"/> and its concrete type.
    /// </summary>
    /// <param name="services">The service collection to add registrations to.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for fluent chaining.</returns>
    public static IServiceCollection AddMockPrinter(this IServiceCollection services)
    {
        services.AddSingleton(_ => new MockPrinter(
            new DeviceDescriptor("mock-printer-001", DeviceType.Printer, "MockPrinter", "mock")));
        services.AddSingleton<IPrinter>(sp => sp.GetRequiredService<MockPrinter>());
        return services;
    }

    /// <summary>
    /// Registers a real <see cref="SerialBarcodeScanner"/> behind <see cref="IBarcodeScanner"/>.
    /// The optional delegates configure the serial transport and scanner options at registration
    /// time; no COM port is opened until the device is connected.
    /// </summary>
    /// <param name="services">The service collection to add registrations to.</param>
    /// <param name="configureTransport">Optional serial transport configuration (e.g. <c>t => t.PortName = "COM3"</c>).</param>
    /// <param name="configureScanner">Optional scanner-specific configuration.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for fluent chaining.</returns>
    public static IServiceCollection AddSerialScanner(
        this IServiceCollection services,
        Action<SerialTransportOptions>? configureTransport = null,
        Action<SerialScannerOptions>? configureScanner = null)
    {
        var transportOptions = new SerialTransportOptions();
        configureTransport?.Invoke(transportOptions);
        var scannerOptions = new SerialScannerOptions();
        configureScanner?.Invoke(scannerOptions);

        services.AddSingleton<IBarcodeScanner>(_ => new SerialBarcodeScanner(
            new DeviceDescriptor("serial-scanner-001", DeviceType.Scanner, "SerialBarcodeScanner", "serial"),
            transportOptions,
            scannerOptions));
        return services;
    }

    /// <summary>
    /// Registers a real <see cref="SerialScale"/> behind <see cref="IWeightScale"/>.
    /// The optional delegates configure the serial transport and scale options at registration
    /// time; no COM port is opened until the device is connected.
    /// </summary>
    /// <param name="services">The service collection to add registrations to.</param>
    /// <param name="configureTransport">Optional serial transport configuration (e.g. <c>t => t.PortName = "COM4"</c>).</param>
    /// <param name="configureScale">Optional scale-specific configuration.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for fluent chaining.</returns>
    public static IServiceCollection AddSerialScale(
        this IServiceCollection services,
        Action<SerialTransportOptions>? configureTransport = null,
        Action<SerialScaleOptions>? configureScale = null)
    {
        var transportOptions = new SerialTransportOptions();
        configureTransport?.Invoke(transportOptions);
        var scaleOptions = new SerialScaleOptions();
        configureScale?.Invoke(scaleOptions);

        services.AddSingleton<IWeightScale>(_ => new SerialScale(
            new DeviceDescriptor("serial-scale-001", DeviceType.Scale, "SerialScale", "serial"),
            transportOptions,
            scaleOptions));
        return services;
    }

    /// <summary>
    /// Registers a real ESC/POS <see cref="EscPosPrinter"/> behind <see cref="IPrinter"/>.
    /// The optional delegates configure the serial transport and printer options at registration
    /// time; no COM port is opened until the device is connected.
    /// </summary>
    /// <param name="services">The service collection to add registrations to.</param>
    /// <param name="configureTransport">Optional serial transport configuration (e.g. <c>t => t.PortName = "COM5"</c>).</param>
    /// <param name="configurePrinter">Optional printer-specific configuration.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for fluent chaining.</returns>
    public static IServiceCollection AddEscPosPrinter(
        this IServiceCollection services,
        Action<SerialTransportOptions>? configureTransport = null,
        Action<SerialPrinterOptions>? configurePrinter = null)
    {
        var transportOptions = new SerialTransportOptions();
        configureTransport?.Invoke(transportOptions);
        var printerOptions = new SerialPrinterOptions();
        configurePrinter?.Invoke(printerOptions);

        services.AddSingleton<IPrinter>(_ => new EscPosPrinter(
            new DeviceDescriptor("serial-printer-001", DeviceType.Printer, "EscPosPrinter", "serial"),
            transportOptions,
            printerOptions));
        return services;
    }

    /// <summary>
    /// Registers the <see cref="EscPosReceiptRenderer"/> as the <see cref="IReceiptRenderer"/>.
    /// When multiple renderer methods are called, the last registration wins.
    /// </summary>
    /// <param name="services">The service collection to add registrations to.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for fluent chaining.</returns>
    public static IServiceCollection AddEscPosReceiptRenderer(this IServiceCollection services)
    {
        services.AddSingleton<IReceiptRenderer, EscPosReceiptRenderer>();
        return services;
    }

    /// <summary>
    /// Registers the <see cref="PdfReceiptRenderer"/> as the <see cref="IReceiptRenderer"/>.
    /// When multiple renderer methods are called, the last registration wins.
    /// </summary>
    /// <param name="services">The service collection to add registrations to.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for fluent chaining.</returns>
    public static IServiceCollection AddPdfReceiptRenderer(this IServiceCollection services)
    {
        services.AddSingleton<IReceiptRenderer, PdfReceiptRenderer>();
        return services;
    }

    /// <summary>
    /// Registers the <see cref="SkiaReceiptRenderer"/> as the <see cref="IReceiptRenderer"/>.
    /// Unlike <see cref="AddPdfReceiptRenderer"/>, this renderer uses SkiaSharp and works on
    /// Android (QuestPDF's native Skia binary is not compiled for Android).
    /// When multiple renderer methods are called, the last registration wins.
    /// </summary>
    /// <param name="services">The service collection to add registrations to.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for fluent chaining.</returns>
    public static IServiceCollection AddSkiaReceiptRenderer(this IServiceCollection services)
    {
        services.AddSingleton<IReceiptRenderer, SkiaReceiptRenderer>();
        return services;
    }
}
