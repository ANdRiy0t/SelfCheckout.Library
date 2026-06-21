using Microsoft.Extensions.DependencyInjection;
using SelfCheckout.HardwareBridge.Abstractions.Enums;
using SelfCheckout.HardwareBridge.Abstractions.Interfaces;
using SelfCheckout.HardwareBridge.Core;
using SelfCheckout.HardwareBridge.Devices.Mocks;
using SelfCheckout.HardwareBridge.Devices.Real;
using SelfCheckout.HardwareBridge.Devices.Receipts;

namespace SelfCheckout.HardwareBridge.Tests.DependencyInjection;

public class ServiceCollectionExtensionsTests
{
    private ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddSelfCheckoutDevices().AddMockDevices();
        return services.BuildServiceProvider();
    }

    [Fact]
    public void AddSelfCheckoutDevices_RegistersIBarcodeScanner()
    {
        using var provider = BuildProvider();
        var scanner = provider.GetService<IBarcodeScanner>();

        Assert.NotNull(scanner);
        Assert.IsType<MockScanner>(scanner);
    }

    [Fact]
    public void AddSelfCheckoutDevices_RegistersIWeightScale()
    {
        using var provider = BuildProvider();
        var scale = provider.GetService<IWeightScale>();

        Assert.NotNull(scale);
        Assert.IsType<MockScale>(scale);
    }

    [Fact]
    public void AddSelfCheckoutDevices_RegistersIPaymentTerminal()
    {
        using var provider = BuildProvider();
        var terminal = provider.GetService<IPaymentTerminal>();

        Assert.NotNull(terminal);
        Assert.IsType<MockPaymentTerminal>(terminal);
    }

    [Fact]
    public void AddSelfCheckoutDevices_RegistersIDeviceManager()
    {
        using var provider = BuildProvider();
        var manager = provider.GetService<IDeviceManager>();

        Assert.NotNull(manager);
        Assert.IsType<DeviceManager>(manager);
    }

    [Fact]
    public void AddSelfCheckoutDevices_ReturnsServiceCollectionForFluentChaining()
    {
        var services = new ServiceCollection();
        var result = services.AddSelfCheckoutDevices();

        Assert.Same(services, result);
    }

    [Fact]
    public void AddSelfCheckoutDevices_DoesNotRegisterMocks()
    {
        var services = new ServiceCollection();
        services.AddSelfCheckoutDevices();
        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<IDeviceManager>());
        Assert.Null(provider.GetService<IBarcodeScanner>());
        Assert.Null(provider.GetService<IWeightScale>());
        Assert.Null(provider.GetService<IPaymentTerminal>());
    }

    [Fact]
    public void AddSelfCheckoutDevices_RegistersAsSingletons()
    {
        using var provider = BuildProvider();

        var scanner1 = provider.GetService<IBarcodeScanner>();
        var scanner2 = provider.GetService<IBarcodeScanner>();
        Assert.Same(scanner1, scanner2);

        var scale1 = provider.GetService<IWeightScale>();
        var scale2 = provider.GetService<IWeightScale>();
        Assert.Same(scale1, scale2);

        var terminal1 = provider.GetService<IPaymentTerminal>();
        var terminal2 = provider.GetService<IPaymentTerminal>();
        Assert.Same(terminal1, terminal2);

        var manager1 = provider.GetService<IDeviceManager>();
        var manager2 = provider.GetService<IDeviceManager>();
        Assert.Same(manager1, manager2);
    }

    [Fact]
    public void AddSelfCheckoutDevices_MockDevicesHaveCorrectDeviceType()
    {
        using var provider = BuildProvider();

        var scanner = provider.GetRequiredService<IBarcodeScanner>();
        Assert.Equal(DeviceType.Scanner, scanner.Descriptor.Type);

        var scale = provider.GetRequiredService<IWeightScale>();
        Assert.Equal(DeviceType.Scale, scale.Descriptor.Type);

        var terminal = provider.GetRequiredService<IPaymentTerminal>();
        Assert.Equal(DeviceType.PaymentTerminal, terminal.Descriptor.Type);
    }

    [Fact]
    public void AddMockDevices_RegistersAllFour()
    {
        var services = new ServiceCollection();
        services.AddSelfCheckoutDevices().AddMockDevices();
        using var provider = services.BuildServiceProvider();

        Assert.IsType<MockScanner>(provider.GetService<IBarcodeScanner>());
        Assert.IsType<MockScale>(provider.GetService<IWeightScale>());
        Assert.IsType<MockPaymentTerminal>(provider.GetService<IPaymentTerminal>());
        Assert.IsType<MockPrinter>(provider.GetService<IPrinter>());
    }

    [Fact]
    public void AddMockDevices_RegistersConcreteTypes()
    {
        var services = new ServiceCollection();
        services.AddSelfCheckoutDevices().AddMockDevices();
        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<MockScanner>());
        Assert.NotNull(provider.GetService<MockPrinter>());
    }

    [Fact]
    public void AddSerialScanner_RegistersInterface()
    {
        var services = new ServiceCollection();
        services.AddSelfCheckoutDevices().AddSerialScanner();
        using var provider = services.BuildServiceProvider();

        Assert.IsType<SerialBarcodeScanner>(provider.GetRequiredService<IBarcodeScanner>());
    }

    [Fact]
    public void AddSerialScale_RegistersInterface()
    {
        var services = new ServiceCollection();
        services.AddSelfCheckoutDevices().AddSerialScale();
        using var provider = services.BuildServiceProvider();

        Assert.IsType<SerialScale>(provider.GetRequiredService<IWeightScale>());
    }

    [Fact]
    public void AddEscPosPrinter_RegistersInterface()
    {
        var services = new ServiceCollection();
        services.AddSelfCheckoutDevices().AddEscPosPrinter();
        using var provider = services.BuildServiceProvider();

        Assert.IsType<EscPosPrinter>(provider.GetRequiredService<IPrinter>());
    }

    [Fact]
    public void AddSerialScanner_AppliesConfigureDelegate()
    {
        var services = new ServiceCollection();
        services.AddSelfCheckoutDevices().AddSerialScanner(t => t.PortName = "COM7");
        using var provider = services.BuildServiceProvider();

        Assert.IsType<SerialBarcodeScanner>(provider.GetRequiredService<IBarcodeScanner>());
    }

    [Fact]
    public void AddEscPosReceiptRenderer_RegistersRenderer()
    {
        var services = new ServiceCollection();
        services.AddSelfCheckoutDevices().AddEscPosReceiptRenderer();
        using var provider = services.BuildServiceProvider();

        Assert.IsType<EscPosReceiptRenderer>(provider.GetRequiredService<IReceiptRenderer>());
    }

    [Fact]
    public void AddPdfReceiptRenderer_RegistersRenderer()
    {
        var services = new ServiceCollection();
        services.AddSelfCheckoutDevices().AddPdfReceiptRenderer();
        using var provider = services.BuildServiceProvider();

        Assert.IsType<PdfReceiptRenderer>(provider.GetRequiredService<IReceiptRenderer>());
    }

    [Fact]
    public void ReceiptRenderer_LastCallWins()
    {
        var services = new ServiceCollection();
        services.AddSelfCheckoutDevices().AddEscPosReceiptRenderer().AddPdfReceiptRenderer();
        using var provider = services.BuildServiceProvider();

        Assert.IsType<PdfReceiptRenderer>(provider.GetRequiredService<IReceiptRenderer>());
    }
}
