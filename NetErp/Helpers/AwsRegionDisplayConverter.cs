using Dictionaries;
using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace NetErp.Helpers
{
    /// <summary>
    /// Convierte el enum value (<c>US_EAST_1</c>) en su descripción amigable
    /// (<c>US East (N. Virginia)</c>) usando <see cref="GlobalDictionaries.AwsRegions"/>.
    /// </summary>
    public class AwsRegionDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string enumValue || string.IsNullOrEmpty(enumValue)) return string.Empty;
            AwsRegionItem? item = GlobalDictionaries.AwsRegions.FirstOrDefault(r => r.EnumValue == enumValue);
            return item?.Display ?? enumValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
