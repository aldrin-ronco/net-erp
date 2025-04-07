using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace NetErp.Helpers
{
    public class EnumToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return Visibility.Collapsed;

            // El parámetro debe ser una cadena con formato "Valor1:Visibility1;Valor2:Visibility2;..."
            string paramString = parameter.ToString();
            if (string.IsNullOrEmpty(paramString))
                return Visibility.Collapsed;

            string currentValue = value.ToString();
            string[] mappings = paramString.Split(';');

            foreach (string mapping in mappings)
            {
                string[] parts = mapping.Split(':');
                if (parts.Length == 2 && parts[0] == currentValue)
                {
                    if (Enum.TryParse<Visibility>(parts[1], out Visibility result))
                        return result;
                }
            }

            // Valor predeterminado si no hay coincidencia
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
