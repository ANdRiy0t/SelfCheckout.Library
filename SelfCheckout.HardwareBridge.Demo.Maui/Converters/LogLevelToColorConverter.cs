using System.Globalization;
using SelfCheckout.HardwareBridge.Demo.Maui.ViewModels;

namespace SelfCheckout.HardwareBridge.Demo.Maui.Converters;

public sealed class LogLevelToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is LogLevel l
            ? l switch
            {
                LogLevel.Info  => Color.FromArgb("#C9D1D9"),
                LogLevel.Error => Colors.Crimson,
                _              => Color.FromArgb("#C9D1D9"),
            }
            : Color.FromArgb("#C9D1D9");

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
