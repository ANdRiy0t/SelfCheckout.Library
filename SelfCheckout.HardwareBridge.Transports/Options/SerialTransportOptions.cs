using System.IO.Ports;

namespace SelfCheckout.HardwareBridge.Transports.Options;

/// <summary>
/// Configuration options for <see cref="Serial.SerialTransport"/> describing how to open the underlying
/// <see cref="SerialPort"/>. Uses plain C# defaults rather than <c>IOptions&lt;T&gt;</c> so consumers
/// can construct, mutate, and pass instances directly without DI ceremony.
/// </summary>
public class SerialTransportOptions
{
    /// <summary>The serial/COM port name to open (for example "COM1" on Windows or "/dev/ttyUSB0" on Linux). Defaults to "COM1".</summary>
    public string PortName { get; set; } = "COM1";

    /// <summary>The line speed in bits per second. Defaults to 9600 — the most common default for RS-232 retail peripherals.</summary>
    public int BaudRate { get; set; } = 9600;

    /// <summary>The parity-checking protocol for the connection. Defaults to <see cref="Parity.None"/>.</summary>
    public Parity Parity { get; set; } = Parity.None;

    /// <summary>The number of data bits per byte transmitted. Defaults to 8 — the standard for 8N1 framing.</summary>
    public int DataBits { get; set; } = 8;

    /// <summary>The number of stop bits per byte transmitted. Defaults to <see cref="StopBits.One"/>.</summary>
    public StopBits StopBits { get; set; } = StopBits.One;

    /// <summary>The hardware/software flow-control protocol. Defaults to <see cref="Handshake.None"/>.</summary>
    public Handshake Handshake { get; set; } = Handshake.None;

    /// <summary>Per-read timeout applied to the underlying <see cref="SerialPort.ReadTimeout"/>. Defaults to 500 ms.</summary>
    public TimeSpan ReadTimeout { get; set; } = TimeSpan.FromMilliseconds(500);

    /// <summary>Per-write timeout applied to the underlying <see cref="SerialPort.WriteTimeout"/>. Defaults to 500 ms.</summary>
    public TimeSpan WriteTimeout { get; set; } = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// Buffer size (in bytes) used by the async read pump when renting from <see cref="System.Buffers.ArrayPool{T}"/>
    /// and when sizing the underlying <see cref="SerialPort.ReadBufferSize"/>. Defaults to 4096.
    /// </summary>
    public int ReadBufferSize { get; set; } = 4096;
}
