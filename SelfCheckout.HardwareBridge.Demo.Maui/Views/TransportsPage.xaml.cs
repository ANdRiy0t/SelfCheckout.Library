using SelfCheckout.HardwareBridge.Demo.Maui.ViewModels;

namespace SelfCheckout.HardwareBridge.Demo.Maui.Views;

public partial class TransportsPage : ContentPage
{
    public TransportsPage(TransportsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
