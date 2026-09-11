using System.Globalization;

namespace WashTrack.Converters
{
    // Flips a bool for XAML bindings. Used to drive the opposite of a flag
    // without adding a second property — e.g. IsEnabled="{Binding IsBusy,
    // Converter={StaticResource InverseBool}}" disables a button while loading.
    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is bool b && !b;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is bool b && !b;
    }
}