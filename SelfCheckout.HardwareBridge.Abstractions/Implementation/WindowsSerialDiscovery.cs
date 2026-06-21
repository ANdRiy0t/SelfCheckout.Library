using System.IO.Ports;
using System.Management;
using SelfCheckout.HardwareBridge.Abstractions.Events;
using SelfCheckout.HardwareBridge.Abstractions.Interfaces;

namespace SelfCheckout.HardwareBridge.Abstractions.Implementation;

public class WindowsSerialDiscovery : IDeviceDiscovery
{

    public event EventHandler<DeviceChangeEvent>? Changed;

    private ManagementEventWatcher? _watcher;
    private HashSet<string> _known = new(StringComparer.OrdinalIgnoreCase);

    public Task StartAsync(CancellationToken ct = default)
    {
        _known = SerialPort.GetPortNames().ToHashSet(StringComparer.OrdinalIgnoreCase);

        _watcher = new ManagementEventWatcher(
            new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 2"));
        _watcher.EventArrived += (_, __) =>
        {
            var now = SerialPort.GetPortNames().ToHashSet(StringComparer.OrdinalIgnoreCase);
            var added = now.Except(_known).ToList();
            _known = now;

            foreach (var com in added)
                Changed?.Invoke(this, new DeviceChangeEvent(DeviceChangeKind.Arrived, com));
        };

        _watcher.Start();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct = default)
    {
        _watcher?.Stop();
        _watcher?.Dispose();
        _watcher = null;
        return Task.CompletedTask;
    }
}
