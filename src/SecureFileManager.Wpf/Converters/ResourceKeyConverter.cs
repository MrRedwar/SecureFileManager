using System.Globalization;
using System.Windows;
using System.Windows.Data;
namespace SecureFileManager.Wpf.Converters;

public sealed class ResourceKeyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is string key ? Application.Current.TryFindResource(key) ?? DependencyProperty.UnsetValue : DependencyProperty.UnsetValue;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}
