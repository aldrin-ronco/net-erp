using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Dictionaries
{
    public static class BooksDictionaries
    {
        /// <summary>
        /// Naturalezas de las cuentas contables
        /// </summary>
        public static Dictionary<char, string> AccountNatureDictionary = new()
        {
            { 'D', "DÉBITO" },
            { 'C', "CRÉDITO" }
        };

        /// <summary>
        /// Tipos de anulaciones de documentos
        /// </summary>
        public static Dictionary<char, string> AnnulmentTypeDictionary = new()
        {
            { 'A', "CON DOCUMENTO ADICIONAL" },
            { 'X', "SIN DOCUMENTO ADICIONAL" }
        };

        /// <summary>
        /// Regimenes de tributacion
        /// </summary>
        public static Dictionary<char, string> RegimeDictionary = new()
        {
            { 'R', "RESPONSABLE DE IVA" },
            { 'N', "NO RESPONSABLE DE IVA" }
        };

        /// <summary>
        /// Enumeracion para el tipo de captura de datos PN = Persona Natural, RS = Razon Social
        /// </summary>
        public enum CaptureTypeEnum { PN, RS, Undefined }
   
        public enum SequenceOriginEnum { M, D, Undefined }

        public static Dictionary<string, string> RetentionGroupDictionary = new()
        {
            { "RTFTE", "Retención de Renta" },
            { "RTIVA", "Retención de IVA" },
            { "RTICA", "Retención de ICA" },
            { "RCREE", "Retención de CREE" },
        };

        public static Dictionary<char, string> ModeDictionary = new()
        {
            { 'A', "Autoriza" },
            { 'H', "Habilita" },

        };

        public enum PersonType
        {
            [EnumMember(Value = "LEGAL_ENTITY")]
            LegalEntity,
            [EnumMember(Value = "NATURAL_PERSON")]
            NaturalPerson

        }

        // Régimen de IVA esperado por la API
        public enum TaxRegime
        {
            [EnumMember(Value = "NON_VAT_RESPONSIBLE")]
            NonVatResponsible,

            [EnumMember(Value = "VAT_RESPONSIBLE")]
            VatResponsible
        }

    }

    // Extensiones para mapeo a valores de API y nombres de visualización
    public static class PersonTypeExtensions
    {
        // Devuelve el valor exacto esperado por la API (GraphQL)
        public static string ToApiValue(this BooksDictionaries.PersonType value) => value switch
        {
            BooksDictionaries.PersonType.LegalEntity => "LEGAL_ENTITY",
            BooksDictionaries.PersonType.NaturalPerson => "NATURAL_PERSON",
            _ => throw new System.ArgumentOutOfRangeException(nameof(value), value, "Unsupported PersonType")
        };

        // Intenta convertir desde el valor de la API al enum
        public static bool TryFromApiValue(string apiValue, out BooksDictionaries.PersonType result)
        {
            switch (apiValue?.Trim())
            {
                case string s when s.Equals("LEGAL_ENTITY", System.StringComparison.OrdinalIgnoreCase):
                    result = BooksDictionaries.PersonType.LegalEntity; return true;
                case string s when s.Equals("NATURAL_PERSON", System.StringComparison.OrdinalIgnoreCase):
                    result = BooksDictionaries.PersonType.NaturalPerson; return true;
                default:
                    result = default; return false;
            }
        }

        public static BooksDictionaries.PersonType FromApiValue(string apiValue)
        {
            if (TryFromApiValue(apiValue, out var result)) return result;
            throw new System.ArgumentException($"Unsupported PersonType apiValue: '{apiValue}'", nameof(apiValue));
        }

        // Nombre amigable para UI
        public static string GetDisplayName(this BooksDictionaries.PersonType value) => value switch
        {
            BooksDictionaries.PersonType.LegalEntity => "Persona Jurídica",
            BooksDictionaries.PersonType.NaturalPerson => "Persona Natural",
            _ => value.ToString()
        };
    }

    // Extensiones para TaxRegime
    public static class TaxRegimeExtensions
    {
        // Valor exacto esperado por la API
        public static string ToApiValue(this BooksDictionaries.TaxRegime value) => value switch
        {
            BooksDictionaries.TaxRegime.NonVatResponsible => "NON_VAT_RESPONSIBLE",
            BooksDictionaries.TaxRegime.VatResponsible => "VAT_RESPONSIBLE",
            _ => throw new System.ArgumentOutOfRangeException(nameof(value), value, "Unsupported TaxRegime")
        };

        public static bool TryFromApiValue(string apiValue, out BooksDictionaries.TaxRegime result)
        {
            switch (apiValue?.Trim())
            {
                case string s when s.Equals("NON_VAT_RESPONSIBLE", System.StringComparison.OrdinalIgnoreCase):
                    result = BooksDictionaries.TaxRegime.NonVatResponsible; return true;
                case string s when s.Equals("VAT_RESPONSIBLE", System.StringComparison.OrdinalIgnoreCase):
                    result = BooksDictionaries.TaxRegime.VatResponsible; return true;
                default:
                    result = default; return false;
            }
        }

        public static BooksDictionaries.TaxRegime FromApiValue(string apiValue)
        {
            if (TryFromApiValue(apiValue, out var result)) return result;
            throw new System.ArgumentException($"Unsupported TaxRegime apiValue: '{apiValue}'", nameof(apiValue));
        }

        public static string GetDisplayName(this BooksDictionaries.TaxRegime value) => value switch
        {
            BooksDictionaries.TaxRegime.NonVatResponsible => "No responsable de IVA",
            BooksDictionaries.TaxRegime.VatResponsible => "Responsable de IVA",
            _ => value.ToString()
        };
    }
}
