using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NetErp.UserControls
{
    public partial class EmptyStateView : UserControl
    {
        // Pack scheme requires Application initialized — eager init at type load
        // can throw if EmptyStateView static ctor runs before App. Lazy defers until
        // first access (post-startup). Force pack scheme registration as safety.
        private static readonly Lazy<ImageSource> DefaultImageLazy = new(() =>
        {
            _ = System.IO.Packaging.PackUriHelper.UriSchemePack;
            return new BitmapImage(
                new Uri("pack://application:,,,/NetErp;component/Resources/Images/vecteezy_desert-landscape-404-error-page-concept-illustration-flat_9007135.jpg"));
        });
        private static ImageSource DefaultImage => DefaultImageLazy.Value;

        private bool _isExecuting;

        #region Dependency Properties

        public static readonly DependencyProperty ImageSourceProperty =
            DependencyProperty.Register(nameof(ImageSource), typeof(ImageSource), typeof(EmptyStateView),
                new PropertyMetadata(default(ImageSource)));

        public static readonly DependencyProperty ImageWidthProperty =
            DependencyProperty.Register(nameof(ImageWidth), typeof(double), typeof(EmptyStateView),
                new PropertyMetadata(280.0));

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(EmptyStateView),
                new PropertyMetadata("No hay registros"));

        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register(nameof(Message), typeof(string), typeof(EmptyStateView),
                new PropertyMetadata("Cree un nuevo registro para comenzar"));

        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(EmptyStateView),
                new PropertyMetadata(null, OnCommandChanged));

        public static readonly DependencyProperty ButtonTextProperty =
            DependencyProperty.Register(nameof(ButtonText), typeof(string), typeof(EmptyStateView),
                new PropertyMetadata("Nuevo"));

        public static readonly DependencyProperty ContextInfoProperty =
            DependencyProperty.Register(nameof(ContextInfo), typeof(string), typeof(EmptyStateView),
                new PropertyMetadata(null));

        public static readonly DependencyProperty ButtonVisibleProperty =
            DependencyProperty.Register(nameof(ButtonVisible), typeof(bool), typeof(EmptyStateView),
                new PropertyMetadata(true));

        public static readonly DependencyProperty ButtonIconProperty =
            DependencyProperty.Register(nameof(ButtonIcon), typeof(ImageSource), typeof(EmptyStateView),
                new PropertyMetadata(null));

        public static readonly DependencyProperty HasPermissionProperty =
            DependencyProperty.Register(nameof(HasPermission), typeof(bool), typeof(EmptyStateView),
                new PropertyMetadata(true, OnHasPermissionChanged));

        #endregion

        #region Properties

        public ImageSource ImageSource
        {
            get => (ImageSource)GetValue(ImageSourceProperty);
            set => SetValue(ImageSourceProperty, value);
        }

        public double ImageWidth
        {
            get => (double)GetValue(ImageWidthProperty);
            set => SetValue(ImageWidthProperty, value);
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public string Message
        {
            get => (string)GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }

        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        public string ButtonText
        {
            get => (string)GetValue(ButtonTextProperty);
            set => SetValue(ButtonTextProperty, value);
        }

        public bool ButtonVisible
        {
            get => (bool)GetValue(ButtonVisibleProperty);
            set => SetValue(ButtonVisibleProperty, value);
        }

        public ImageSource? ButtonIcon
        {
            get => (ImageSource?)GetValue(ButtonIconProperty);
            set => SetValue(ButtonIconProperty, value);
        }

        public string? ContextInfo
        {
            get => (string?)GetValue(ContextInfoProperty);
            set => SetValue(ContextInfoProperty, value);
        }

        public bool HasPermission
        {
            get => (bool)GetValue(HasPermissionProperty);
            set => SetValue(HasPermissionProperty, value);
        }

        #endregion

        public EmptyStateView()
        {
            InitializeComponent();
            // Asignar default después de InitializeComponent — si caller no provee
            // ImageSource, usar imagen embebida. Lazy garantiza pack scheme listo.
            if (ImageSource == null)
            {
                try { ImageSource = DefaultImage; }
                catch { /* recurso ausente — permite que el control funcione sin imagen */ }
            }
        }

        private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not EmptyStateView view) return;

            if (e.OldValue is ICommand oldCommand)
                oldCommand.CanExecuteChanged -= view.OnCanExecuteChanged;

            if (e.NewValue is ICommand newCommand)
                newCommand.CanExecuteChanged += view.OnCanExecuteChanged;

            view.UpdateButtonEnabled();
        }

        private void OnCanExecuteChanged(object? sender, EventArgs e)
        {
            UpdateButtonEnabled();
        }

        private void UpdateButtonEnabled()
        {
            if (ActionButton == null) return;
            ActionButton.IsEnabled = !_isExecuting && HasPermission && (Command?.CanExecute(null) ?? false);
        }

        private static void OnHasPermissionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is EmptyStateView view)
                view.UpdateButtonEnabled();
        }

        private async void OnActionButtonClick(object sender, RoutedEventArgs e)
        {
            if (_isExecuting || Command == null || !Command.CanExecute(null)) return;

            _isExecuting = true;
            ActionButton.IsEnabled = false;
            try
            {
                // Ceder el UI thread para que WPF renderice el estado deshabilitado.
                // ContextIdle (prioridad 3) se ejecuta DESPUÉS de que Render (prioridad 7)
                // y todas las operaciones de mayor prioridad hayan completado.
                await Dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.ContextIdle);

                if (Command is DevExpress.Mvvm.AsyncCommand asyncCommand)
                    await asyncCommand.ExecuteAsync(null);
                else
                    Command.Execute(null);
            }
            finally
            {
                _isExecuting = false;
                UpdateButtonEnabled();
            }
        }
    }
}
