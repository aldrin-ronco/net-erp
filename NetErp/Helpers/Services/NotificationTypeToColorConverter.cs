using NetErp.Billing.PriceList.DTO;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace NetErp.Helpers.Services
{
    public class NotificationTypeToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is NotificationType type)
            {
                return type switch
                {
                    NotificationType.Success => new SolidColorBrush(Color.FromRgb(46, 158, 70)),    // Verde
                    NotificationType.Error => new SolidColorBrush(Color.FromRgb(204, 50, 50)),      // Rojo
                    NotificationType.Warning => new SolidColorBrush(Color.FromRgb(245, 154, 35)),   // Naranja
                    NotificationType.Info => new SolidColorBrush(Color.FromRgb(31, 120, 210)),      // Azul
                    _ => new SolidColorBrush(Colors.Gray)
                };
            }

            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class NotificationTypeToSymbolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is NotificationType type)
            {
                return type switch
                {
                    NotificationType.Success => "✓",  // Símbolo de verificación
                    NotificationType.Error => "✗",    // Símbolo X
                    NotificationType.Warning => "⚠",  // Símbolo de advertencia
                    NotificationType.Info => "ℹ",     // Símbolo de información
                    _ => "ℹ"
                };
            }

            return "ℹ";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class OperationStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is OperationStatus status)
            {
                return status switch
                {
                    OperationStatus.Pending => new SolidColorBrush(Colors.Orange),
                    OperationStatus.Saved => new SolidColorBrush(Colors.Green),
                    OperationStatus.Failed => new SolidColorBrush(Colors.Red),
                    _ => new SolidColorBrush(Colors.Transparent)
                };
            }

            return new SolidColorBrush(Colors.Transparent);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
