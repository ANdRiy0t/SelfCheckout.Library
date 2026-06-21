using SelfCheckout.HardwareBridge.Demo.Maui.ViewModels;

namespace SelfCheckout.HardwareBridge.Demo.Maui.Views;

public partial class DevicesPage : ContentPage
{
    private readonly DevicesViewModel _vm;

    public DevicesPage(DevicesViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.Subscribe();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.Unsubscribe();
    }
}
