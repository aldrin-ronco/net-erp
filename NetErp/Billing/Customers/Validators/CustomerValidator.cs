using System;
using System.Collections.Generic;

namespace NetErp.Billing.Customers.Validators
{
    /// <summary>
    /// Lógica de validación pura para Customer.
    /// Sin dependencias de WPF, Caliburn.Micro ni INotifyDataErrorInfo.
    /// </summary>
    public class CustomerValidator
    {
        /// <summary>
        /// Valida una propiedad de tipo string.
        /// </summary>
        public IReadOnlyList<string> Validate(string propertyName, string? value, CustomerValidationContext context)
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
                "IdentificationNumber" when string.IsNullOrEmpty(value) || value.Trim().Length < context.MinimumDocumentLength
                    => ["El número de identificación no puede estar vacío"],
                "FirstName" when string.IsNullOrEmpty(value.Trim()) && context.CaptureInfoAsPN
                    => ["El primer nombre no puede estar vacío"],
                "FirstLastName" when string.IsNullOrEmpty(value.Trim()) && context.CaptureInfoAsPN
                    => ["El primer apellido no puede estar vacío"],
                "BusinessName" when string.IsNullOrEmpty(value.Trim()) && context.CaptureInfoAsPJ
                    => ["La razón social no puede estar vacía"],
                "BlockingReason" when string.IsNullOrEmpty(value.Trim()) && !context.IsActive
                    => ["Debe especificar un motivo de bloqueo"],
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
        /// Valida una propiedad de tipo objeto (selecciones de ComboBox/LookUp).
        /// </summary>
        public IReadOnlyList<string> ValidateSelection(string propertyName, object? value)
        {
            return propertyName switch
            {
                "SelectedCountry" when value == null => ["Debe seleccionar un país"],
                "SelectedDepartment" when value == null => ["Debe seleccionar un departamento"],
                "SelectedCityId" when value is int cityId && cityId == 0 => ["Debe seleccionar un municipio"],
                _ => []
            };
        }

        /// <summary>
        /// Valida todas las propiedades según el modo de captura.
        /// </summary>
        public Dictionary<string, IReadOnlyList<string>> ValidateAll(CustomerValidationContext context)
        {
            var result = new Dictionary<string, IReadOnlyList<string>>();

            if (context.CaptureInfoAsPJ)
                AddIfErrors(result, "BusinessName", Validate("BusinessName", context.BusinessName, context));

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
        public bool CanSave(CustomerCanSaveContext context)
        {
            if (context.SelectedIdentificationType == null) return false;
            if (string.IsNullOrEmpty(context.IdentificationNumber?.Trim()) ||
                context.IdentificationNumber.Length < context.MinimumDocumentLength) return false;
            if (context.HasVerificationDigit && string.IsNullOrEmpty(context.VerificationDigit)) return false;
            if (context.CaptureInfoAsPJ && string.IsNullOrEmpty(context.BusinessName)) return false;
            if (context.CaptureInfoAsPN && (string.IsNullOrEmpty(context.FirstName) || string.IsNullOrEmpty(context.FirstLastName))) return false;
            if (context.SelectedCountry == null) return false;
            if (context.SelectedDepartment == null) return false;
            if (context.SelectedCityId == 0) return false;
            if (context.HasErrors) return false;
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
    /// Contexto de validación para propiedades individuales.
    /// Contiene el estado del ViewModel necesario para evaluar reglas condicionales.
    /// </summary>
    public class CustomerValidationContext
    {
        public bool CaptureInfoAsPN { get; init; }
        public bool CaptureInfoAsPJ { get; init; }
        public bool IsActive { get; init; }
        public int MinimumDocumentLength { get; init; }
        public string? BusinessName { get; init; }
        public string? FirstName { get; init; }
        public string? FirstLastName { get; init; }
        public string? IdentificationNumber { get; init; }
    }

    /// <summary>
    /// Contexto para evaluar CanSave — snapshot de todos los valores necesarios.
    /// </summary>
    public class CustomerCanSaveContext
    {
        public object? SelectedIdentificationType { get; init; }
        public string? IdentificationNumber { get; init; }
        public int MinimumDocumentLength { get; init; }
        public bool HasVerificationDigit { get; init; }
        public string? VerificationDigit { get; init; }
        public bool CaptureInfoAsPJ { get; init; }
        public bool CaptureInfoAsPN { get; init; }
        public string? BusinessName { get; init; }
        public string? FirstName { get; init; }
        public string? FirstLastName { get; init; }
        public object? SelectedCountry { get; init; }
        public object? SelectedDepartment { get; init; }
        public int SelectedCityId { get; init; }
        public bool HasErrors { get; init; }
        public bool IsNewRecord { get; init; }
        public bool HasChanges { get; init; }
        public bool HasEmailChanges { get; init; }
    }
}
