using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Constants
{
    public static class Constant
    {
        public static readonly string DefaultCountryCode = "169";
        public static readonly string DefaultDepartmentCode = "01";
        public static readonly string DefaultCityCode = "001";
        public static readonly string DefaultIdentificationTypeCode = "31";

        /// <summary>
        /// Límite máximo de caracteres para campos MemoEdit (leyendas, observaciones, etc.)
        /// </summary>
        public const int MemoMaxLength = 600;

        /// <summary>
        /// Versión string de MemoMaxLength para binding en XAML
        /// </summary>
        public const string MemoMaxLengthDisplay = "600";

    }
}
