using Common.Validators;
using Models.DTO.Billing;
using System;

namespace Services.Validators
{
    public class CreditLimitValidator : ICreditLimitValidator
    {
        public ValidationResult ValidateLimit(decimal newLimit, decimal currentUsed, decimal originalLimit)
        {
            // Regla 1: El límite no puede ser menor al valor usado
            if (newLimit < currentUsed)
            {
                return ValidationResult.Error("El valor autorizado no puede ser menor al valor utilizado");
            }

            // Regla 2: El límite no puede ser negativo
            if (newLimit < 0)
            {
                return ValidationResult.Error("El límite de crédito no puede ser negativo");
            }

            // Regla 3: Cambios significativos generan advertencia
            if (originalLimit > 0)
            {
                decimal changePercentage = Math.Abs((newLimit - originalLimit) / originalLimit) * 100;
                if (changePercentage > 50)
                {
                    return ValidationResult.Warning($"El cambio de límite es superior al 50% ({changePercentage:F1}%).");
                }
            }

            // Regla 4: Límites muy altos generan advertencia
            if (newLimit > 10000000) // 10 millones
            {
                return ValidationResult.Warning($"El límite de crédito es muy alto ({newLimit:C}). Verifique que sea correcto.");
            }

            return ValidationResult.Success();
        }

    }
}