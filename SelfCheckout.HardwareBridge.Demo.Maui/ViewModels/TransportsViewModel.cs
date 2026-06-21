using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SelfCheckout.HardwareBridge.Transports.Mocks;
using SelfCheckout.HardwareBridge.Transports.Options;

namespace SelfCheckout.HardwareBridge.Demo.Maui.ViewModels;

/// <summary>
/// Transports tab VM: exercises <see cref="MockTransport"/> directly (Open/Close/Write/EnqueueRead)
/// and displays read-only <see cref="SerialTransportOptions"/> information for the SerialTransport
/// section. A background ReadAllAsync pump streams enqueued bytes into <see cref="ReadChunks"/>.
/// </summary>
public partial class TransportsViewModel : ObservableObject
{
    private const int MaxChunks = 50;

    private readonly MockTransport _mockTransport;
    private readonly SerialTransportOptions _serialOptions;
    private readonly CancellationTokenSource _pumpCts = new();

    public TransportsViewModel(MockTransport mockTransport, SerialTransportOptions serialOptions)
    {
        _mockTransport = mockTransport;
        _serialOptions = serialOptions;

        isOpen = mockTransport.IsOpen;
        openCount = mockTransport.OpenCount;
        closeCount = mockTransport.CloseCount;
        SerialOptionsSummary = BuildSerialSummary(serialOptions);

        // Fire-and-forget background pump: reads from ITransport.ReadAllAsync and surfaces
        // each chunk into the UI-bound ReadChunks collection.
        _ = Task.Run(() => PumpReadsAsync(_pumpCts.Token));
    }

    [ObservableProperty] private bool isOpen;
    [ObservableProperty] private int openCount;
    [ObservableProperty] private int closeCount;
    [ObservableProperty] private string writeInputHex = "48 65 6C 6C 6F";
    [ObservableProperty] private string enqueueInputHex = "41 43 4B";

    public ObservableCollection<string> WrittenChunks { get; } = new();
    public ObservableCollection<string> ReadChunks { get; } = new();

    public string SerialOptionsSummary { get; }

    [RelayCommand]
    private async Task OpenTransportAsync()
    {
        try
        {
            await _mockTransport.OpenAsync();
            RefreshStateFromTransport();
        }
        catch (InvalidOperationException ex)
        {
            await ShowSnackbarAsync(ex.Message);
        }
        catch (Exception ex)
        {
            await ShowSnackbarAsync($"Open failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task CloseTransportAsync()
    {
        try
        {
            await _mockTransport.CloseAsync();
            RefreshStateFromTransport();
        }
        catch (InvalidOperationException ex)
        {
            await ShowSnackbarAsync(ex.Message);
        }
        catch (Exception ex)
        {
            await ShowSnackbarAsync($"Close failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task WriteTransportAsync()
    {
        if (!TryParseHex(WriteInputHex, out var bytes))
        {
            await ShowSnackbarAsync("Invalid hex");
            return;
        }

        try
        {
            await _mockTransport.WriteAsync(bytes);
            RefreshStateFromTransport();
        }
        catch (InvalidOperationException ex)
        {
            await ShowSnackbarAsync(ex.Message);
        }
        catch (Exception ex)
        {
            await ShowSnackbarAsync($"Write failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task EnqueueReadAsync()
    {
        if (!TryParseHex(EnqueueInputHex, out var bytes))
        {
            await ShowSnackbarAsync("Invalid hex");
            return;
        }

        try
        {
            await _mockTransport.EnqueueReadAsync(bytes);
        }
        catch (Exception ex)
        {
            await ShowSnackbarAsync($"Enqueue failed: {ex.Message}");
        }
    }

    private async Task PumpReadsAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var chunk in _mockTransport.ReadAllAsync(ct).ConfigureAwait(false))
            {
                var hex = ToHex(chunk.Span);
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    ReadChunks.Insert(0, hex);
                    while (ReadChunks.Count > MaxChunks) ReadChunks.RemoveAt(ReadChunks.Count - 1);
                });
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception)
        {
            // Pump failure is swallowed — the channel may be completed on disposal.
        }
    }

    private void RefreshStateFromTransport()
    {
        if (MainThread.IsMainThread)
        {
            IsOpen = _mockTransport.IsOpen;
            OpenCount = _mockTransport.OpenCount;
            CloseCount = _mockTransport.CloseCount;
            RebuildWrittenChunks();
        }
        else
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                IsOpen = _mockTransport.IsOpen;
                OpenCount = _mockTransport.OpenCount;
                CloseCount = _mockTransport.CloseCount;
                RebuildWrittenChunks();
            });
        }
    }

    private void RebuildWrittenChunks()
    {
        WrittenChunks.Clear();
        // Newest first to match ReadChunks ordering.
        foreach (var chunk in _mockTransport.WrittenChunks.AsEnumerable().Reverse())
        {
            WrittenChunks.Add(ToHex(chunk));
            if (WrittenChunks.Count >= MaxChunks) break;
        }
    }

    private static bool TryParseHex(string input, out byte[] bytes)
    {
        bytes = Array.Empty<byte>();
        if (string.IsNullOrWhiteSpace(input)) return false;

        try
        {
            var tokens = input.Split(new[] { ' ', '\t', '\r', '\n', ',' }, StringSplitOptions.RemoveEmptyEntries);
            var result = new byte[tokens.Length];
            for (int i = 0; i < tokens.Length; i++)
            {
                result[i] = Convert.ToByte(tokens[i], 16);
            }

            bytes = result;
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (OverflowException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static string ToHex(ReadOnlySpan<byte> bytes)
    {
        if (bytes.IsEmpty) return "(empty)";
        var sb = new StringBuilder(bytes.Length * 3);
        for (int i = 0; i < bytes.Length; i++)
        {
            if (i > 0) sb.Append(' ');
            sb.Append(bytes[i].ToString("X2", CultureInfo.InvariantCulture));
        }

        return sb.ToString();
    }

    private static string ToHex(byte[] bytes) => ToHex(bytes.AsSpan());

    private static string BuildSerialSummary(SerialTransportOptions o)
    {
        // Format: COM1 @ 9600 8N1 RT=500ms WT=500ms BUF=4096 HS=None
        var parity = o.Parity.ToString().Substring(0, 1).ToUpperInvariant();
        var stopBits = o.StopBits switch
        {
            System.IO.Ports.StopBits.One => "1",
            System.IO.Ports.StopBits.OnePointFive => "1.5",
            System.IO.Ports.StopBits.Two => "2",
            _ => "?"
        };
        return $"{o.PortName} @ {o.BaudRate} {o.DataBits}{parity}{stopBits}  RT={o.ReadTimeout.TotalMilliseconds:0}ms  WT={o.WriteTimeout.TotalMilliseconds:0}ms  BUF={o.ReadBufferSize}  HS={o.Handshake}";
    }

    private static async Task ShowSnackbarAsync(string message)
    {
        var snackbar = Snackbar.Make(
            message,
            action: null,
            actionButtonText: "OK",
            duration: TimeSpan.FromSeconds(3));
        await snackbar.Show();
    }
}
