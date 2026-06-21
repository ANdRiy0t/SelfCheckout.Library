using System.Text;

namespace SelfCheckout.HardwareBridge.Devices.Real.Options;

/// <summary>
/// Configuration for <see cref="SerialBarcodeScanner"/>. Mirrors the plain-C#-defaults style of the
/// Mock options classes (mutable properties, no <c>IOptions&lt;T&gt;</c>) so consumers can construct
/// and pass instances directly. Most RS-232 barcode scanners emit a bare CR/LF-terminated string with
/// no symbology metadata, so the symbology is configured here rather than parsed from the wire.
/// </summary>
public class SerialScannerOptions
{
    /// <summary>
    /// Byte values (as characters) that terminate a barcode line. Defaults to CR and LF, covering
    /// the vast majority of RS-232 scanners. Configurable even though the default is CR/LF.
    /// </summary>
    public char[] LineTerminators { get; set; } = ['\r', '\n'];

    /// <summary>
    /// Symbology stamped onto every <c>ScanResult</c>. Raw serial framing carries no symbology
    /// metadata, so this is a static label. Defaults to "UNKNOWN".
    /// </summary>
    public string DefaultSymbology { get; set; } = "UNKNOWN";

    /// <summary>
    /// Text encoding used to decode the accumulated barcode bytes. Defaults to UTF-8.
    /// </summary>
    public Encoding Encoding { get; set; } = Encoding.UTF8;
}
