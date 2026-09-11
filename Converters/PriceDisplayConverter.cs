using System.Globalization;
using WashTrack.Models;

namespace WashTrack.Converters
{
    // Turns a Service into the one-line price label shown in the services list.
    // A Service stores whichever pricing mode it uses (see Models/Service.cs)
    // and leaves the other fields null, so this picks the first mode that's set.
    // Order matters: flat rate wins over min-kilo, and min-kilo over per-kilo,
    // because a service configured with a minimum charge also has a per-kilo
    // rate for the excess and we want the minimum shown first.
    public class PriceDisplayConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not Service service)
                return string.Empty;

            // Mode C: Flat Rate
            if (service.FlatRate.HasValue)
                return $"₱{service.FlatRate:F2}/piece";

            // Mode B: Minimum Charge (+ optional excess rate past the minimum)
            if (service.MinKilo.HasValue)
            {
                string text = $"₱{service.MinKiloCharge:F2} (up to {service.MinKilo:0.#}kg)";
                if (service.ExcessPerKilo.HasValue)
                    text += $" +₱{service.ExcessPerKilo:F2}/kg after";
                return text;
            }

            // Mode A: Per Kilo
            if (service.PricePerKilo.HasValue)
                return $"₱{service.PricePerKilo:F2}/kg";

            // Service was saved without any pricing mode filled in
            return "No price set";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}