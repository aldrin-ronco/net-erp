using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace NetErp.UserControls.ItemDimensionEditor.Views
{
    public partial class ItemDimensionEditorView : UserControl
    {
        public static readonly DependencyProperty AdditionalContentProperty =
            DependencyProperty.Register(nameof(AdditionalContent), typeof(object), typeof(ItemDimensionEditorView),
                new PropertyMetadata(null));

        /// <summary>
        /// Slot inyectable por el caller para añadir captura adicional (costo, precio,
        /// descuento, botón de confirmación). El contenido se renderiza al final del
        /// panel info (junto al botón "Dimensiones").
        /// </summary>
        public object AdditionalContent
        {
            get => GetValue(AdditionalContentProperty);
            set => SetValue(AdditionalContentProperty, value);
        }

        public ItemDimensionEditorView()
        {
            InitializeComponent();
        }
    }
}
