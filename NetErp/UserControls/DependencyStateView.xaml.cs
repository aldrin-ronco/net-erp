using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NetErp.Helpers;

namespace NetErp.UserControls
{
    public partial class DependencyStateView : UserControl
    {
        private static readonly ImageSource DefaultImage = new BitmapImage(
            new Uri("pack://application:,,,/NetErp;component/Resources/Images/vecteezy_data-information-word-not-found-concept-illustration-flat_16349592.jpg"));

        #region Dependency Properties

        public static readonly DependencyProperty ImageSourceProperty =
            DependencyProperty.Register(nameof(ImageSource), typeof(ImageSource), typeof(DependencyStateView),
                new PropertyMetadata(DefaultImage));

        public static readonly DependencyProperty ImageWidthProperty =
            DependencyProperty.Register(nameof(ImageWidth), typeof(double), typeof(DependencyStateView),
                new PropertyMetadata(240.0));

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(DependencyStateView),
                new PropertyMetadata("Configuración requerida"));

        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register(nameof(Message), typeof(string), typeof(DependencyStateView),
                new PropertyMetadata("Este módulo requiere configuración previa"));

        public static readonly DependencyProperty DependenciesProperty =
            DependencyProperty.Register(nameof(Dependencies), typeof(IEnumerable<DependencyItem>), typeof(DependencyStateView),
                new PropertyMetadata(null));

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

        public IEnumerable<DependencyItem>? Dependencies
        {
            get => (IEnumerable<DependencyItem>?)GetValue(DependenciesProperty);
            set => SetValue(DependenciesProperty, value);
        }

        #endregion

        public DependencyStateView()
        {
            InitializeComponent();
        }
    }
}
