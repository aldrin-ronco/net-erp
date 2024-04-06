namespace NetErp.UserControls
{
    using Common.Extensions;
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;

    /// <summary>
    /// Interaction logic for GridPaging.xaml.
    /// </summary>
    public partial class GridPaging
    {
        #region Dependency properties Declarations

        /// <summary>
        /// Total Count.
        /// </summary>
        public static readonly DependencyProperty TotalCountProperty;

        /// <summary>
        /// Page Index.
        /// </summary>
        public static readonly DependencyProperty PageIndexProperty;

        /// <summary>
        /// Page Size.
        /// </summary>
        public static readonly DependencyProperty PageSizeProperty;

        /// <summary>
        /// Dependency command property declaration.
        /// </summary>
        public static readonly DependencyProperty ChangedIndexCommandProperty;

        /// <summary>
        /// Response time
        /// </summary>
        public static readonly DependencyProperty ResponseTimeProperty;

        /// <summary>
        /// Gets or sets TotalRow.
        /// </summary>
        /// <value>
        /// The total row.
        /// </value>
        public int TotalCount
        {
            get
            {
                return (int)GetValue(GridPaging.TotalCountProperty);
            }

            set
            {
                SetValue(GridPaging.TotalCountProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets ActualPaging.
        /// </summary>
        /// <value>
        /// The actual paging.
        /// </value>
        public int PageIndex
        {
            get
            {
                return (int)GetValue(GridPaging.PageIndexProperty);
            }

            set
            {
                SetValue(GridPaging.PageIndexProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets PageSize.
        /// </summary>
        /// <value>
        /// The page size.
        /// </value>
        public int PageSize
        {
            get
            {
                return (int)GetValue(GridPaging.PageSizeProperty);
            }

            set
            {
                if ((int)GetValue(PageSizeProperty) != value)
                    SetValue(GridPaging.PageSizeProperty, value);
            }
        }

        public string ResponseTime
        {
            get
            {
                return (string)GetValue(ResponseTimeProperty);
            }
            set
            {
                SetValue(ResponseTimeProperty, value);
            }
        }

        #endregion

        #region Static Constructor. dEclaration of Dependency properties

        /// <summary>
        /// Initializes static members of the <see cref="GridPaging"/> class.
        /// </summary>
        static GridPaging()
        {
            UIPropertyMetadata md = new UIPropertyMetadata(0, PropertyTotalCountChanged);
            GridPaging.TotalCountProperty = DependencyProperty.Register("TotalCount", typeof(int), typeof(GridPaging), md);
            UIPropertyMetadata md1 = new UIPropertyMetadata(0, PropertyPageIndexChanged);
            GridPaging.PageIndexProperty = DependencyProperty.Register("PageIndex", typeof(int), typeof(GridPaging), md1);
            UIPropertyMetadata md2 = new UIPropertyMetadata(0, PropertyPageSizeChanged);
            GridPaging.PageSizeProperty = DependencyProperty.Register("PageSize", typeof(int), typeof(GridPaging), md2);
            UIPropertyMetadata md3 = new UIPropertyMetadata(string.Empty, PropertyResponseTimeChanged);
            GridPaging.ResponseTimeProperty = DependencyProperty.Register("ResponseTime", typeof(string), typeof(GridPaging), md3);
            // Registro del Comando.
            ChangedIndexCommandProperty =
            DependencyProperty.Register("ChangedIndexCommand", typeof(ICommand), typeof(GridPaging), new UIPropertyMetadata(null));
        }

        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="GridPaging"/> class.
        /// </summary>
        public GridPaging()
        {
            InitializeComponent();
            this.TotalCount = 0;
            this.PageIndex = 1;
            this.cbPageSize.SelectedIndex = 1; // Default 100
            this.IsControlVisible = true;
            this.HasNextPage = false;
            this.HasPreviousPage = false;
            this.ResponseTime = "";
        }

        #endregion

        #region Dependency Command Declaration

        /// <summary>
        /// Gets or sets NextIndexCommand.
        /// </summary>
        /// <value>
        /// The next index command.
        /// </value>
        public ICommand ChangedIndexCommand
        {
            get { return (ICommand)GetValue(ChangedIndexCommandProperty); }
            set { SetValue(ChangedIndexCommandProperty, value); }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether IsVisible.
        /// </summary>
        /// <value>
        /// The is visible.
        /// </value>
        public bool IsControlVisible
        {
            get { return this.Visibility == Visibility.Visible; }
            set { this.Visibility = value ? Visibility.Visible : Visibility.Collapsed; }
        }

        /// <summary>
        /// Gets TotalPages.
        /// </summary>
        /// <value>
        /// The total pages.
        /// </value>
        public int TotalPages
        {
            get
            {
                // Calcule el número de paginas necesarias
                if (this.PageSize > 0)
                {
                    var tc = this.TotalCount / this.PageSize;
                    tc = tc * this.PageSize < this.TotalCount ? tc + 1 : tc;
                    return tc;
                }

                return 1;
            }
        }

        /// <summary>
        /// Gets a value indicating whether HasPreviousPage.
        /// </summary>
        /// <value>
        /// The has previous page.
        /// </value>
        public bool HasPreviousPage
        {
            get { return btnFirst.IsEnabled; }
            internal set { btnFirst.IsEnabled = btnPrevious.IsEnabled = value; }
        }

        /// <summary>
        /// Gets a value indicating whether HasNextPage.
        /// </summary>
        /// <value>
        /// The has next page.
        /// </value>
        public bool HasNextPage
        {
            get { return btnLast.IsEnabled; }
            internal set { btnLast.IsEnabled = btnNext.IsEnabled = value; }
        }

        #endregion

        #region Metodos Publicos

        /// <summary>
        /// Lleva el page index a 1.
        /// </summary>
        public void ResetPageIndex()
        {
            this.PageIndex = 1;
        }

        #endregion

        #region Refactoring Configuracion de Elementos

        /// <summary>
        /// Config Visibility, and pageSize.
        /// </summary>
        /// <param name="gp">
        /// The gp.
        /// </param>
        private static void ConfigureValoresInternos(GridPaging gp)
        {
            // Set the Total de paginas....
            gp.lTotalPagina.Content = gp.TotalPages;

            // Set the pageSize control
            foreach (ComboBoxItem comboBoxItem in gp.cbPageSize.Items)
            {
                int cbi = Convert.ToInt32(comboBoxItem.Content);
                if (cbi == gp.PageSize)
                {
                    gp.cbPageSize.SelectedItem = comboBoxItem;
                    break;
                }
            }

            // if the setted value in Page size is not in list, return to original value.
            ComboBoxItem sel = (ComboBoxItem)gp.cbPageSize.SelectedItem;
            gp.PageSize = Convert.ToInt32(sel.Content);

            // Set the visibility of Pagination Buttons.
            gp.ButtonGrid.Visibility = gp.TotalCount > gp.PageSize ?
                Visibility.Visible :
                Visibility.Hidden;

            // Calculate the HasNextPage and previous page
            gp.HasPreviousPage = gp.PageIndex > 1;
            gp.HasNextPage = gp.TotalPages > gp.PageIndex;
        }

        /// <summary>
        /// Execute the command if it is assigned.
        /// </summary>
        private void ExecuteCommandChangeIndex()
        {
            // Test if the command index is asigned.
            if (this.ChangedIndexCommand != null)
            {
                this.ChangedIndexCommand.Execute(null);
            }
        }

        #endregion

        #region Eventos controles

        /// <summary>
        /// Change the Page Size Property control.
        /// This make that PageIndex go to 1.
        /// </summary>
        /// <param name="d">
        /// The d.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        private static void PropertyPageSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            GridPaging gp = (GridPaging)d;
            ConfigureValoresInternos(gp);
        }

        /// <summary>
        /// Evento para actualizar el page index.
        /// </summary>
        /// <param name="d">
        /// The d.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        private static void PropertyPageIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Update Total Count label....
            GridPaging gp = (GridPaging)d;
            int actualPage = (int)e.NewValue;
            gp.lPagina.Content = actualPage;
            ConfigureValoresInternos(gp);
        }

        /// <summary>
        /// Evento cuando cambia la cantidad total de Registros.
        /// </summary>
        /// <param name="d">
        /// The d.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        private static void PropertyTotalCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Update Total Count label....
            GridPaging gp = (GridPaging)d;
            if ((int)e.NewValue == 0)
            {
                gp.lTotal.Content = $"No hubieron resultados";
            }
            else
            {
                gp.lTotal.Content = $"{((int)e.NewValue > 1 ? "Se encontraron" : "Se encontró")} {string.Format("{0:N0}", e.NewValue)} {((int)e.NewValue > 1 ? "registros" : "registro")}, " +
                                    $"{ExpandElapsedTime(gp.ResponseTime, "En")}";
            }
            ConfigureValoresInternos(gp);
        }

        private static void PropertyResponseTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            GridPaging gp = (GridPaging)d;
            if (gp.TotalCount == 0)
            {
                gp.lTotal.Content = $"No hubieron resultados";
            }
            else
            {
                gp.lTotal.Content = $"{(gp.TotalCount > 1 ? "Se encontraron" : "Se encontró")} {string.Format("{0:N0}", gp.TotalCount)} {(gp.TotalCount > 1 ? "registros" : "registro")}, " +
                                    $"{ExpandElapsedTime(gp.ResponseTime, "En")}";
            }
            ConfigureValoresInternos(gp);
        }

        private static string ExpandElapsedTime(string responseTime, string prefix = "")
        {
            if (responseTime == null || string.IsNullOrEmpty(responseTime)) return "";

            var hour = Convert.ToInt32(responseTime.Substring(0, 2));
            var min = Convert.ToInt32(responseTime.Substring(3, 2));
            var sec = Convert.ToInt32(responseTime.Substring(6, 2));
            var mil = Convert.ToInt32(responseTime.Right(2));

            string hora = "", minuto = "", segundo = "", ms = "";
            // Hora
            if (hour == 0)
            {
                hora = "";
            }
            else
            {
                hora = hour > 1 ? $"{hour} horas" : $"{hour} hora";
            }
            // Minuto
            if (min == 0)
            {
                minuto = "";
            }
            else
            {
                minuto = min > 1 ? $"{min} minutos" : $"{min} minuto";
            }
            // Segundo
            if (sec == 0)
            {
                segundo = "";
            }
            else
            {
                segundo = sec > 1 ? $"{sec} segundos" : $"{sec} segundo";
            }
            // Milesimas de segundos
            if (mil == 0)
            {
                ms = "";
            }
            else
            {
                ms = $"{mil} ms";
            }

            return $"{prefix} {hora}" + (string.IsNullOrEmpty(hora) ? "" : ", ") +
                   $"{minuto}" + (string.IsNullOrEmpty(minuto) ? "" : ", ") +
                   $"{segundo}" + (string.IsNullOrEmpty(segundo) ? "" : ", ") +
                   $"{ms}".Trim();
        }

        /// <summary>
        /// Cambia la selección del PageSize.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        private void ComboBoxSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var items = e.AddedItems;
            if (items != null && items.Count > 0)
            {
                var value = ((ComboBoxItem)items[0]).Content;
                this.PageSize = Convert.ToInt32(value);
                this.PageIndex = 1;
                if (this.TotalCount > 0)
                {
                    this.ExecuteCommandChangeIndex();
                }
            }
        }

        #endregion

        #region Button events for Index Control

        /// <summary>
        /// Increment the Page Index, and invoke ChangeIndexCommand.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        private void BtnNextClick(object sender, RoutedEventArgs e)
        {
            if (this.PageIndex < this.TotalPages)
            {
                this.PageIndex++;
                this.ExecuteCommandChangeIndex();
            }
        }

        /// <summary>
        /// Increment the Page Index to the last index, and invoke ChangeIndexCommand.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        private void BtnLastClick(object sender, RoutedEventArgs e)
        {
            int page = this.TotalPages;
            this.PageIndex = page;
            this.ExecuteCommandChangeIndex();
        }

        /// <summary>
        /// Decrement the Page Index, and invoke ChangeIndexCommand.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        private void BtnPreviousClick(object sender, RoutedEventArgs e)
        {
            if (this.PageIndex > 1)
            {
                this.PageIndex--;
                this.ExecuteCommandChangeIndex();
            }
        }

        /// <summary>
        /// Go to first index, and invoke ChangeIndexCommand.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        private void BtnFirstClick(object sender, RoutedEventArgs e)
        {
            const int Page = 1;
            this.PageIndex = Page;
            this.ExecuteCommandChangeIndex();
        }

        #endregion
    }
}
