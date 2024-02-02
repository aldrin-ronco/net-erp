using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace NetErp.Helpers
{
    public class StringToStyleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var _style = Application.Current.TryFindResource((string)value);
            return _style;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
