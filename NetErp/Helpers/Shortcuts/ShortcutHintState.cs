using Caliburn.Micro;
using System.Windows;
using System.Windows.Input;

namespace NetErp.Helpers.Shortcuts
{
    /// <summary>
    /// Singleton notificable que expone el estado de la tecla Ctrl. Las vistas
    /// pueden bindearse a <see cref="IsCtrlHeld"/> para mostrar hints de
    /// shortcuts (ej. <c>⌃N</c> en botones) sólo mientras el usuario mantiene
    /// presionado Ctrl.
    /// </summary>
    public class ShortcutHintState : PropertyChangedBase
    {
        public static ShortcutHintState Instance { get; } = new();

        public bool IsCtrlHeld
        {
            get;
            private set { if (field != value) { field = value; NotifyOfPropertyChange(); } }
        }

        private ShortcutHintState() { }

        /// <summary>
        /// Engancha listeners globales a <c>Window</c> para detectar Ctrl. Llamar
        /// una vez al arranque de la app desde <c>App.OnStartup</c> o equivalente.
        /// </summary>
        public static void Initialize()
        {
            EventManager.RegisterClassHandler(typeof(Window),
                UIElement.PreviewKeyDownEvent, new KeyEventHandler(OnPreviewKeyDown), handledEventsToo: true);
            EventManager.RegisterClassHandler(typeof(Window),
                UIElement.PreviewKeyUpEvent, new KeyEventHandler(OnPreviewKeyUp), handledEventsToo: true);
            // Lost-focus de la app puede dejar el flag colgado (alt-tab con Ctrl down).
            EventManager.RegisterClassHandler(typeof(Window),
                UIElement.LostKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(OnLostFocus), handledEventsToo: true);
        }

        private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
                Instance.IsCtrlHeld = true;
        }

        private static void OnPreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
                Instance.IsCtrlHeld = false;
        }

        private static void OnLostFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            // Si la app pierde foco mientras Ctrl está down, reset (no recibimos KeyUp).
            if ((Keyboard.Modifiers & ModifierKeys.Control) == 0)
                Instance.IsCtrlHeld = false;
        }
    }
}
