using System.Globalization;

namespace WashTrack.Converters
{
    // True when a string has actual content. Used to hide labels that would
    // otherwise render as blank rows — e.g. the error/info banners on the login
    // page only appear once a message is set. One-way only.
    public class StringToBoolConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => !string.IsNullOrWhiteSpace(value as string);

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}