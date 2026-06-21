using System.Text;

namespace SelfCheckout.HardwareBridge.Devices.Real.Options;

/// <summary>
/// Configuration for <see cref="SerialScale"/>. The scale auto-detects between command mode
/// (request/response, e.g. Massa-K / A&amp;D) and continuous mode (free-running stream, e.g. CAS /
/// budget scales) on connect, then parses weight lines with a configurable regex.
/// </summary>
public class SerialScaleOptions
{
    /// <summary>
    /// Command sent to request a weight reading in command mode (and during auto-detect).
    /// Defaults to "W\r".
    /// </summary>
    public string WeightCommand { get; set; } = "W\r";

    /// <summary>
    /// How long to wait for a command-mode reply on connect before falling back to continuous mode.
    /// Also used as the per-read wait when polling for a fresh reading in command mode.
    /// Defaults to 500 ms.
    /// </summary>
    public TimeSpan AutoDetectTimeout { get; set; } = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// Regex used to extract a weight from a decoded line. The full match is parsed as the decimal
    /// value; capture group 1 supplies the unit ("kg" or "g"). Defaults to
    /// <c>[-+]?\d+\.?\d*\s*(kg|g)</c>.
    /// </summary>
    public string WeightPattern { get; set; } = @"[-+]?\d+\.?\d*\s*(kg|g)";

    /// <summary>
    /// Unit used when the regex matches a numeric value but no unit group. Defaults to "g".
    /// </summary>
    public string DefaultUnit { get; set; } = "g";

    /// <summary>
    /// Text encoding used to decode weight lines and encode the weight command. Defaults to ASCII,
    /// which suits the digit/letter payloads typical of scale protocols.
    /// </summary>
    public Encoding Encoding { get; set; } = Encoding.ASCII;
}
