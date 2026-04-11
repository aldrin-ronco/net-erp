using System.Collections.Generic;
using System.Linq;

namespace NetErp.Books.AccountingEntities.Validators
{
    /// <summary>
    /// Lógica de validación pura para AccountingEntity.
    /// Sin dependencias de WPF, Caliburn.Micro ni INotifyDataErrorInfo.
    /// Espeja el patrón de SupplierValidator pero incluye requerimientos de
    /// ubicación geográfica en CanSave y una ruta adicional de "tiene cambios
    /// en emails" para permitir guardado en modo edición.
    /// </summary>
    public class AccountingEntityValidator
    {
        /// <summary>
        /// Valida una propiedad de tipo string. El call site tipicamente viene del
        /// setter del VM con la propiedad recien asignada.
        /// </summary>
        public IReadOnlyList<string> Validate(string propertyName, string? value, AccountingEntityValidationContext context)
        {
            if (string.IsNullOrEmpty(value)) value = string.Empty;

            // Limpiar caracteres no-dígitos en teléfonos antes de contar.
            if (propertyName.Contains("Phone"))
            {
                value = new string(value.Where(char.IsDigit).ToArray());
            }

            return propertyName switch
            {
                "IdentificationNumber" when string.IsNullOrEmpty(value?.Trim())
                    => ["El número de identificación no puede estar vacío"],
                "IdentificationNumber" when value!.Trim().Length < context.MinimumDocumentLength
                    => [$"El número de identificación debe tener al menos {context.MinimumDocumentLength} caracteres"],
                "FirstName" when string.IsNullOrEmpty(context.FirstName?.Trim()) && context.CaptureInfoAsPN
                    => ["El primer nombre no puede estar vacío"],
                "FirstLastName" when string.IsNullOrEmpty(context.FirstLastName?.Trim()) && context.CaptureInfoAsPN
                    => ["El primer apellido no puede estar vacío"],
                "BusinessName" when string.IsNullOrEmpty(context.BusinessName?.Trim()) && context.CaptureInfoAsPJ
                    => ["La razón social no puede estar vacía"],
                "PrimaryPhone" when value.Length != 7 && !string.IsNullOrEmpty(value)
                    => ["El número de teléfono debe contener 7 dígitos"],
                "SecondaryPhone" when value.Length != 7 && !string.IsNullOrEmpty(value)
                    => ["El número de teléfono debe contener 7 dígitos"],
                "PrimaryCellPhone" when value.Length != 10 && !string.IsNullOrEmpty(value)
                    => ["El número de teléfono celular debe contener 10 dígitos"],
                "SecondaryCellPhone" when value.Length != 10 && !string.IsNullOrEmpty(value)
                    => ["El número de teléfono celular debe contener 10 dígitos"],
                _ => []
            };
        }

        /// <summary>
        /// Valida todas las propiedades según el modo de captura.
        /// </summary>
        public Dictionary<string, IReadOnlyList<string>> ValidateAll(AccountingEntityValidationContext context)
        {
            Dictionary<string, IReadOnlyList<string>> result = [];

            if (context.CaptureInfoAsPN)
            {
                AddIfErrors(result, "FirstName", Validate("FirstName", context.FirstName, context));
                AddIfErrors(result, "FirstLastName", Validate("FirstLastName", context.FirstLastName, context));
                AddIfErrors(result, "IdentificationNumber", Validate("IdentificationNumber", context.IdentificationNumber, context));
            }

            if (context.CaptureInfoAsPJ)
            {
                AddIfErrors(result, "BusinessName", Validate("BusinessName", context.BusinessName, context));
            }

            return result;
        }

        /// <summary>
        /// Evalúa si el formulario es guardable.
        /// </summary>
        public bool CanSave(AccountingEntityCanSaveContext context)
        {
            if (context.IsBusy) return false;
            if (context.MinimumDocumentLength == 0) return false;
            if (string.IsNullOrEmpty(context.IdentificationNumber?.Trim()) ||
                context.IdentificationNumber.Length < context.MinimumDocumentLength) return false;
            if (context.HasVerificationDigit && string.IsNullOrEmpty(context.VerificationDigit)) return false;
            if (context.CaptureInfoAsPJ && string.IsNullOrEmpty(context.BusinessName)) return false;
            if (context.CaptureInfoAsPN && (string.IsNullOrEmpty(context.FirstName) || string.IsNullOrEmpty(context.FirstLastName))) return false;
            if (!context.HasCountry) return false;
            if (!context.HasDepartment) return false;
            if (!context.HasCity) return false;
            if (context.HasErrors) return false;

            // Para registros existentes exigimos alguna modificación (propiedades o emails).
            // Los registros nuevos siempre son "modificados" desde el estado vacío.
            if (!context.IsNewRecord && !context.HasChanges && !context.HasEmailChanges) return false;

            return true;
        }

        private static void AddIfErrors(Dictionary<string, IReadOnlyList<string>> dict,
                                         string key, IReadOnlyList<string> errors)
        {
            if (errors.Count > 0) dict[key] = errors;
        }
    }

    /// <summary>
    /// Contexto para validación de propiedades individuales.
    /// </summary>
    public class AccountingEntityValidationContext
    {
        public bool CaptureInfoAsPN { get; init; }
        public bool CaptureInfoAsPJ { get; init; }
        public int MinimumDocumentLength { get; init; }
        public string? IdentificationNumber { get; init; }
        public string? FirstName { get; init; }
        public string? FirstLastName { get; init; }
        public string? BusinessName { get; init; }
        public string? PrimaryPhone { get; init; }
        public string? SecondaryPhone { get; init; }
        public string? PrimaryCellPhone { get; init; }
        public string? SecondaryCellPhone { get; init; }
    }

    /// <summary>
    /// Contexto para evaluar CanSave. Además de los campos de validación incluye
    /// gates de lifecycle (IsBusy, IsNewRecord, HasChanges, HasErrors) y los flags
    /// de ubicación geográfica.
    /// </summary>
    public class AccountingEntityCanSaveContext
    {
        public bool IsBusy { get; init; }
        public bool IsNewRecord { get; init; }
        public int MinimumDocumentLength { get; init; }
        public bool HasVerificationDigit { get; init; }
        public string? IdentificationNumber { get; init; }
        public string? VerificationDigit { get; init; }
        public bool CaptureInfoAsPN { get; init; }
        public bool CaptureInfoAsPJ { get; init; }
        public string? FirstName { get; init; }
        public string? FirstLastName { get; init; }
        public string? BusinessName { get; init; }
        public bool HasCountry { get; init; }
        public bool HasDepartment { get; init; }
        public bool HasCity { get; init; }
        public bool HasChanges { get; init; }
        public bool HasEmailChanges { get; init; }
        public bool HasErrors { get; init; }
    }
}
