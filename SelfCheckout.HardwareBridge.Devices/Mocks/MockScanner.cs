using SelfCheckout.HardwareBridge.Abstractions.Enums;
using SelfCheckout.HardwareBridge.Abstractions.Exceptions;
using SelfCheckout.HardwareBridge.Abstractions.Interfaces;
using SelfCheckout.HardwareBridge.Abstractions.Models;
using SelfCheckout.HardwareBridge.Core;
using SelfCheckout.HardwareBridge.Devices.Mocks.Options;

namespace SelfCheckout.HardwareBridge.Devices.Mocks;

public class MockScanner : DeviceBase, IBarcodeScanner
{
    private readonly MockScannerOptions _options;
    private readonly SemaphoreSlim _operationLock = new(1, 1);

    public event EventHandler<ScanResult>? BarcodeScanned;

    public MockScanner(DeviceDescriptor descriptor, MockScannerOptions? options = null)
        : base(descriptor)
    {
        _options = options ?? new MockScannerOptions();
    }

    protected override async Task ConnectCoreAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(_options.ConnectDelay, cancellationToken);
    }

    protected override Task DisconnectCoreAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async Task<ScanResult> ScanAsync(CancellationToken cancellationToken = default)
    {
        await _operationLock.WaitAsync(cancellationToken);
        try
        {
            if (DeviceStatus != DeviceState.Ready)
            {
                throw new DeviceException(
                    $"Scanner '{Descriptor.Id}' is not ready. Current state: {DeviceStatus}.",
                    Descriptor.Id,
                    Descriptor.Type,
                    ErrorCode.ConnectionFailed);
            }

            DeviceStatus = DeviceState.Busy;

            if (_options.SimulateDisconnectOnScan)
            {
                DeviceStatus = DeviceState.Disconnected;
                throw new DeviceException(
                    $"Scanner '{Descriptor.Id}' lost connection during scan.",
                    Descriptor.Id,
                    Descriptor.Type,
                    ErrorCode.ConnectionLost);
            }

            if (_options.ShouldFailOnScan)
            {
                DeviceStatus = DeviceState.Error;
                throw new DeviceException(
                    $"Scanner '{Descriptor.Id}' failed during scan.",
                    Descriptor.Id,
                    Descriptor.Type,
                    _options.FailureErrorCode);
            }

            await Task.Delay(_options.ScanDelay, cancellationToken);

            var result = new ScanResult(
                _options.DefaultBarcode,
                _options.DefaultSymbology,
                DateTimeOffset.UtcNow);

            DeviceStatus = DeviceState.Ready;
            BarcodeScanned?.Invoke(this, result);
            return result;
        }
        finally
        {
            _operationLock.Release();
        }
    }

    public void SimulateScan(string barcode, string symbology = "EAN13")
    {
        var result = new ScanResult(barcode, symbology, DateTimeOffset.UtcNow);
        BarcodeScanned?.Invoke(this, result);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _operationLock.Dispose();
        }

        base.Dispose(disposing);
    }
}
