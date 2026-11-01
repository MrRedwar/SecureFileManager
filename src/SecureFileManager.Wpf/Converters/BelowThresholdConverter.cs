using System.Globalization;
using System.Windows.Data;
namespace SecureFileManager.Wpf.Converters;

public sealed class BelowThresholdConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is double size && double.TryParse(parameter?.ToString(), NumberStyles.Number,
            CultureInfo.InvariantCulture, out var threshold) && size < threshold;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}
