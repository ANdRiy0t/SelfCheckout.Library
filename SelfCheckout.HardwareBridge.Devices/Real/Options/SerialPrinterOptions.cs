using System.Text;

namespace SelfCheckout.HardwareBridge.Devices.Real.Options;

/// <summary>
/// Configuration for <see cref="EscPosPrinter"/>. Controls the text encoding used by the
/// <c>PrintTextAsync</c> helper and whether the printer is initialized (ESC @) on connect.
/// </summary>
public class SerialPrinterOptions
{
    /// <summary>
    /// Text encoding used by the <c>PrintTextAsync</c> helper to convert strings to bytes.
    /// Defaults to UTF-8.
    /// </summary>
    public Encoding Encoding { get; set; } = Encoding.UTF8;

    /// <summary>
    /// When true, sends the ESC/POS initialize command (ESC @, <c>0x1B 0x40</c>) on connect to
    /// reset the printer to a known state. Defaults to true.
    /// </summary>
    public bool InitOnConnect { get; set; } = true;
}
