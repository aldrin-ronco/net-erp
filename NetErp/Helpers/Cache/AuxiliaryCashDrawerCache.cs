using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using Models.Treasury;
using NetErp.Helpers.GraphQLQueryBuilder;
using Newtonsoft.Json.Linq;
using QueryBuilder = NetErp.Helpers.GraphQLQueryBuilder.GraphQLQueryBuilder;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Helpers.Cache
{
    /// <summary>
    /// Cache global de cajas auxiliares (<c>Parent != null</c>). Filtra cliente-side porque
    /// <c>CashDrawerFilters</c> no soporta un flag "hasParent". Volumen esperado: &lt;100 por compañía.
    /// </summary>
    public class AuxiliaryCashDrawerCache : IEntityCache<CashDrawerGraphQLModel>, IBatchLoadableCache,
        IHandle<TreasuryCashDrawerCreateMessage>,
        IHandle<TreasuryCashDrawerUpdateMessage>,
        IHandle<TreasuryCashDrawerDeleteMessage>
    {
        private readonly IRepository<CashDrawerGraphQLModel> _service;
        private readonly Lock _lock = new();

        private readonly ObservableCollection<CashDrawerGraphQLModel> _items = [];
        public ReadOnlyObservableCollection<CashDrawerGraphQLModel> Items { get; }
        public bool IsInitialized { get; private set; }

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadQuery = new(() =>
        {
            var fields = FieldSpec<PageType<CashDrawerGraphQLModel>>
                .Create()
                .SelectList(x => x.Entries, entries => entries
                    .Field(x => x.Id)
                    .Field(x => x.Name)
                    .Field(x => x.IsPettyCash)
                    .Select(x => x.Parent, nested => nested.Field(p => p.Id))
                    .Select(x => x.CostCenter, nested => nested
                        .Field(c => c.Id)
                        .Field(c => c.Name))
                )
                .Build();

            var paginationParam = new GraphQLQueryParameter("pagination", "Pagination");
            var filtersParam = new GraphQLQueryParameter("filters", "CashDrawerFilters");
            var fragment = new GraphQLQueryFragment("auxiliaryCashDrawersPage", [paginationParam, filtersParam], fields, "PageResponse");
            var query = new QueryBuilder([fragment]).GetQuery();

            return (fragment, query);
        });

        public AuxiliaryCashDrawerCache(
            IRepository<CashDrawerGraphQLModel> service,
            IEventAggregator eventAggregator)
        {
            _service = service;
            eventAggregator.SubscribeOnUIThread(this);
            Items = new ReadOnlyObservableCollection<CashDrawerGraphQLModel>(_items);
        }

        public async Task EnsureLoadedAsync()
        {
            if (IsInitialized) return;

            try
            {
                var (fragment, query) = _loadQuery.Value;
                var variables = new GraphQLVariables()
                    .For(fragment, "pagination", new { PageSize = -1 })
                    .For(fragment, "filters", new { isPettyCash = false })
                    .Build();

                var result = await _service.GetPageAsync(query, variables);
                var entries = ((System.Collections.Generic.IEnumerable<CashDrawerGraphQLModel>)result.Entries).Where(IsAuxiliary).ToList();

                UiDispatcher.Invoke(() =>
                {
                    lock (_lock)
                    {
                        _items.Clear();
                        foreach (var item in entries)
                            _items.Add(item);
                        IsInitialized = true;
                    }
                });
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }

        #region IBatchLoadableCache

        public GraphQLQueryFragment LoadFragment => _loadQuery.Value.Fragment;

        public void ApplyVariables(GraphQLVariables variables, GraphQLQueryFragment batchFragment)
        {
            variables.For(batchFragment, "pagination", new { PageSize = -1 });
            variables.For(batchFragment, "filters", new { isPettyCash = false });
        }

        public void PopulateFromBatchResponse(JToken data)
        {
            var page = data.ToObject<PageType<CashDrawerGraphQLModel>>();
            if (page == null) return;

            UiDispatcher.Invoke(() =>
            {
                lock (_lock)
                {
                    _items.Clear();
                    foreach (var item in page.Entries.Where(IsAuxiliary))
                        _items.Add(item);
                    IsInitialized = true;
                }
            });
        }

        #endregion

        public void Clear()
        {
            UiDispatcher.Invoke(() =>
            {
                lock (_lock)
                {
                    _items.Clear();
                    IsInitialized = false;
                }
            });
        }

        public void Add(CashDrawerGraphQLModel item)
        {
            UiDispatcher.Invoke(() =>
            {
                lock (_lock)
                {
                    if (!_items.Any(x => x.Id == item.Id))
                        _items.Add(item);
                }
            });
        }

        public void Update(CashDrawerGraphQLModel item)
        {
            UiDispatcher.Invoke(() =>
            {
                lock (_lock)
                {
                    var existing = _items.FirstOrDefault(x => x.Id == item.Id);
                    if (existing != null)
                    {
                        var index = _items.IndexOf(existing);
                        _items[index] = item;
                    }
                }
            });
        }

        public void Remove(int id)
        {
            UiDispatcher.Invoke(() =>
            {
                lock (_lock)
                {
                    var item = _items.FirstOrDefault(x => x.Id == id);
                    if (item != null)
                        _items.Remove(item);
                }
            });
        }

        #region IHandle Implementations

        private static bool IsAuxiliary(CashDrawerGraphQLModel drawer) => drawer.Parent is not null;

        public Task HandleAsync(TreasuryCashDrawerCreateMessage message, CancellationToken cancellationToken)
        {
            var created = message.CreatedCashDrawer?.Entity;
            if (created != null && IsAuxiliary(created)) Add(created);
            return Task.CompletedTask;
        }

        public Task HandleAsync(TreasuryCashDrawerUpdateMessage message, CancellationToken cancellationToken)
        {
            var updated = message.UpdatedCashDrawer?.Entity;
            if (updated == null) return Task.CompletedTask;

            if (IsAuxiliary(updated))
            {
                var existing = _items.FirstOrDefault(x => x.Id == updated.Id);
                if (existing != null) Update(updated);
                else Add(updated);
            }
            else
            {
                Remove(updated.Id);
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(TreasuryCashDrawerDeleteMessage message, CancellationToken cancellationToken)
        {
            if (message.DeletedCashDrawer?.DeletedId > 0)
                Remove(message.DeletedCashDrawer.DeletedId.Value);
            return Task.CompletedTask;
        }

        #endregion
    }
}
