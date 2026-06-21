using SelfCheckout.HardwareBridge.Demo.Maui.ViewModels;

namespace SelfCheckout.HardwareBridge.Demo.Maui.Views;

public partial class SettingsPage : ContentPage
{
    public SettingsPage(SettingsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
