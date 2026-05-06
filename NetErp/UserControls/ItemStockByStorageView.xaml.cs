using Caliburn.Micro;
using Common.Interfaces;
using Models.Inventory;
using NetErp.Helpers.GraphQLQueryBuilder;
using NetErp.UserControls.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.UserControls
{
    /// <summary>
    /// User control de solo lectura. Muestra una banda horizontal de tarjetas
    /// con las existencias de un item agrupadas por (storage, dimension).
    ///
    /// Modos de uso (no excluyentes):
    ///   - Push: bind <see cref="Stocks"/> con un IEnumerable&lt;StockTotalGraphQLModel&gt; ya cargado por el caller.
    ///   - Pull: bind <see cref="ItemId"/> y deja <see cref="AutoLoad"/>=true. El UC consulta stockTotalsPage.
    ///
    /// Si <see cref="Stocks"/> trae datos, prevalece sobre el pull.
    /// </summary>
    public partial class ItemStockByStorageView : UserControl
    {
        public static readonly DependencyProperty StocksProperty =
            DependencyProperty.Register(nameof(Stocks),
                typeof(IEnumerable<StockTotalGraphQLModel>),
                typeof(ItemStockByStorageView),
                new PropertyMetadata(null, OnStocksChanged));

        public static readonly DependencyProperty ItemIdProperty =
            DependencyProperty.Register(nameof(ItemId),
                typeof(int),
                typeof(ItemStockByStorageView),
                new PropertyMetadata(0, OnItemIdChanged));

        public static readonly DependencyProperty MeasurementUnitAbbreviationProperty =
            DependencyProperty.Register(nameof(MeasurementUnitAbbreviation),
                typeof(string),
                typeof(ItemStockByStorageView),
                new PropertyMetadata(string.Empty, OnUnitChanged));

        public static readonly DependencyProperty AutoLoadProperty =
            DependencyProperty.Register(nameof(AutoLoad),
                typeof(bool),
                typeof(ItemStockByStorageView),
                new PropertyMetadata(true));

        public static readonly DependencyProperty HighlightedStorageIdsProperty =
            DependencyProperty.Register(nameof(HighlightedStorageIds),
                typeof(IEnumerable<int>),
                typeof(ItemStockByStorageView),
                new PropertyMetadata(null, OnHighlightChanged));

        public static readonly DependencyProperty AllowFractionProperty =
            DependencyProperty.Register(nameof(AllowFraction),
                typeof(bool),
                typeof(ItemStockByStorageView),
                new PropertyMetadata(true, OnAllowFractionChanged));

        private static readonly DependencyPropertyKey IsLoadingPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(IsLoading), typeof(bool),
                typeof(ItemStockByStorageView), new PropertyMetadata(false));
        public static readonly DependencyProperty IsLoadingProperty = IsLoadingPropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey HasStocksPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(HasStocks), typeof(bool),
                typeof(ItemStockByStorageView), new PropertyMetadata(false));
        public static readonly DependencyProperty HasStocksProperty = HasStocksPropertyKey.DependencyProperty;

        public IEnumerable<StockTotalGraphQLModel>? Stocks
        {
            get => (IEnumerable<StockTotalGraphQLModel>?)GetValue(StocksProperty);
            set => SetValue(StocksProperty, value);
        }

        public int ItemId
        {
            get => (int)GetValue(ItemIdProperty);
            set => SetValue(ItemIdProperty, value);
        }

        public string MeasurementUnitAbbreviation
        {
            get => (string)GetValue(MeasurementUnitAbbreviationProperty);
            set => SetValue(MeasurementUnitAbbreviationProperty, value);
        }

        public bool AutoLoad
        {
            get => (bool)GetValue(AutoLoadProperty);
            set => SetValue(AutoLoadProperty, value);
        }

        public IEnumerable<int>? HighlightedStorageIds
        {
            get => (IEnumerable<int>?)GetValue(HighlightedStorageIdsProperty);
            set => SetValue(HighlightedStorageIdsProperty, value);
        }

        public bool AllowFraction
        {
            get => (bool)GetValue(AllowFractionProperty);
            set => SetValue(AllowFractionProperty, value);
        }

        public bool IsLoading
        {
            get => (bool)GetValue(IsLoadingProperty);
            private set => SetValue(IsLoadingPropertyKey, value);
        }

        public bool HasStocks
        {
            get => (bool)GetValue(HasStocksProperty);
            private set => SetValue(HasStocksPropertyKey, value);
        }

        public ObservableCollection<ItemStockDisplayDto> DisplayItems { get; } = [];

        private CancellationTokenSource? _cts;
        private IEnumerable<StockTotalGraphQLModel>? _lastSource;

        public ItemStockByStorageView()
        {
            InitializeComponent();
            Unloaded += OnUnloaded;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        private static void OnStocksChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((ItemStockByStorageView)d).OnStocksOrItemChanged(stocksJustSet: true);

        private static void OnItemIdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((ItemStockByStorageView)d).OnStocksOrItemChanged(stocksJustSet: false);

        private static void OnUnitChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ItemStockByStorageView view = (ItemStockByStorageView)d;
            if (DesignerProperties.GetIsInDesignMode(view)) return;
            view.PopulateFromCurrentStocks();
        }

        private static void OnHighlightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ItemStockByStorageView view = (ItemStockByStorageView)d;
            if (DesignerProperties.GetIsInDesignMode(view)) return;
            view.PopulateFromCurrentStocks();
        }

        private static void OnAllowFractionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ItemStockByStorageView view = (ItemStockByStorageView)d;
            if (DesignerProperties.GetIsInDesignMode(view)) return;
            view.PopulateFromCurrentStocks();
        }

        private void OnStocksOrItemChanged(bool stocksJustSet)
        {
            if (DesignerProperties.GetIsInDesignMode(this)) return;

            if (Stocks != null && Stocks.Any())
            {
                _lastSource = Stocks;
                Repopulate();
                return;
            }

            if (stocksJustSet)
            {
                _lastSource = null;
                DisplayItems.Clear();
                HasStocks = false;
                return;
            }

            if (ItemId > 0 && AutoLoad)
            {
                _ = LoadFromApiAsync();
            }
            else
            {
                _lastSource = null;
                DisplayItems.Clear();
                HasStocks = false;
            }
        }

        private void PopulateFromCurrentStocks()
        {
            Repopulate();
        }

        private void Repopulate()
        {
            DisplayItems.Clear();
            IEnumerable<StockTotalGraphQLModel> source = _lastSource ?? Stocks ?? [];
            string unit = string.IsNullOrWhiteSpace(MeasurementUnitAbbreviation) ? "UND" : MeasurementUnitAbbreviation;
            string fmt = AllowFraction ? "N2" : "N0";
            HashSet<int> highlight = HighlightedStorageIds is null
                ? []
                : [.. HighlightedStorageIds];
            foreach (StockTotalGraphQLModel s in source.Where(x => x.Quantity > 0))
            {
                bool showDim = !string.Equals(s.Dimension, "BASE", StringComparison.OrdinalIgnoreCase);
                int storageId = s.Storage?.Id ?? 0;
                bool isHighlighted = storageId > 0 && highlight.Contains(storageId);
                DisplayItems.Add(new ItemStockDisplayDto(
                    StorageName: s.Storage?.Name ?? string.Empty,
                    QuantityText: $"{s.Quantity.ToString(fmt)} {unit}",
                    DimensionLabel: MapDimensionLabel(s.Dimension),
                    ShowDimension: showDim,
                    IsHighlighted: isHighlighted));
            }
            HasStocks = DisplayItems.Count > 0;
        }

        private async Task LoadFromApiAsync()
        {
            CancellationTokenSource? old = _cts;
            CancellationTokenSource cts = new();
            _cts = cts;
            old?.Cancel();
            old?.Dispose();
            int requestedItemId = ItemId;
            try
            {
                IsLoading = true;
                IRepository<StockTotalGraphQLModel> repo = IoC.Get<IRepository<StockTotalGraphQLModel>>();
                var (fragment, query) = ItemStockByStorageQueries.StockTotalsByItem.Value;

                dynamic filters = new ExpandoObject();
                filters.itemId = requestedItemId;

                object variables = new GraphQLVariables()
                    .For(fragment, "pagination", new { Page = 1, PageSize = -1 })
                    .For(fragment, "filters", filters)
                    .Build();

                PageType<StockTotalGraphQLModel> page = await repo.GetPageAsync(query, variables, cts.Token);

                if (cts.IsCancellationRequested || ItemId != requestedItemId) return;
                _lastSource = page.Entries;
                Repopulate();
            }
            catch (OperationCanceledException) { }
            catch (Exception)
            {
                DisplayItems.Clear();
                HasStocks = false;
            }
            finally
            {
                if (ReferenceEquals(_cts, cts))
                {
                    IsLoading = false;
                    _cts = null;
                    cts.Dispose();
                }
            }
        }

        private static string MapDimensionLabel(string? dim) => dim?.ToUpperInvariant() switch
        {
            "SIZE" => "Talla",
            "LOT" => "Lote",
            "SERIAL" => "Serial",
            _ => dim ?? string.Empty
        };
    }

    public sealed record ItemStockDisplayDto(
        string StorageName,
        string QuantityText,
        string DimensionLabel,
        bool ShowDimension,
        bool IsHighlighted);
}
