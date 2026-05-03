using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace NetErp.Helpers.Shortcuts
{
    /// <summary>
    /// Attached behavior que conecta los comandos canónicos de
    /// <see cref="AppCommands"/> a métodos del DataContext del UserControl
    /// donde se aplique. Resolución por convención:
    ///
    /// <para>
    /// Para un comando <c>X</c> (e.g. <c>AppCommands.New</c>), busca en el
    /// DataContext: <c>X</c>, <c>XAsync</c>, <c>XCommand</c>. Para CanExecute
    /// busca <c>CanX</c> property (bool). Si no existe, asume true.
    /// </para>
    ///
    /// <para>
    /// Uso XAML:
    /// <code>
    /// &lt;UserControl ...
    ///     xmlns:s="clr-namespace:NetErp.Helpers.Shortcuts"&gt;
    ///     &lt;UserControl.Resources&gt;
    ///         &lt;s:CommandList x:Key="MyShortcuts"&gt;
    ///             &lt;x:Static Member="s:AppCommands.New"/&gt;
    ///             &lt;x:Static Member="s:AppCommands.Edit"/&gt;
    ///         &lt;/s:CommandList&gt;
    ///     &lt;/UserControl.Resources&gt;
    ///     &lt;Grid s:Shortcuts.Enabled="{StaticResource MyShortcuts}"/&gt;
    /// &lt;/UserControl&gt;
    /// </code>
    /// </para>
    /// </summary>
    public static class Shortcuts
    {
        public static readonly DependencyProperty EnabledProperty =
            DependencyProperty.RegisterAttached(
                "Enabled",
                typeof(CommandList),
                typeof(Shortcuts),
                new PropertyMetadata(null, OnEnabledChanged));

        public static CommandList? GetEnabled(DependencyObject obj) =>
            (CommandList?)obj.GetValue(EnabledProperty);

        public static void SetEnabled(DependencyObject obj, CommandList? value) =>
            obj.SetValue(EnabledProperty, value);

        private static readonly DependencyProperty HandlerProperty =
            DependencyProperty.RegisterAttached("Handler", typeof(KeyEventHandler), typeof(Shortcuts));

        /// <summary>
        /// Lista de teclas (separadas por coma) que un descendant declara
        /// reclamadas por su lógica local. El handler global de
        /// <see cref="Shortcuts"/> NO interceptará esa tecla cuando el foco
        /// (e.OriginalSource) esté dentro del subtree marcado.
        ///
        /// <para>Uso: <c>s:Shortcuts.ReservedKeys="Escape,Delete"</c> en un
        /// child UC con handlers locales (PreviewKeyDown / IsCancel button /
        /// editor edit-mode). Permite que el comportamiento local corra primero
        /// sin que el opt-in del parent lo pise.</para>
        ///
        /// <para>Aplica solo al primer ancestor (más cercano al foco) que tenga
        /// la propiedad seteada. No bubble — opt-in explícito por nodo.</para>
        /// </summary>
        public static readonly DependencyProperty ReservedKeysProperty =
            DependencyProperty.RegisterAttached("ReservedKeys", typeof(string), typeof(Shortcuts),
                new PropertyMetadata(null));

        public static string? GetReservedKeys(DependencyObject obj) => (string?)obj.GetValue(ReservedKeysProperty);
        public static void SetReservedKeys(DependencyObject obj, string? value) => obj.SetValue(ReservedKeysProperty, value);

        private static void OnEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not UIElement element) return;

            // Cleanup previo: remover handler PreviewKeyDown.
            if (element.GetValue(HandlerProperty) is KeyEventHandler oldHandler)
            {
                element.PreviewKeyDown -= oldHandler;
                element.SetValue(HandlerProperty, null);
            }

            if (e.NewValue is not CommandList list || list.Count == 0) return;

            // Tunnel handler — corre antes que controles internos (DataGrid F2,
            // TextEdit ESC, etc) consuman la tecla.
            KeyEventHandler handler = (sender, args) =>
            {
                if (args.Handled) return;
                foreach (RoutedUICommand cmd in list)
                {
                    foreach (InputGesture gesture in cmd.InputGestures)
                    {
                        if (gesture is KeyGesture kg && kg.Matches(sender, args))
                        {
                            // Defensa: si el foco está dentro de un descendant que
                            // reclama esta tecla vía s:Shortcuts.ReservedKeys, no
                            // interceptar — dejar que el handler local corra.
                            if (IsKeyReservedByDescendant(args.OriginalSource as DependencyObject, sender as DependencyObject, args.Key))
                                return;
                            object? dc = GetDataContext(sender);
                            if (dc == null) return;
                            if (!CanExecuteOn(dc, cmd)) return;
                            ExecuteOn(dc, cmd);
                            args.Handled = true;
                            return;
                        }
                    }
                }
            };
            element.PreviewKeyDown += handler;
            element.SetValue(HandlerProperty, handler);
        }

        private static bool CanExecuteOn(object dc, RoutedUICommand cmd)
        {
            Type t = dc.GetType();
            PropertyInfo? canProp = t.GetProperty("Can" + cmd.Name, BindingFlags.Public | BindingFlags.Instance);
            if (canProp != null && canProp.PropertyType == typeof(bool))
                return (bool)(canProp.GetValue(dc) ?? false);

            PropertyInfo? cmdProp = t.GetProperty(cmd.Name + "Command", BindingFlags.Public | BindingFlags.Instance);
            if (cmdProp != null && cmdProp.GetValue(dc) is ICommand ic)
                return ic.CanExecute(null);

            return t.GetMethod(cmd.Name + "Async", BindingFlags.Public | BindingFlags.Instance) != null
                || t.GetMethod(cmd.Name, BindingFlags.Public | BindingFlags.Instance) != null;
        }

        private static void ExecuteOn(object dc, RoutedUICommand cmd)
        {
            Type t = dc.GetType();
            string name = cmd.Name;

            MethodInfo? mi = t.GetMethod(name + "Async", BindingFlags.Public | BindingFlags.Instance);
            if (mi != null && mi.GetParameters().Length == 0)
            {
                object? result = mi.Invoke(dc, null);
                if (result is Task task) _ = task;
                return;
            }

            mi = t.GetMethod(name, BindingFlags.Public | BindingFlags.Instance);
            if (mi != null && mi.GetParameters().Length == 0)
            {
                mi.Invoke(dc, null);
                return;
            }

            PropertyInfo? pi = t.GetProperty(name + "Command", BindingFlags.Public | BindingFlags.Instance);
            if (pi != null && pi.GetValue(dc) is ICommand ic && ic.CanExecute(null))
                ic.Execute(null);
        }

        private static object? GetDataContext(object? sender)
        {
            return sender switch
            {
                FrameworkElement fe => fe.DataContext,
                _ => null
            };
        }

        /// <summary>
        /// Sube desde <paramref name="from"/> hasta justo antes de
        /// <paramref name="optInRoot"/>. Si encuentra un nodo con
        /// <see cref="ReservedKeysProperty"/> conteniendo <paramref name="key"/>,
        /// retorna true → el parent debe bypassear el shortcut.
        /// </summary>
        private static bool IsKeyReservedByDescendant(DependencyObject? from, DependencyObject? optInRoot, Key key)
        {
            if (from == null || optInRoot == null) return false;
            string keyName = key.ToString();
            DependencyObject? node = from;
            while (node != null && !ReferenceEquals(node, optInRoot))
            {
                string? reserved = node.GetValue(ReservedKeysProperty) as string;
                if (!string.IsNullOrWhiteSpace(reserved))
                {
                    foreach (string token in reserved.Split(','))
                    {
                        if (string.Equals(token.Trim(), keyName, StringComparison.OrdinalIgnoreCase))
                            return true;
                    }
                }
                node = System.Windows.Media.VisualTreeHelper.GetParent(node)
                       ?? (node is FrameworkElement fe ? fe.Parent : null);
            }
            return false;
        }
    }

    /// <summary>Lista declarable en XAML para opt-in de comandos.</summary>
    public class CommandList : List<RoutedUICommand> { }
}
