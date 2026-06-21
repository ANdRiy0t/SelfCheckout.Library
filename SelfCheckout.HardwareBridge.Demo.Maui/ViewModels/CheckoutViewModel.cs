using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Storage;
using SelfCheckout.HardwareBridge.Abstractions.Enums;
using SelfCheckout.HardwareBridge.Abstractions.Exceptions;
using SelfCheckout.HardwareBridge.Abstractions.Interfaces;
using SelfCheckout.HardwareBridge.Abstractions.Models;
using SelfCheckout.HardwareBridge.Devices.Receipts;

namespace SelfCheckout.HardwareBridge.Demo.Maui.ViewModels;

/// <summary>
/// Checkout tab VM: real-flow POS cart driven by the four device interfaces. Logs through the
/// shared <see cref="DevicesViewModel"/> event log instead of owning a second one.
/// </summary>
public partial class CheckoutViewModel : ObservableObject
{
    private readonly IBarcodeScanner _scanner;
    private readonly IWeightScale _scale;
    private readonly IPaymentTerminal _terminal;
    private readonly IPrinter _printer;
    private readonly IReceiptRenderer _renderer;
    private readonly DevicesViewModel _devicesVm;

    public CheckoutViewModel(
        IBarcodeScanner scanner,
        IWeightScale scale,
        IPaymentTerminal terminal,
        IPrinter printer,
        IReceiptRenderer renderer,
        DevicesViewModel devicesVm)
    {
        _scanner = scanner;
        _scale = scale;
        _terminal = terminal;
        _printer = printer;
        _renderer = renderer;
        _devicesVm = devicesVm;

        Cart.CollectionChanged += OnCartChanged;
    }

    public ObservableCollection<CartItem> Cart { get; } = new();

    [ObservableProperty] private string lastBarcode = string.Empty;
    [ObservableProperty] private decimal currentWeight;
    [ObservableProperty] private string weightUnit = "kg";
    [ObservableProperty] private decimal currentPrice = 9.99m;
    [ObservableProperty] private decimal total;
    [ObservableProperty] private bool isReceiptReady = false;

    // Snapshot of the receipt built from the cart at the moment of an approved payment,
    // captured BEFORE Cart.Clear() so the downloaded PDF reflects what was actually paid for.
    private Receipt? _lastReceipt;

    private void OnCartChanged(object? sender, NotifyCollectionChangedEventArgs e) => RecomputeTotal();

    private void RecomputeTotal()
    {
        decimal sum = 0m;
        foreach (var item in Cart)
        {
            sum += item.Price * item.Quantity;
        }

        Total = sum;
    }

    [RelayCommand]
    private async Task ScanAndAddAsync()
    {
        try
        {
            var r = await _scanner.ScanAsync();
            LastBarcode = r.Barcode;
            var item = new CartItem(r.Barcode, CurrentPrice, 1m, "ea");
            Cart.Add(item);
            _devicesVm.AddLogEntry(LogLevel.Info, $"Cart += {r.Barcode} @ {CurrentPrice:C} (total {Total:C})");
        }
        catch (DeviceException ex)
        {
            _devicesVm.AddLogEntry(LogLevel.Error, $"Scan failed: {ex.ErrorCode} - {ex.Message}");
            await ShowSnackbarAsync($"Scan failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task WeighAsync()
    {
        try
        {
            var w = await _scale.GetWeightAsync();
            CurrentWeight = w.Value;
            WeightUnit = w.Unit;
            _devicesVm.AddLogEntry(LogLevel.Info, $"Weight: {w.Value} {w.Unit} (stable={w.IsStable})");
        }
        catch (DeviceException ex)
        {
            _devicesVm.AddLogEntry(LogLevel.Error, $"Weigh failed: {ex.ErrorCode} - {ex.Message}");
            await ShowSnackbarAsync($"Weigh failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task PayAsync()
    {
        if (Total <= 0m)
        {
            await ShowSnackbarAsync("Cart is empty");
            return;
        }

        try
        {
            var p = await _terminal.ProcessPaymentAsync(Total, "USD");
            if (p.IsApproved)
            {
                // Snapshot the cart into a Receipt BEFORE clearing — the PDF must reflect
                // what was paid for. Total recomputes to 0 once the cart clears, but the
                // receipt is already built from the pre-clear cart, so amounts stay correct.
                var builder = new ReceiptBuilder()
                    .Store("SelfCheckout Demo", "123 Demo Street")
                    .Date(DateTimeOffset.Now)
                    .Payment(PaymentMethod.Card, Total)
                    .Footer("Thank you for shopping!");

                foreach (var item in Cart)
                    builder.AddItem(item.Barcode, item.Quantity, item.Price, item.Unit);

                _lastReceipt = builder.Build();
                IsReceiptReady = true;

                _devicesVm.AddLogEntry(LogLevel.Info, $"Payment APPROVED: {Total:C} ({Cart.Count} items)");
                Cart.Clear();
            }
            else
            {
                _devicesVm.AddLogEntry(LogLevel.Error, $"Payment DECLINED: {Total:C}");
                await ShowSnackbarAsync("Payment declined");
            }
        }
        catch (DeviceException ex)
        {
            _devicesVm.AddLogEntry(LogLevel.Error, $"Pay failed: {ex.ErrorCode} - {ex.Message}");
            await ShowSnackbarAsync($"Pay failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task PrintReceiptAsync()
    {
        if (Cart.Count == 0)
        {
            await ShowSnackbarAsync("Cart is empty");
            return;
        }

        try
        {
            var builder = new ReceiptBuilder()
                .Store("SelfCheckout Demo", "123 Demo Street")
                .Date(DateTimeOffset.Now)
                .Payment(PaymentMethod.Card, Total)
                .Footer("Thank you for shopping!");

            foreach (var item in Cart)
                builder.AddItem(item.Barcode, item.Quantity, item.Price, item.Unit);

            var receipt = builder.Build();
            await _printer.PrintReceiptAsync(receipt, _renderer);
            await _printer.CutAsync();
            _devicesVm.AddLogEntry(LogLevel.Info, $"Receipt printed ({receipt.Items.Count} items, total {receipt.Total:C}) and cut");
        }
        catch (DeviceException ex)
        {
            _devicesVm.AddLogEntry(LogLevel.Error, $"Print failed: {ex.ErrorCode} - {ex.Message}");
            await ShowSnackbarAsync($"Print failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task DownloadReceiptAsync()
    {
        if (_lastReceipt is null)
        {
            await ShowSnackbarAsync("No receipt available");
            return;
        }

        try
        {
            var bytes = await _renderer.RenderAsync(_lastReceipt);
            var filePath = Path.Combine(
                FileSystem.CacheDirectory,
                $"receipt_{DateTimeOffset.Now:yyyyMMdd_HHmmss}.pdf");
            await File.WriteAllBytesAsync(filePath, bytes);

            await Share.RequestAsync(new ShareFileRequest
            {
                Title = "Receipt",
                File = new ShareFile(filePath, "application/pdf"),
            });

            _devicesVm.AddLogEntry(LogLevel.Info, $"Receipt PDF generated ({bytes.Length} bytes) and shared");
        }
        catch (Exception ex)
        {
            _devicesVm.AddLogEntry(LogLevel.Error, $"Download receipt failed: {ex.Message}");
            await ShowSnackbarAsync($"Download failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private void RemoveItem(CartItem? item)
    {
        if (item is null) return;
        if (Cart.Remove(item))
        {
            _devicesVm.AddLogEntry(LogLevel.Info, $"Cart -= {item.Barcode} (total {Total:C})");
        }
    }

    [RelayCommand]
    private void ClearCart()
    {
        if (Cart.Count == 0) return;
        Cart.Clear();
        IsReceiptReady = false;
        _lastReceipt = null;
        _devicesVm.AddLogEntry(LogLevel.Info, "Cart cleared");
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

/// <summary>One line in the checkout cart.</summary>
public sealed record CartItem(string Barcode, decimal Price, decimal Quantity, string Unit);
