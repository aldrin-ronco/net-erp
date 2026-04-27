using System.Collections.Generic;

namespace NetErp.UserControls.ItemDimensionEditor.DTO
{
    /// <summary>
    /// Filtros que el UC pasa al <c>SearchProvider</c> inyectado por el caller.
    /// </summary>
    public sealed class ItemSearchFilters
    {
        /// <summary>Texto digitado por el usuario.</summary>
        public string Term { get; init; } = string.Empty;

        /// <summary>
        /// True cuando el UC pide resolución única por EAN/reference/code (ENTER directo o lectura barcode).
        /// El caller debe armar query que devuelva 0 o 1 resultado y filtrar por esos campos.
        /// </summary>
        public bool ExactMatchOnly { get; init; }

        /// <summary>
        /// Filtros contextuales adicionales que el caller pueda querer pasar a la query
        /// (ej. solo billable, solo activos, restricción por proveedor, etc.).
        /// </summary>
        public IReadOnlyDictionary<string, object>? Custom { get; init; }
    }
}
