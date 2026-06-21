using System.Globalization;
using SelfCheckout.HardwareBridge.Abstractions.Enums;

namespace SelfCheckout.HardwareBridge.Demo.Maui.Converters;

public sealed class DeviceStateToDisplayStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is DeviceState s
            ? s switch
            {
                DeviceState.Disconnected => "Disconnected",
                DeviceState.Connecting   => "Connecting...",
                DeviceState.Ready        => "Ready",
                DeviceState.Busy         => "Busy",
                DeviceState.Error        => "Error",
                _                        => "Unknown",
            }
            : "Unknown";

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
