using Models.Inventory;
using System;
using System.Collections.Generic;

namespace NetErp.UserControls.ItemDimensionEditor.DTO
{
    /// <summary>
    /// Resultado emitido por el UC <c>ItemDimensionEditor</c> cada vez que la captura de
    /// cantidad y dimensiones es válida. El caller arma con esto su payload del documento.
    /// El evento se redispara cada vez que el usuario edita los datos del UC mientras
    /// éste sigue interactivo (hasta que el caller llama <c>Reset()</c>).
    /// </summary>
    public sealed class LineCompletedEventArgs : EventArgs
    {
        /// <summary>Item completo seleccionado por el usuario.</summary>
        public ItemGraphQLModel Item { get; init; } = new();

        /// <summary>
        /// Cantidad total de la línea. Para base es la cantidad capturada;
        /// para dimensionados es la suma o cuenta de dimensiones.
        /// </summary>
        public decimal TotalQuantity { get; init; }

        /// <summary>Lotes preseleccionados (vacío si no aplica).</summary>
        public IReadOnlyList<LotDraft> Lots { get; init; } = [];

        /// <summary>Seriales preseleccionados (vacío si no aplica).</summary>
        public IReadOnlyList<SerialDraft> Serials { get; init; } = [];

        /// <summary>Tallas preseleccionadas (vacío si no aplica).</summary>
        public IReadOnlyList<SizeDraft> Sizes { get; init; } = [];

        /// <summary>Sentido del movimiento: I (entrada) u O (salida).</summary>
        public DimensionDirection Direction { get; init; }
    }
}
