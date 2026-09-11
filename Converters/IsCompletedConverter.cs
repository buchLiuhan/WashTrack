using System.Globalization;
namespace WashTrack.Converters
{
    // True when a Transaction.Status is "Completed". Lets the transaction list
    // show/hide the "done" badge straight from the status string.
    // One-way only — ConvertBack is never called by these bindings.
    public class IsCompletedConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is string status && status == "Completed";
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}