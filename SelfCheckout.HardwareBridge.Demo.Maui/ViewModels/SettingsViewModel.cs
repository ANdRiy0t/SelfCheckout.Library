using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SelfCheckout.HardwareBridge.Devices.Mocks.Options;

namespace SelfCheckout.HardwareBridge.Demo.Maui.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly MockScannerOptions _scanner;
    private readonly MockScaleOptions _scale;
    private readonly MockPaymentTerminalOptions _terminal;
    private readonly MockPrinterOptions _printerOptions;

    public SettingsViewModel(
        MockScannerOptions scanner,
        MockScaleOptions scale,
        MockPaymentTerminalOptions terminal,
        MockPrinterOptions printer)
    {
        _scanner = scanner;
        _scale = scale;
        _terminal = terminal;
        _printerOptions = printer;

        scannerFailOnScan = scanner.ShouldFailOnScan;
        scannerSimulateDisconnect = scanner.SimulateDisconnectOnScan;
        scaleFailOnRead = scale.ShouldFailOnRead;
        terminalShouldDecline = terminal.ShouldDecline;
        terminalShouldFailOnProcess = terminal.ShouldFailOnProcess;
        terminalSimulateDisconnect = terminal.SimulateDisconnectOnProcess;
        printerShouldFail = printer.ShouldFailOnPrint;
        printerSimulateOutOfPaper = printer.SimulateOutOfPaper;
    }

    [ObservableProperty] private bool scannerFailOnScan;
    partial void OnScannerFailOnScanChanged(bool value) => _scanner.ShouldFailOnScan = value;

    [ObservableProperty] private bool scannerSimulateDisconnect;
    partial void OnScannerSimulateDisconnectChanged(bool value) => _scanner.SimulateDisconnectOnScan = value;

    [ObservableProperty] private bool scaleFailOnRead;
    partial void OnScaleFailOnReadChanged(bool value) => _scale.ShouldFailOnRead = value;

    [ObservableProperty] private bool terminalShouldDecline;
    partial void OnTerminalShouldDeclineChanged(bool value) => _terminal.ShouldDecline = value;

    [ObservableProperty] private bool terminalShouldFailOnProcess;
    partial void OnTerminalShouldFailOnProcessChanged(bool value) => _terminal.ShouldFailOnProcess = value;

    [ObservableProperty] private bool terminalSimulateDisconnect;
    partial void OnTerminalSimulateDisconnectChanged(bool value) => _terminal.SimulateDisconnectOnProcess = value;

    [ObservableProperty] private bool printerShouldFail;
    partial void OnPrinterShouldFailChanged(bool value) => _printerOptions.ShouldFailOnPrint = value;

    [ObservableProperty] private bool printerSimulateOutOfPaper;
    partial void OnPrinterSimulateOutOfPaperChanged(bool value) => _printerOptions.SimulateOutOfPaper = value;

    [RelayCommand]
    private static async Task CloseAsync()
        => await Shell.Current.Navigation.PopModalAsync();
}
