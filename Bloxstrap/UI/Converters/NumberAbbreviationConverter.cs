using System;
using System.Globalization;
using System.Windows.Data;

namespace Bloxstrap.UI.Converters
{
    public class NumberAbbreviationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return string.Empty;

            string? stringValue = value.ToString();
            if (string.IsNullOrEmpty(stringValue))
                return string.Empty;

            if (double.TryParse(stringValue, out double number))
            {
                if (number >= 1_000_000_000)
                    return (number / 1_000_000_000D).ToString("0.#") + "B";
                if (number >= 1_000_000)
                    return (number / 1_000_000D).ToString("0.#") + "M";
                if (number >= 1_000)
                    return (number / 1_000D).ToString("0.#") + "K";
                return number.ToString("0");
            }

            return stringValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}