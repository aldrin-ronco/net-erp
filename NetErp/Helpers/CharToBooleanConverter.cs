using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace NetErp.Helpers
{
    public class CharToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Compara el valor actual con el parámetro (la clave del diccionario)
            return value != null && parameter != null && value.Equals(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Devuelve el parámetro si el RadioButton está seleccionado
            return (bool)value ? parameter : Binding.DoNothing;
        }
    }
}
