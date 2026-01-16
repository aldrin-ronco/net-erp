using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace NetErp.Helpers
{
    public class EnumBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var ParameterString = parameter as string;
            if (ParameterString == null)
                return DependencyProperty.UnsetValue;

            if (Enum.IsDefined(value.GetType(), value) == false)
                return DependencyProperty.UnsetValue;

            object paramvalue = Enum.Parse(value.GetType(), ParameterString);
            return paramvalue.Equals(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var ParameterString = parameter as string;
            var valueAsBool = (bool)value;

            // Cuando el RadioButton se desmarca (false), no debemos modificar la propiedad
            // Solo actualizamos cuando se marca (true)
            if (ParameterString == null || !valueAsBool)
            {
                return Binding.DoNothing;
            }
            return Enum.Parse(targetType, ParameterString);
        }
    }
}
