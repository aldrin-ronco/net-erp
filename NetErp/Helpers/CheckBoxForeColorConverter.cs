using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace NetErp.Helpers
{
    public class CheckBoxForeColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? new SolidColorBrush(Colors.Blue) : new SolidColorBrush(Colors.Black);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
