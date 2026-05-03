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
        /// Slot caller para captura entre UND y el botón "Dimensiones". Típicamente
        /// costo unitario, precio, descuento.
        /// </summary>
        public object AdditionalContent
        {
            get => GetValue(AdditionalContentProperty);
            set => SetValue(AdditionalContentProperty, value);
        }

        public static readonly DependencyProperty TrailingContentProperty =
            DependencyProperty.Register(nameof(TrailingContent), typeof(object), typeof(ItemDimensionEditorView),
                new PropertyMetadata(null));

        /// <summary>
        /// Slot caller después del botón "Dimensiones". Típicamente botón de confirmación
        /// (Agregar). Permite que el foco fluya: cost → Dimensiones → Agregar.
        /// </summary>
        public object TrailingContent
        {
            get => GetValue(TrailingContentProperty);
            set => SetValue(TrailingContentProperty, value);
        }

        public static readonly DependencyProperty ShowSearchBadgeProperty =
            DependencyProperty.Register(nameof(ShowSearchBadge), typeof(bool), typeof(ItemDimensionEditorView),
                new PropertyMetadata(false));

        /// <summary>
        /// Cuando true, muestra badge ⌃F overlay sobre el campo de búsqueda al
        /// mantener Ctrl. Consumer activa esto sólo si declara `s:AppCommands.Search`
        /// en su CommandList. Default false.
        /// </summary>
        public bool ShowSearchBadge
        {
            get => (bool)GetValue(ShowSearchBadgeProperty);
            set => SetValue(ShowSearchBadgeProperty, value);
        }

        public ItemDimensionEditorView()
        {
            InitializeComponent();
            PreviewKeyDown += OnPreviewKeyDown;
        }

        /// <summary>
        /// ESC con item seleccionado dentro del UC = cancelar selección + limpiar
        /// búsqueda (equivalente a presionar X). Marca handled para impedir que
        /// el ESC propague al contenedor (que típicamente cierra la vista vía
        /// botón con <c>IsCancel="True"</c>).
        /// </summary>
        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Escape) return;
            if (DataContext is not ViewModels.ItemDimensionEditorViewModel vm) return;
            if (!vm.HasSelectedItem) return;
            vm.ClearSearch();
            e.Handled = true;
        }
    }
}
