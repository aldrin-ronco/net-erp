using Caliburn.Micro;
using Models.Inventory;
using NetErp.UserControls.ItemDimensionEditor.DTO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NetErp.UserControls.ItemDimensionEditor.ViewModels
{
    /// <summary>
    /// Modal de búsqueda de productos. Recibe término inicial, dispara filtro,
    /// muestra grilla de resultados. ENTER selecciona row resaltada; ESC cancela.
    /// </summary>
    public class ItemSearchModalViewModel : Screen
    {
        private const int DebounceMs = 250;
        private const int MinChars = 2;

        private readonly Func<ItemSearchFilters, CancellationToken, Task<IReadOnlyList<ItemGraphQLModel>>> _searchProvider;
        private CancellationTokenSource? _debounceCts;

        public ItemSearchModalViewModel(
            Func<ItemSearchFilters, CancellationToken, Task<IReadOnlyList<ItemGraphQLModel>>> searchProvider,
            string initialTerm)
        {
            _searchProvider = searchProvider ?? throw new ArgumentNullException(nameof(searchProvider));
            FilterTerm = initialTerm ?? string.Empty;
            DialogWidth = 800;
            DialogHeight = 540;
        }

        public double DialogWidth { get; set; }
        public double DialogHeight { get; set; }

        public ObservableCollection<ItemGraphQLModel> Items { get; } = [];

        private string _filterTerm = string.Empty;
        public string FilterTerm
        {
            get => _filterTerm;
            set
            {
                if (_filterTerm == value) return;
                _filterTerm = value ?? string.Empty;
                NotifyOfPropertyChange();
                _ = DebouncedSearchAsync();
            }
        }

        private bool _filterFocus;
        public bool FilterFocus { get => _filterFocus; set { _filterFocus = value; NotifyOfPropertyChange(); } }

        private bool _isBusy;
        public bool IsBusy { get => _isBusy; set { if (_isBusy != value) { _isBusy = value; NotifyOfPropertyChange(); } } }

        public ItemGraphQLModel? Highlighted
        {
            get;
            set { if (field != value) { field = value; NotifyOfPropertyChange(nameof(Highlighted)); NotifyOfPropertyChange(nameof(CanAccept)); } }
        }

        public ItemGraphQLModel? SelectedItem { get; private set; }

        public bool CanAccept => Highlighted != null;

        protected override async Task OnInitializeAsync(CancellationToken cancellationToken)
        {
            await base.OnInitializeAsync(cancellationToken);
            FilterFocus = true;
            await ExecuteSearchAsync();
        }

        private async Task DebouncedSearchAsync()
        {
            _debounceCts?.Cancel();
            _debounceCts = new CancellationTokenSource();
            var token = _debounceCts.Token;
            try
            {
                await Task.Delay(DebounceMs, token);
                if (token.IsCancellationRequested) return;
                await ExecuteSearchAsync();
            }
            catch (TaskCanceledException) { }
        }

        private async Task ExecuteSearchAsync()
        {
            if (!string.IsNullOrEmpty(_filterTerm) && _filterTerm.Length < MinChars)
            {
                Items.Clear();
                return;
            }
            IsBusy = true;
            try
            {
                var filters = new ItemSearchFilters { Term = _filterTerm.Trim(), ExactMatchOnly = false };
                var result = await _searchProvider(filters, CancellationToken.None);
                Items.Clear();
                foreach (var i in result) Items.Add(i);
                if (Items.Count > 0) Highlighted = Items[0];
            }
            finally { IsBusy = false; }
        }

        public void OnFilterKeyDown(KeyEventArgs e)
        {
            if (e == null) return;
            if (e.Key == Key.Down && Items.Count > 0)
            {
                e.Handled = true;
                Highlighted = Items[0];
            }
            else if (e.Key == Key.Enter)
            {
                e.Handled = true;
                _ = AcceptAsync();
            }
            else if (e.Key == Key.Escape)
            {
                e.Handled = true;
                _ = CancelAsync();
            }
        }

        public void OnGridKeyDown(KeyEventArgs e, object selected)
        {
            if (e == null) return;
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                if (selected is ItemGraphQLModel item) Highlighted = item;
                _ = AcceptAsync();
            }
            else if (e.Key == Key.Escape)
            {
                e.Handled = true;
                _ = CancelAsync();
            }
        }

        public void Pick(ItemGraphQLModel item)
        {
            if (item == null) return;
            Highlighted = item;
            _ = AcceptAsync();
        }

        public async Task AcceptAsync()
        {
            if (!CanAccept) return;
            SelectedItem = Highlighted;
            await TryCloseAsync(true);
        }

        public Task CancelAsync() => TryCloseAsync(false);
    }
}
