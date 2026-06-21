using SelfCheckout.HardwareBridge.Abstractions.Models;

namespace SelfCheckout.HardwareBridge.Abstractions.Interfaces;

public interface IBarcodeScanner : IDevice
{

    event EventHandler<ScanResult>? BarcodeScanned;

    Task<ScanResult> ScanAsync(CancellationToken cancellationToken = default);
}
