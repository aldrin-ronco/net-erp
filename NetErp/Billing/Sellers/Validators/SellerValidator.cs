using System;
using System.Collections.Generic;

namespace NetErp.Billing.Sellers.Validators
{
    /// <summary>
    /// Lógica de validación pura para Seller.
    /// Sin dependencias de WPF, Caliburn.Micro ni INotifyDataErrorInfo.
    /// </summary>
    public class SellerValidator
    {
        /// <summary>
        /// Valida una propiedad de tipo string.
        /// </summary>
        public IReadOnlyList<string> Validate(string propertyName, string? value, SellerValidationContext context)
        {
            if (string.IsNullOrEmpty(value)) value = string.Empty;

            // Limpiar caracteres de teléfono antes de validar
            if (propertyName.Contains("Phone"))
            {
                value = value.Replace(" ", "").Replace(Convert.ToChar(9).ToString(), "");
                value = value.Replace(Convert.ToChar(44).ToString(), "").Replace(Convert.ToChar(59).ToString(), "");
                value = value.Replace(Convert.ToChar(45).ToString(), "").Replace(Convert.ToChar(95).ToString(), "");
            }

            return propertyName switch
            {
                "IdentificationNumber" when string.IsNullOrEmpty(context.IdentificationNumber) || (context.IdentificationNumber ?? "").Trim().Length < context.MinimumDocumentLength
                    => ["El número de identificación no puede estar vacío"],
                "FirstName" when string.IsNullOrEmpty(context.FirstName?.Trim()) && context.CaptureInfoAsPN
                    => ["El primer nombre no puede estar vacío"],
                "FirstLastName" when string.IsNullOrEmpty(context.FirstLastName?.Trim()) && context.CaptureInfoAsPN
                    => ["El primer apellido no puede estar vacío"],
                "PrimaryPhone" when value.Length != 7 && !string.IsNullOrEmpty(value)
                    => ["El número de teléfono debe contener 7 digitos"],
                "SecondaryPhone" when value.Length != 7 && !string.IsNullOrEmpty(value)
                    => ["El número de teléfono debe contener 7 digitos"],
                "PrimaryCellPhone" when value.Length != 10 && !string.IsNullOrEmpty(value)
                    => ["El número de teléfono celular debe contener 10 digitos"],
                "SecondaryCellPhone" when value.Length != 10 && !string.IsNullOrEmpty(value)
                    => ["El número de teléfono celular debe contener 10 digitos"],
                _ => []
            };
        }

        /// <summary>
        /// Valida todas las propiedades según el modo de captura.
        /// </summary>
        public Dictionary<string, IReadOnlyList<string>> ValidateAll(SellerValidationContext context)
        {
            var result = new Dictionary<string, IReadOnlyList<string>>();

            if (context.CaptureInfoAsPN)
            {
                AddIfErrors(result, "FirstName", Validate("FirstName", context.FirstName, context));
                AddIfErrors(result, "FirstLastName", Validate("FirstLastName", context.FirstLastName, context));
                AddIfErrors(result, "IdentificationNumber", Validate("IdentificationNumber", context.IdentificationNumber, context));
            }

            return result;
        }

        /// <summary>
        /// Evalúa si el formulario es guardable.
        /// </summary>
        public bool CanSave(SellerCanSaveContext context)
        {
            if (string.IsNullOrEmpty(context.IdentificationNumber?.Trim()) ||
                context.IdentificationNumber.Length < context.MinimumDocumentLength) return false;
            if (context.CostCenterSelectedCount == 0) return false;
            if (context.CaptureInfoAsPN && (string.IsNullOrEmpty(context.FirstName) || string.IsNullOrEmpty(context.FirstLastName))) return false;
            if (!context.HasChanges) return false;
            if (context.HasErrors) return false;
            return true;
        }

        private static void AddIfErrors(Dictionary<string, IReadOnlyList<string>> dict,
                                         string key, IReadOnlyList<string> errors)
        {
            if (errors.Count > 0) dict[key] = errors;
        }
    }

    /// <summary>
    /// Contexto de validación para propiedades individuales.
    /// </summary>
    public class SellerValidationContext
    {
        public bool CaptureInfoAsPN { get; init; }
        public int MinimumDocumentLength { get; init; }
        public string? FirstName { get; init; }
        public string? FirstLastName { get; init; }
        public string? IdentificationNumber { get; init; }
    }

    /// <summary>
    /// Contexto para evaluar CanSave.
    /// </summary>
    public class SellerCanSaveContext
    {
        public string? IdentificationNumber { get; init; }
        public int MinimumDocumentLength { get; init; }
        public int CostCenterSelectedCount { get; init; }
        public bool CaptureInfoAsPN { get; init; }
        public string? FirstName { get; init; }
        public string? FirstLastName { get; init; }
        public bool HasChanges { get; init; }
        public bool HasErrors { get; init; }
    }
}
