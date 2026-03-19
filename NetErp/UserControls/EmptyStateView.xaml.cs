using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NetErp.UserControls
{
    public partial class EmptyStateView : UserControl
    {
        private static readonly ImageSource DefaultImage = new BitmapImage(
            new Uri("pack://application:,,,/NetErp;component/Resources/Images/vecteezy_desert-landscape-404-error-page-concept-illustration-flat_9007135.jpg"));

        public static readonly DependencyProperty ImageSourceProperty =
            DependencyProperty.Register(nameof(ImageSource), typeof(ImageSource), typeof(EmptyStateView),
                new PropertyMetadata(DefaultImage));

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
                new PropertyMetadata(null));

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

        public EmptyStateView()
        {
            InitializeComponent();
        }
    }
}
