using SelfCheckout.HardwareBridge.Demo.Maui.ViewModels;
using ZXing.Net.Maui;

namespace SelfCheckout.HardwareBridge.Demo.Maui.Views;

public partial class CameraScanPage : ContentPage
{
    private readonly CameraScanViewModel _vm;

    public CameraScanPage(CameraScanViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;

        Reader.Options = new BarcodeReaderOptions
        {
            Formats   = BarcodeFormat.Ean13 | BarcodeFormat.Ean8
                       | BarcodeFormat.Code128 | BarcodeFormat.QrCode,
            AutoRotate = true,
            Multiple   = false,
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.Camera>();
        }

        if (status != PermissionStatus.Granted)
        {

            _vm.CancelScan();
            if (Navigation.ModalStack.Count > 0)
                await Navigation.PopModalAsync();
        }

    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        _vm.CancelScan();
    }

    private async void OnBarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
    {

        var first = e.Results?.FirstOrDefault();
        if (first is null) return;

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            if (!_vm.IsScanning) return;
            _vm.CompleteScan(first.Value, first.Format.ToString());
            if (Navigation.ModalStack.Count > 0)
                await Navigation.PopModalAsync();
        });
    }
}
