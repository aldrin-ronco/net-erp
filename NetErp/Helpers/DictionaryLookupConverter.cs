using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace NetErp.Helpers
{
    public class DictionaryLookupConverter : IValueConverter
    {
        public Dictionary<string, string>? Dictionary { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string key && Dictionary != null && Dictionary.TryGetValue(key, out string? display))
                return display;
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
