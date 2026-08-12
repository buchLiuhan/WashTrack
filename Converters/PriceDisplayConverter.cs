using System.Globalization;
using WashTrack.Models;

namespace WashTrack.Converters
{
    public class PriceDisplayConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not Service service)
                return string.Empty;

            if (service.FlatRate.HasValue)
                return $"₱{service.FlatRate:F2}/piece";

            if (service.MinKilo.HasValue)
            {
                string text = $"₱{service.MinKiloCharge:F2} (up to {service.MinKilo:0.#}kg)";
                if (service.ExcessPerKilo.HasValue)
                    text += $" +₱{service.ExcessPerKilo:F2}/kg after";
                return text;
            }

            if (service.PricePerKilo.HasValue)
                return $"₱{service.PricePerKilo:F2}/kg";

            return "No price set";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}