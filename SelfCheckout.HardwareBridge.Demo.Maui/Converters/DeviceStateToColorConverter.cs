using System.Globalization;
using SelfCheckout.HardwareBridge.Abstractions.Enums;

namespace SelfCheckout.HardwareBridge.Demo.Maui.Converters;

public sealed class DeviceStateToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is DeviceState s
            ? s switch
            {
                DeviceState.Disconnected => Colors.Gray,
                DeviceState.Connecting   => Colors.Goldenrod,
                DeviceState.Ready        => Colors.LimeGreen,
                DeviceState.Busy         => Colors.DodgerBlue,
                DeviceState.Error        => Colors.Crimson,
                _                        => Colors.Transparent,
            }
            : Colors.Transparent;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
