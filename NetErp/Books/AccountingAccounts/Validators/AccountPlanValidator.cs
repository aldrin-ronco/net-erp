using System.Collections.Generic;
using System.Linq;

namespace NetErp.Books.AccountingAccounts.Validators
{
    /// <summary>
    /// Lógica de validación pura para el plan de cuentas contables.
    /// Maneja los dos modos del formulario:
    ///   - Nuevo registro: exige que todos los niveles (Lv1..Lv5) tengan nombre, que
    ///     el código auxiliar tenga 8 dígitos, no esté duplicado, y que el margen sea
    ///     mayor que cero cuando el prefijo de cuenta lo requiera.
    ///   - Edición: solo valida el nombre del nivel que efectivamente se está editando
    ///     (el nivel más profundo visible, derivado del largo del código original).
    /// </summary>
    public class AccountPlanValidator
    {
        /// <summary>
        /// Prefijos de cuentas auxiliares (Lv5) que requieren margen obligatorio.
        /// Convención contable local. Si se agregan o remueven prefijos, los tests del
        /// validador deben actualizarse en consecuencia.
        /// </summary>
        private static readonly string[] _marginPrefixes = ["2408", "2365", "2367", "2368"];

        public IReadOnlyList<string> Validate(string propertyName, object? value, AccountPlanValidationContext context)
        {
            return propertyName switch
            {
                nameof(AccountPlanValidationContext.Lv5Code) when context.IsNewRecord
                    => ValidateLv5CodeForNew(value as string, context),
                nameof(AccountPlanValidationContext.Lv1Name)
                    => ValidateNameAtLevel(value as string, level: 1, context),
                nameof(AccountPlanValidationContext.Lv2Name)
                    => ValidateNameAtLevel(value as string, level: 2, context),
                nameof(AccountPlanValidationContext.Lv3Name)
                    => ValidateNameAtLevel(value as string, level: 3, context),
                nameof(AccountPlanValidationContext.Lv4Name)
                    => ValidateNameAtLevel(value as string, level: 4, context),
                nameof(AccountPlanValidationContext.Lv5Name)
                    => ValidateNameAtLevel(value as string, level: 5, context),
                nameof(AccountPlanValidationContext.Margin) when context.RequiresMargin
                    => ValidateMargin(value as decimal?, context),
                _ => []
            };
        }

        public Dictionary<string, IReadOnlyList<string>> ValidateAll(AccountPlanValidationContext context)
        {
            Dictionary<string, IReadOnlyList<string>> result = [];

            if (context.IsNewRecord)
            {
                AddIfErrors(result, nameof(AccountPlanValidationContext.Lv5Code), ValidateLv5CodeForNew(context.Lv5Code, context));
                AddIfErrors(result, nameof(AccountPlanValidationContext.Lv1Name), ValidateNameAtLevel(context.Lv1Name, 1, context));
                AddIfErrors(result, nameof(AccountPlanValidationContext.Lv2Name), ValidateNameAtLevel(context.Lv2Name, 2, context));
                AddIfErrors(result, nameof(AccountPlanValidationContext.Lv3Name), ValidateNameAtLevel(context.Lv3Name, 3, context));
                AddIfErrors(result, nameof(AccountPlanValidationContext.Lv4Name), ValidateNameAtLevel(context.Lv4Name, 4, context));
                AddIfErrors(result, nameof(AccountPlanValidationContext.Lv5Name), ValidateNameAtLevel(context.Lv5Name, 5, context));
                if (context.RequiresMargin)
                    AddIfErrors(result, nameof(AccountPlanValidationContext.Margin), ValidateMargin(context.Margin, context));
            }
            else
            {
                int level = GetDeepestVisibleLevel(context.CodeLength);
                string? nameValue = level switch
                {
                    1 => context.Lv1Name,
                    2 => context.Lv2Name,
                    3 => context.Lv3Name,
                    4 => context.Lv4Name,
                    5 => context.Lv5Name,
                    _ => null
                };
                string nameKey = $"Lv{level}Name";
                AddIfErrors(result, nameKey, ValidateNameAtLevel(nameValue, level, context));

                if (level == 5 && context.RequiresMargin)
                    AddIfErrors(result, nameof(AccountPlanValidationContext.Margin), ValidateMargin(context.Margin, context));
            }

            return result;
        }

        public bool CanSave(AccountPlanCanSaveContext context)
        {
            if (context.IsBusy) return false;
            if (context.RequiresMargin && context.Margin <= 0) return false;

            if (context.IsNewRecord)
            {
                if (string.IsNullOrWhiteSpace(context.Lv5Code) || context.Lv5Code.Length < 8) return false;
                if (context.ExistingCodes.Contains(context.Lv5Code)) return false;
                if (string.IsNullOrWhiteSpace(context.Lv1Name)) return false;
                if (string.IsNullOrWhiteSpace(context.Lv2Name)) return false;
                if (string.IsNullOrWhiteSpace(context.Lv3Name)) return false;
                if (string.IsNullOrWhiteSpace(context.Lv4Name)) return false;
                if (string.IsNullOrWhiteSpace(context.Lv5Name)) return false;
                return true;
            }

            // Edit mode: only the deepest visible level's name is required.
            int level = GetDeepestVisibleLevel(context.CodeLength);
            string? nameValue = level switch
            {
                1 => context.Lv1Name,
                2 => context.Lv2Name,
                3 => context.Lv3Name,
                4 => context.Lv4Name,
                5 => context.Lv5Name,
                _ => null
            };
            if (string.IsNullOrWhiteSpace(nameValue)) return false;

            // Lv5 (auxiliary) additionally needs the code to have 8 digits.
            if (level == 5 && (string.IsNullOrWhiteSpace(context.Lv5Code) || context.Lv5Code.Length < 8))
                return false;

            return true;
        }

        /// <summary>
        /// Returns true when the given account code is an 8-digit auxiliary whose prefix
        /// requires a margin value to be captured (withholding/tax accounts).
        /// </summary>
        public static bool RequiresMarginFor(string code)
        {
            if (string.IsNullOrEmpty(code) || code.Length < 8) return false;
            return _marginPrefixes.Any(p => code.StartsWith(p));
        }

        /// <summary>
        /// Maps an account code length to its hierarchical level:
        /// 1 digit = Lv1 (class), 2 = Lv2 (group), 4 = Lv3 (account),
        /// 6 = Lv4 (subaccount), 8+ = Lv5 (auxiliary).
        /// </summary>
        private static int GetDeepestVisibleLevel(int codeLength) => codeLength switch
        {
            >= 8 => 5,
            >= 6 => 4,
            >= 4 => 3,
            >= 2 => 2,
            _ => 1
        };

        private static IReadOnlyList<string> ValidateLv5CodeForNew(string? value, AccountPlanValidationContext context)
        {
            if (string.IsNullOrWhiteSpace(value))
                return ["El código de la cuenta auxiliar no puede estar vacío"];
            if (value.Length < 8)
                return ["El código de la cuenta auxiliar debe tener 8 dígitos"];
            if (context.ExistingCodes.Contains(value))
                return ["Ya existe una cuenta con este código"];
            return [];
        }

        private static IReadOnlyList<string> ValidateNameAtLevel(string? value, int level, AccountPlanValidationContext context)
        {
            // In new mode every level is required.
            // In edit mode only the deepest visible level is required (shallower ones are
            // read-only and already populated from the database).
            bool required = context.IsNewRecord || GetDeepestVisibleLevel(context.CodeLength) == level;
            if (!required) return [];

            return string.IsNullOrWhiteSpace(value)
                ? [$"El nombre del {LevelLabel(level)} no puede estar vacío"]
                : [];
        }

        private static IReadOnlyList<string> ValidateMargin(decimal? value, AccountPlanValidationContext context)
        {
            if (!context.RequiresMargin) return [];
            return (value ?? 0m) <= 0m
                ? ["El margen es obligatorio para esta cuenta y debe ser mayor que cero"]
                : [];
        }

        private static string LevelLabel(int level) => level switch
        {
            1 => "clase",
            2 => "grupo",
            3 => "cuenta",
            4 => "sub cuenta",
            5 => "auxiliar",
            _ => "nivel"
        };

        private static void AddIfErrors(Dictionary<string, IReadOnlyList<string>> dict,
                                         string key, IReadOnlyList<string> errors)
        {
            if (errors.Count > 0) dict[key] = errors;
        }
    }

    public class AccountPlanValidationContext
    {
        public bool IsNewRecord { get; init; }

        /// <summary>
        /// Length of the entity's Code field. In edit mode this determines which level
        /// of the plan is actually being edited; in new mode this should mirror Lv5Code.Length.
        /// </summary>
        public int CodeLength { get; init; }

        public string Lv5Code { get; init; } = string.Empty;
        public string Lv1Name { get; init; } = string.Empty;
        public string Lv2Name { get; init; } = string.Empty;
        public string Lv3Name { get; init; } = string.Empty;
        public string Lv4Name { get; init; } = string.Empty;
        public string Lv5Name { get; init; } = string.Empty;
        public decimal Margin { get; init; }
        public bool RequiresMargin { get; init; }
        public HashSet<string> ExistingCodes { get; init; } = [];
    }

    public class AccountPlanCanSaveContext
    {
        public bool IsBusy { get; init; }
        public bool IsNewRecord { get; init; }
        public int CodeLength { get; init; }
        public string Lv5Code { get; init; } = string.Empty;
        public string Lv1Name { get; init; } = string.Empty;
        public string Lv2Name { get; init; } = string.Empty;
        public string Lv3Name { get; init; } = string.Empty;
        public string Lv4Name { get; init; } = string.Empty;
        public string Lv5Name { get; init; } = string.Empty;
        public decimal Margin { get; init; }
        public bool RequiresMargin { get; init; }
        public HashSet<string> ExistingCodes { get; init; } = [];
    }
}
