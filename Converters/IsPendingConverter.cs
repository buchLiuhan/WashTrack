using System.Globalization;

namespace WashTrack.Converters
{
    // True when a Transaction.Status is "Pending". Counterpart to
    // IsCompletedConverter — shows the pending badge and the action buttons
    // that only apply to unfinished jobs. One-way only.
    public class IsPendingConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is string status && status == "Pending";

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}