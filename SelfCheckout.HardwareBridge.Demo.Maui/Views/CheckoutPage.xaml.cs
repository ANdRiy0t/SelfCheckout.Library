using SelfCheckout.HardwareBridge.Demo.Maui.ViewModels;

namespace SelfCheckout.HardwareBridge.Demo.Maui.Views;

public partial class CheckoutPage : ContentPage
{
    public CheckoutPage(CheckoutViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
