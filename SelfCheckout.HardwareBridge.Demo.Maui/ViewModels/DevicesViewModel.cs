using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SelfCheckout.HardwareBridge.Abstractions.Enums;
using SelfCheckout.HardwareBridge.Abstractions.Exceptions;
using SelfCheckout.HardwareBridge.Abstractions.Interfaces;
using SelfCheckout.HardwareBridge.Abstractions.Models;
using SelfCheckout.HardwareBridge.Demo.Maui.Views;
using SelfCheckout.HardwareBridge.Devices.Mocks;

namespace SelfCheckout.HardwareBridge.Demo.Maui.ViewModels;

/// <summary>
/// Devices tab VM: owns device status, connect-all flow, the shared event log, and the modal
/// settings / camera-scan entry points. Extracted from the original single-page MainViewModel
/// so it can be shared across tabs (CheckoutViewModel appends entries via <see cref="AddLogEntry"/>).
/// </summary>
public partial class DevicesViewModel : ObservableObject
{
    private const int MaxLog = 50;

    private readonly IBarcodeScanner _scanner;
    private readonly IWeightScale _scale;
    private readonly IPaymentTerminal _terminal;
    private readonly IPrinter _printer;
    private readonly IDeviceManager _manager;
    private readonly MockScanner _mockScanner;

    public DevicesViewModel(
        IBarcodeScanner scanner,
        IWeightScale scale,
        IPaymentTerminal terminal,
        IPrinter printer,
        IDeviceManager manager,
        MockScanner mockScanner)
    {
        _scanner = scanner;
        _scale = scale;
        _terminal = terminal;
        _printer = printer;
        _manager = manager;
        _mockScanner = mockScanner;

        scannerState = _scanner.DeviceStatus;
        scaleState = _scale.DeviceStatus;
        terminalState = _terminal.DeviceStatus;
        printerState = _printer.DeviceStatus;

        _manager.RegisterDevice(_scanner);
        _manager.RegisterDevice(_scale);
        _manager.RegisterDevice(_terminal);
        _manager.RegisterDevice(_printer);
    }

    public ObservableCollection<LogEntry> Log { get; } = new();

    [ObservableProperty] private DeviceState scannerState;
    [ObservableProperty] private DeviceState scaleState;
    [ObservableProperty] private DeviceState terminalState;
    [ObservableProperty] private DeviceState printerState;

    public void Subscribe()
    {
        Unsubscribe();
        _scanner.StateChanged       += OnScannerStateChanged;
        _scanner.BarcodeScanned     += OnBarcodeScanned;
        _scale.StateChanged         += OnScaleStateChanged;
        _terminal.StateChanged      += OnTerminalStateChanged;
        _printer.StateChanged       += OnPrinterStateChanged;
        _manager.DeviceConnected    += OnDeviceConnected;
        _manager.DeviceDisconnected += OnDeviceDisconnected;
    }

    public void Unsubscribe()
    {
        _scanner.StateChanged       -= OnScannerStateChanged;
        _scanner.BarcodeScanned     -= OnBarcodeScanned;
        _scale.StateChanged         -= OnScaleStateChanged;
        _terminal.StateChanged      -= OnTerminalStateChanged;
        _printer.StateChanged       -= OnPrinterStateChanged;
        _manager.DeviceConnected    -= OnDeviceConnected;
        _manager.DeviceDisconnected -= OnDeviceDisconnected;
    }

    private void OnScannerStateChanged(object? sender, DeviceState newState)
        => MainThread.BeginInvokeOnMainThread(() =>
        {
            ScannerState = newState;
            AppendLog(LogLevel.Info, $"Scanner -> {newState}");
        });

    private void OnScaleStateChanged(object? sender, DeviceState newState)
        => MainThread.BeginInvokeOnMainThread(() =>
        {
            ScaleState = newState;
            AppendLog(LogLevel.Info, $"Scale -> {newState}");
        });

    private void OnTerminalStateChanged(object? sender, DeviceState newState)
        => MainThread.BeginInvokeOnMainThread(() =>
        {
            TerminalState = newState;
            AppendLog(LogLevel.Info, $"Terminal -> {newState}");
        });

    private void OnPrinterStateChanged(object? sender, DeviceState newState)
        => MainThread.BeginInvokeOnMainThread(() =>
        {
            PrinterState = newState;
            AppendLog(LogLevel.Info, $"Printer -> {newState}");
        });

    private void OnBarcodeScanned(object? sender, ScanResult r)
        => MainThread.BeginInvokeOnMainThread(() =>
            AppendLog(LogLevel.Info, $"BarcodeScanned event: {r.Barcode} ({r.Symbology})"));

    private void OnDeviceConnected(object? sender, IDevice d)
        => MainThread.BeginInvokeOnMainThread(() =>
            AppendLog(LogLevel.Info, $"DeviceConnected: {d.Descriptor.Model}"));

    private void OnDeviceDisconnected(object? sender, IDevice d)
        => MainThread.BeginInvokeOnMainThread(() =>
        {
            AppendLog(LogLevel.Error, $"DeviceDisconnected: {d.Descriptor.Model}");
            _ = ShowSnackbarAsync($"{d.Descriptor.Model} disconnected");
        });

    [RelayCommand]
    private async Task ConnectAllAsync()
    {
        try { await _scanner.ConnectAsync(); } catch (DeviceException ex) { AppendLog(LogLevel.Error, $"Scanner connect failed: {ex.Message}"); }
        try { await _scale.ConnectAsync(); }   catch (DeviceException ex) { AppendLog(LogLevel.Error, $"Scale connect failed: {ex.Message}"); }
        try { await _terminal.ConnectAsync(); } catch (DeviceException ex) { AppendLog(LogLevel.Error, $"Terminal connect failed: {ex.Message}"); }
        try { await _printer.ConnectAsync(); }  catch (DeviceException ex) { AppendLog(LogLevel.Error, $"Printer connect failed: {ex.Message}"); }
    }

    [RelayCommand]
    private async Task OpenSettingsAsync()
    {
        var page = Application.Current!.Handler.MauiContext!.Services.GetRequiredService<SettingsPage>();
        await Shell.Current.Navigation.PushModalAsync(page);
    }

    [RelayCommand]
    private async Task OpenCameraScanAsync()
    {
        var services = Application.Current!.Handler.MauiContext!.Services;
        var page = services.GetRequiredService<CameraScanPage>();
        var vm = (CameraScanViewModel)page.BindingContext;

        await Application.Current.Windows[0].Page!.Navigation.PushModalAsync(page);

        var result = await vm.ResultTask;

        if (result is { } r)
        {
            _mockScanner.SimulateScan(r.Barcode, r.Symbology);
        }
    }

    /// <summary>
    /// Append a log entry. Marshals to the main thread if called from a background thread.
    /// Exposed to sibling VMs (e.g. <see cref="CheckoutViewModel"/>) so a single shared
    /// event log is rendered across tabs.
    /// </summary>
    internal void AddLogEntry(LogLevel level, string text) => AppendLog(level, text);

    private void AppendLog(LogLevel level, string text)
    {
        if (MainThread.IsMainThread)
        {
            InsertEntry(level, text);
        }
        else
        {
            MainThread.BeginInvokeOnMainThread(() => InsertEntry(level, text));
        }
    }

    private void InsertEntry(LogLevel level, string text)
    {
        Log.Insert(0, new LogEntry(DateTimeOffset.Now, level, text));
        while (Log.Count > MaxLog) Log.RemoveAt(Log.Count - 1);
    }

    internal static async Task ShowSnackbarAsync(string message)
    {
        var snackbar = Snackbar.Make(
            message,
            action: null,
            actionButtonText: "OK",
            duration: TimeSpan.FromSeconds(3));
        await snackbar.Show();
    }
}
