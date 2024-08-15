using System.Collections.Generic;

namespace Dictionaries
{
    public static class BillingDictionaries
    {
        /// <summary>
        /// Naturalezas de las cuentas contables
        /// </summary>
        public static Dictionary<string, string> BillingDocumentSequenceAuthorizationKindDictionary = new Dictionary<string, string>()
        {
            { "AUTORIZA", "AUTORIZA" },
            { "HABILITA", "HABILITA" }
        };

        public static Dictionary<string, string> BillingDocumentSequenceLabelDictionary = new Dictionary<string, string>()
        {
            { "FACTURA DE VENTA", "FACTURA DE VENTA" },
            { "DOCUMENTO EQUIVALENTE", "DOCUMENTO EQUIVALENTE" },
            { "SISTEMA POS", "SISTEMA POS" }
        };

        public static Dictionary<string, string> BillingDocumentSequenceTitleLabelDictionary = new Dictionary<string, string>()
        {
            { "AUT. DE NUMERACIÓN DE FACTURACIÓN", "AUT. DE NUMERACIÓN DE FACTURACIÓN" }
        };

        public static Dictionary<string, string> BillingDocumentSequenceAuthorizationTypeDictionary = new Dictionary<string, string>()
        {
            { "PAPEL", "PAPEL" },
            { "POR COMPUTADOR", "POR COMPUTADOR" },
            { "MAQUINA REGISTRADORA POS", "MAQUINA REGISTRADORA POS" },
            { "ELECTRONICA", "ELECTRONICA" }
        };
    }
}
