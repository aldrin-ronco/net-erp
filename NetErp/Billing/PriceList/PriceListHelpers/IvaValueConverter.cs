using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace NetErp.Billing.PriceList.PriceListHelpers
{
    public class IvaValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal iva)
            {
                if (iva == -1)
                    return "N/A";

                return iva.ToString("0.00", CultureInfo.GetCultureInfo("en-US"));
            }

            return "N/A";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
