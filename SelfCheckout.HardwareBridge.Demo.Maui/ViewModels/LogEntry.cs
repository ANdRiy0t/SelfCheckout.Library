namespace SelfCheckout.HardwareBridge.Demo.Maui.ViewModels;

public enum LogLevel { Info, Error }

public sealed record LogEntry(DateTimeOffset Timestamp, LogLevel Level, string Text)
{
    public string Display => $"[{Timestamp:HH:mm:ss}] [{Level.ToString().ToUpper()}] {Text}";
}
