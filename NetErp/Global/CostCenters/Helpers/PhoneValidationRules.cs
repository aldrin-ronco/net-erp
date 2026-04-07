using System.Collections.Generic;
using System.Linq;

namespace NetErp.Global.CostCenters.Shared
{
    /// <summary>
    /// Reglas puras de validación de teléfonos. Sin dependencias de WPF ni MVVM.
    /// Usadas por los validators extraídos del módulo CostCenters.
    /// </summary>
    public static class PhoneValidationRules
    {
        private const int LandlineDigits = 7;
        private const int CellPhoneDigits = 10;

        /// <summary>
        /// Limpia caracteres no numéricos de un valor de teléfono.
        /// </summary>
        public static string CleanForValidation(string? value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return new string(value.Where(char.IsDigit).ToArray());
        }

        /// <summary>
        /// Valida un teléfono fijo: si tiene contenido, debe tener exactamente 7 dígitos.
        /// </summary>
        public static IReadOnlyList<string> ValidateLandline(string? value)
        {
            string digits = CleanForValidation(value);
            if (digits.Length == 0) return [];
            return digits.Length != LandlineDigits
                ? [$"El número de teléfono debe contener {LandlineDigits} dígitos"]
                : [];
        }

        /// <summary>
        /// Valida un teléfono celular: si tiene contenido, debe tener exactamente 10 dígitos.
        /// </summary>
        public static IReadOnlyList<string> ValidateCellPhone(string? value)
        {
            string digits = CleanForValidation(value);
            if (digits.Length == 0) return [];
            return digits.Length != CellPhoneDigits
                ? [$"El número de teléfono celular debe contener {CellPhoneDigits} dígitos"]
                : [];
        }
    }
}
