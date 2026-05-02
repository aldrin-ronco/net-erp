using Amazon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dictionaries
{
    /// <summary>
    /// Wrapper para regiones AWS. <c>EnumValue</c> coincide con el enum
    /// <c>AwsRegion</c> de la API GraphQL (formato <c>US_EAST_1</c>); <c>Display</c>
    /// se muestra en UI; <c>Endpoint</c> resuelve el SDK de AWS.
    /// </summary>
    public record AwsRegionItem(string EnumValue, string Display, RegionEndpoint Endpoint);

    public static class GlobalDictionaries
    {
        /// <summary>
        /// Regiones AWS soportadas. Keys (<c>EnumValue</c>) deben coincidir con
        /// el enum <c>AwsRegion</c> del schema GraphQL.
        /// </summary>
        public static IReadOnlyList<AwsRegionItem> AwsRegions { get; } =
        [
            new("US_EAST_1",    "US East (N. Virginia)",   RegionEndpoint.USEast1),
            new("US_EAST_2",    "US East (Ohio)",          RegionEndpoint.USEast2),
            new("US_WEST_1",    "US West (N. California)", RegionEndpoint.USWest1),
            new("US_WEST_2",    "US West (Oregon)",        RegionEndpoint.USWest2),
            new("CA_CENTRAL_1", "Canada (Central)",        RegionEndpoint.CACentral1),
            new("EU_CENTRAL_1", "EU (Frankfurt)",          RegionEndpoint.EUCentral1),
            new("EU_WEST_1",    "EU (Ireland)",            RegionEndpoint.EUWest1),
            new("EU_WEST_2",    "EU (London)",             RegionEndpoint.EUWest2),
            new("EU_WEST_3",    "EU (Paris)",              RegionEndpoint.EUWest3),
        ];

        /// <summary>Resuelve el <c>RegionEndpoint</c> del SDK desde el enum value.</summary>
        public static RegionEndpoint GetAwsRegionEndpoint(string enumValue)
        {
            AwsRegionItem? item = AwsRegions.FirstOrDefault(r => r.EnumValue == enumValue);
            return item?.Endpoint ?? throw new KeyNotFoundException($"AWS region '{enumValue}' no soportada.");
        }

        public static readonly Dictionary<string, string> DateControlTypeDictionary = new()
        {
            {"CONTROLLED_BY_CASH_REGISTER", "FECHA DE TRANSACCIÓN CONTROLADA POR CUADRE DE CAJA" },
            {"SERVER_DATE", "FECHA DEL SERVIDOR" },
            {"LOCAL_DATE", "FECHA DEL EQUIPO LOCAL" },
            {"OPEN_DATE", "FECHA ABIERTA A MANIPULACIÓN" }
        };

        public static readonly Dictionary<string, string> CostCenterStatusDictionary = new()
        {
            {"ACTIVE", "ACTIVO" },
            {"READ_ONLY", "SOLO LECTURA" },
            {"INACTIVE", "INACTIVO" }
        };

        public static readonly Dictionary<int, string> MonthsDictionary = new()
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

        public static readonly Dictionary<char, string> DateFilterOptionsDictionary = new()
        {
            {'=',"=" },
            {'>',">" },
            {'<',"<" },
            {'B',"ENTRE" } //BETWEEN
        };

    }//fin 
}
