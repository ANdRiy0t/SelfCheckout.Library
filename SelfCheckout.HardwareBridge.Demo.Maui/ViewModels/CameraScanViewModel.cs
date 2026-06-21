using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SelfCheckout.HardwareBridge.Demo.Maui.ViewModels;

public partial class CameraScanViewModel : ObservableObject
{
    private readonly TaskCompletionSource<(string Barcode, string Symbology)?> _tcs =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    [ObservableProperty] private bool isScanning = true;

    public Task<(string Barcode, string Symbology)?> ResultTask => _tcs.Task;

    public void CompleteScan(string barcode, string symbology)
    {
        if (!_tcs.Task.IsCompleted)
        {
            IsScanning = false;
            _tcs.TrySetResult((barcode, symbology));
        }
    }

    public void CancelScan()
    {
        if (!_tcs.Task.IsCompleted)
        {
            IsScanning = false;
            _tcs.TrySetResult(null);
        }
    }

    [RelayCommand]
    private async Task CloseAsync()
    {
        CancelScan();
        if (Application.Current?.Windows.Count > 0
            && Application.Current.Windows[0].Page?.Navigation is INavigation nav
            && nav.ModalStack.Count > 0)
        {
            await nav.PopModalAsync();
        }
    }
}
