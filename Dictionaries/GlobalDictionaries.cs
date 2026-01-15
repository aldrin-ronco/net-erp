using Amazon;
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
        public static Dictionary<string, RegionEndpoint> AwsSesRegionDictionary = new Dictionary<string, RegionEndpoint>()
        {
            { "us-east-1", RegionEndpoint.USEast1 },
            { "us-east-2", RegionEndpoint.USEast2 },
            { "us-west-1", RegionEndpoint.USWest1},
            { "us-west-2", RegionEndpoint.USWest2},
            { "ca-central-1", RegionEndpoint.CACentral1},
            { "eu-central-1", RegionEndpoint.EUCentral1},
            { "eu-west-1", RegionEndpoint.EUWest1},
            { "eu-west-2", RegionEndpoint.EUWest2},
            { "eu-west-3", RegionEndpoint.EUWest3}
        };

        public static Dictionary<string, string> DateControlTypeDictionary = new Dictionary<string, string>()
        {
            {"CONTROLLED_BY_CASH_REGISTER", "FECHA DE TRANSACCIÓN CONTROLADA POR CUADRE DE CAJA" },
            {"SERVER_DATE", "FECHA DEL SERVIDOR" },
            {"LOCAL_DATE", "FECHA DEL EQUIPO LOCAL" },
            {"OPEN_DATE", "FECHA ABIERTA A MANIPULACIÓN" }
        };

        public static Dictionary<string, string> CostCenterStatusDictionary = new Dictionary<string, string>()
        {
            {"ACTIVE", "ACTIVO" },
            {"READ_ONLY", "SOLO LECTURA" },
            {"INACTIVE", "INACTIVO" }
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
