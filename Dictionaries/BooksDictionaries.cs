using System.Collections.Generic;

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
        public enum SecuenceTypeEnum { M, D, Undefined }

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

    }
}
