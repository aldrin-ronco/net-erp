using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Config
{
    public static class UICustom
    {
        /// <summary>
        /// Personalizaciones posibles en UI
        /// Tales como :
        /// Tamaño de fuente para etiquetas, Tamaño de fuente para contenidos
        /// Etc..
        /// </summary>

        public static int LabelFontSize { get; set; } = 13;
        public static int ControlFontSize { get; set; } = 13;
    }
}
