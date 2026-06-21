using System.IO.Ports;
using SelfCheckout.HardwareBridge.Transports.Options;

namespace SelfCheckout.HardwareBridge.Tests.Transports;

public class SerialTransportOptionsTests
{
    [Fact]
    public void Defaults_PortNameIsCOM1()
    {
        var options = new SerialTransportOptions();
        Assert.Equal("COM1", options.PortName);
    }

    [Fact]
    public void Defaults_BaudRateIs9600()
    {
        var options = new SerialTransportOptions();
        Assert.Equal(9600, options.BaudRate);
    }

    [Fact]
    public void Defaults_ParityIsNone()
    {
        var options = new SerialTransportOptions();
        Assert.Equal(Parity.None, options.Parity);
    }

    [Fact]
    public void Defaults_DataBitsIs8()
    {
        var options = new SerialTransportOptions();
        Assert.Equal(8, options.DataBits);
    }

    [Fact]
    public void Defaults_StopBitsIsOne()
    {
        var options = new SerialTransportOptions();
        Assert.Equal(StopBits.One, options.StopBits);
    }

    [Fact]
    public void Defaults_HandshakeIsNone()
    {
        var options = new SerialTransportOptions();
        Assert.Equal(Handshake.None, options.Handshake);
    }

    [Fact]
    public void Defaults_ReadAndWriteTimeoutsAre500ms()
    {
        var options = new SerialTransportOptions();
        Assert.Equal(TimeSpan.FromMilliseconds(500), options.ReadTimeout);
        Assert.Equal(TimeSpan.FromMilliseconds(500), options.WriteTimeout);
    }

    [Fact]
    public void Defaults_ReadBufferSizeIs4096()
    {
        var options = new SerialTransportOptions();
        Assert.Equal(4096, options.ReadBufferSize);
    }
}
