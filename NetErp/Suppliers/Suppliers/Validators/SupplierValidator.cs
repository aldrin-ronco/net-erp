using System;
using System.Collections.Generic;
using System.Linq;

namespace NetErp.Suppliers.Suppliers.Validators
{
    /// <summary>
    /// Lógica de validación pura para Supplier.
    /// Sin dependencias de WPF, Caliburn.Micro ni INotifyDataErrorInfo.
    /// </summary>
    public class SupplierValidator
    {
        /// <summary>
        /// Valida una propiedad de tipo string.
        /// </summary>
        public IReadOnlyList<string> Validate(string propertyName, string? value, SupplierValidationContext context)
        {
            if (string.IsNullOrEmpty(value)) value = string.Empty;

            // Limpiar caracteres no numéricos para teléfonos antes de contar
            if (propertyName.Contains("Phone"))
            {
                value = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
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
        /// Valida una propiedad decimal (e.g. IcaWithholdingRate).
        /// </summary>
        public IReadOnlyList<string> Validate(string propertyName, decimal value, SupplierValidationContext context)
        {
            return propertyName switch
            {
                "IcaWithholdingRate" when value < 0m || value > 100m
                    => ["El valor debe estar entre 0 y 100"],
                _ => []
            };
        }

        /// <summary>
        /// Valida todas las propiedades según el modo de captura.
        /// </summary>
        public Dictionary<string, IReadOnlyList<string>> ValidateAll(SupplierValidationContext context)
        {
            Dictionary<string, IReadOnlyList<string>> result = [];

            AddIfErrors(result, "IdentificationNumber", Validate("IdentificationNumber", context.IdentificationNumber, context));

            if (context.CaptureInfoAsPN)
            {
                AddIfErrors(result, "FirstName", Validate("FirstName", context.FirstName, context));
                AddIfErrors(result, "FirstLastName", Validate("FirstLastName", context.FirstLastName, context));
            }

            if (context.CaptureInfoAsPJ)
            {
                AddIfErrors(result, "BusinessName", Validate("BusinessName", context.BusinessName, context));
            }

            AddIfErrors(result, "PrimaryPhone", Validate("PrimaryPhone", context.PrimaryPhone, context));
            AddIfErrors(result, "SecondaryPhone", Validate("SecondaryPhone", context.SecondaryPhone, context));
            AddIfErrors(result, "PrimaryCellPhone", Validate("PrimaryCellPhone", context.PrimaryCellPhone, context));
            AddIfErrors(result, "SecondaryCellPhone", Validate("SecondaryCellPhone", context.SecondaryCellPhone, context));

            AddIfErrors(result, "IcaWithholdingRate", Validate("IcaWithholdingRate", context.IcaWithholdingRate, context));

            return result;
        }

        /// <summary>
        /// Evalúa si el formulario es guardable.
        /// </summary>
        public bool CanSave(SupplierCanSaveContext context)
        {
            if (context.IsBusy) return false;
            if (context.MinimumDocumentLength == 0) return false;
            if (string.IsNullOrEmpty(context.IdentificationNumber?.Trim()) ||
                context.IdentificationNumber.Length < context.MinimumDocumentLength) return false;
            if (context.HasVerificationDigit && string.IsNullOrEmpty(context.VerificationDigit)) return false;
            if (context.CaptureInfoAsPJ && string.IsNullOrEmpty(context.BusinessName)) return false;
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
    public class SupplierValidationContext
    {
        public bool CaptureInfoAsPN { get; init; }
        public bool CaptureInfoAsPJ { get; init; }
        public int MinimumDocumentLength { get; init; }
        public string? FirstName { get; init; }
        public string? FirstLastName { get; init; }
        public string? BusinessName { get; init; }
        public string? IdentificationNumber { get; init; }
        public string? PrimaryPhone { get; init; }
        public string? SecondaryPhone { get; init; }
        public string? PrimaryCellPhone { get; init; }
        public string? SecondaryCellPhone { get; init; }
        public decimal IcaWithholdingRate { get; init; }
    }

    /// <summary>
    /// Contexto para evaluar CanSave.
    /// </summary>
    public class SupplierCanSaveContext
    {
        public bool IsBusy { get; init; }
        public string? IdentificationNumber { get; init; }
        public int MinimumDocumentLength { get; init; }
        public bool HasVerificationDigit { get; init; }
        public string? VerificationDigit { get; init; }
        public bool CaptureInfoAsPN { get; init; }
        public bool CaptureInfoAsPJ { get; init; }
        public string? FirstName { get; init; }
        public string? FirstLastName { get; init; }
        public string? BusinessName { get; init; }
        public bool HasChanges { get; init; }
        public bool HasErrors { get; init; }
    }
}
