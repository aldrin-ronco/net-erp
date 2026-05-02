using System.Windows.Input;

namespace NetErp.Helpers.Shortcuts
{
    /// <summary>
    /// Comandos canónicos de la aplicación con sus key gestures estándar. Las
    /// vistas opt-in vía <see cref="Shortcuts.SetEnabled"/> y el behavior
    /// resuelve el método correspondiente en el ViewModel por convención
    /// (mismo nombre que el comando, opcionalmente con sufijo <c>Async</c>).
    /// </summary>
    public static class AppCommands
    {
        public static readonly RoutedUICommand New = new(
            "Nuevo", nameof(New), typeof(AppCommands),
            [new KeyGesture(Key.N, ModifierKeys.Control)]);

        public static readonly RoutedUICommand Edit = new(
            "Editar", nameof(Edit), typeof(AppCommands),
            [new KeyGesture(Key.F2)]);

        public static readonly RoutedUICommand Save = new(
            "Guardar", nameof(Save), typeof(AppCommands),
            [new KeyGesture(Key.S, ModifierKeys.Control)]);

        public static readonly RoutedUICommand Delete = new(
            "Eliminar", nameof(Delete), typeof(AppCommands),
            [new KeyGesture(Key.Delete)]);

        public static readonly RoutedUICommand Refresh = new(
            "Refrescar", nameof(Refresh), typeof(AppCommands),
            [new KeyGesture(Key.F5)]);

        public static readonly RoutedUICommand Search = new(
            "Buscar", nameof(Search), typeof(AppCommands),
            [new KeyGesture(Key.F, ModifierKeys.Control)]);

        public static readonly RoutedUICommand Close = new(
            "Cerrar", nameof(Close), typeof(AppCommands),
            [new KeyGesture(Key.Escape)]);

        public static readonly RoutedUICommand Confirm = new(
            "Confirmar", nameof(Confirm), typeof(AppCommands),
            [new KeyGesture(Key.Enter, ModifierKeys.Control)]);

        public static readonly RoutedUICommand Post = new(
            "Postear", nameof(Post), typeof(AppCommands),
            [new KeyGesture(Key.F9)]);

        public static readonly RoutedUICommand Annul = new(
            "Anular", nameof(Annul), typeof(AppCommands),
            [new KeyGesture(Key.X, ModifierKeys.Control | ModifierKeys.Shift)]);
    }
}
