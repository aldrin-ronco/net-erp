using System;
using System.Globalization;
using System.Windows.Data;

namespace NetErp.Helpers
{
    /// <summary>
    /// Converter que retorna la longitud de un string.
    /// Utilizado para mostrar contadores de caracteres en campos de texto.
    /// </summary>
    public class StringLengthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                return str.Length.ToString();
            }
            return "0";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
