using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace NetErp.Helpers
{
    public class StringToBooleanConverter: IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // Verifica si el valor es igual al parámetro
            return value != null && value.ToString() == parameter.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // Si el RadioButton está marcado, devuelve el parámetro como string
            if ((bool)value)
            {
                return parameter.ToString();
            }
            return Binding.DoNothing;
        }
    }
}
