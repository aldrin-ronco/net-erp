using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dictionaries
{
    public static class GlobalDictionaries
    {
        /// <summary>
        /// Regiones para la cuentas AWS SES
        /// </summary>
        public static Dictionary<string, string> AwsSesRegionDictionary = new Dictionary<string, string>()
        {
            { "us-east-1", "EE.UU. Este (Norte de Virginia)" },
            { "us-east-2", "EE.UU. Este (Ohio)" },
            { "us-west-1", "EE.UU. Oeste (Norte de California)"},
            { "us-west-2", "EE.UU. Oeste (Oregón)"},
            { "ca-central-1", "Canadá (Central)"},
            { "eu-central-1","Europa (Fráncfort)"},
            { "eu-west-1", "Europa (Irlanda)"},
            { "eu-west-2", "Europa (Londres)"},
            { "eu-west-3", "Europa (París)"}
        };

        public static Dictionary<string, string> DateControlTypeDictionary = new Dictionary<string, string>()
        {
            {"FC","FECHA DE TRANSACCIÓN CONTROLADA POR CUADRE DE CAJA" },
            {"FS","FECHA DEL SERVIDOR" },
            {"FL","FECHA DEL EQUIPO LOCAL" },
            {"FA","FECHA ABIERTA A MANIPULACIÓN" }
        };

        public static Dictionary<char, string> CostCenterStateDictionary = new Dictionary<char, string>()
        {
            {'A',"ACTIVO" },
            {'C',"SOLO CONSULTAS" },
            {'I',"INACTIVO" }
        };

        public static Dictionary<int, string> MonthsDictionary = new Dictionary<int, string>()
        {
            {1, "Enero"},
            {2, "Febrero"},
            {3, "Marzo"},
            {4, "Abril"},
            {5, "Mayo"},
            {6, "Junio"},
            {7, "Julio"},
            {8, "Agosto"},
            {9, "Septiembre"},
            {10, "Octubre"},
            {11, "Noviembre"},
            {12, "Diciembre"},
        };

        public static Dictionary<char, string> DateFilterOptionsDictionary = new Dictionary<char, string>()
        {
            {'=',"=" },
            {'>',">" },
            {'<',"<" },
            {'B',"ENTRE" } //BETWEEN
        };

    }//fin 
}
