using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dictionaries
{
    public class InventoriesDictionaries
    {
        public static Dictionary<string, string> MeasurementUnitTypeDictionary = new Dictionary<string, string>()
        {
            {"LENGTH", "Longitud" },
            {"MASS", "Masa" },
            {"VOLUME", "Volumen" },
            {"AREA", "Área" },
            {"TIME", "Tiempo" },
            {"UNIT", "Unidad" }
        };

        public static Dictionary<char, string> KardexFlowDictionary = new Dictionary<char, string>()
        {
            {'I', "ENTRADA" },
            {'O', "SALIDA" }
        };

        /// <summary>
        /// Abecedario para posibles prefijos  Inventory_Item_Type 
        /// </summary>
        public static Dictionary<char, string> prefixcharDictionary = new Dictionary<char, string>()
        {
            {'A', "A" },
            {'B', "B" },
            {'C', "C" },
            {'D', "D" },
            {'E', "E" },
            {'G', "G" },
            {'H', "H" },
            {'I', "I" },
            {'J', "J" },
            {'K', "K" },
            {'L', "L" },
            {'M', "M" },
            {'N', "N" },
            {'Ñ', "Ñ" },
            {'O', "O" },
            {'P', "P" },
            {'Q', "Q" },
            {'R', "R" },
            {'S', "S" },
            {'T', "T" },
            {'U', "U" },
            {'V', "V" },
            {'W', "W" },
            {'X', "X" },
            {'Y', "Y" },
            {'Z', "Z" }
        };
    }
}
