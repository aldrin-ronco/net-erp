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
    /// Cache global de bancos. Carga completa al arrancar (volumen bajo, &lt;100 por compañía).
    /// Alimenta el árbol de Treasury y los LookUp de Bank en BankAccountDetailViewModel.
    /// </summary>
    public class BankCache : IEntityCache<BankGraphQLModel>, IBatchLoadableCache,
        IHandle<BankCreateMessage>,
        IHandle<BankUpdateMessage>,
        IHandle<BankDeleteMessage>
    {
        private readonly IRepository<BankGraphQLModel> _service;
        private readonly Lock _lock = new();

        private readonly ObservableCollection<BankGraphQLModel> _items = [];
        public ReadOnlyObservableCollection<BankGraphQLModel> Items { get; }
        public bool IsInitialized { get; private set; }

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadQuery = new(() =>
        {
            var fields = FieldSpec<PageType<BankGraphQLModel>>
                .Create()
                .SelectList(x => x.Entries, entries => entries
                    .Field(x => x.Id)
                    .Field(x => x.Code)
                    .Field(x => x.PaymentMethodPrefix)
                    .Select(x => x.AccountingEntity, nested => nested
                        .Field(e => e.Id)
                        .Field(e => e.IdentificationNumber)
                        .Field(e => e.VerificationDigit)
                        .Field(e => e.SearchName)
                        .Field(e => e.CaptureType))
                )
                .Build();

            var paginationParam = new GraphQLQueryParameter("pagination", "Pagination");
            var filtersParam = new GraphQLQueryParameter("filters", "BankFilters");
            var fragment = new GraphQLQueryFragment("banksPage", [paginationParam, filtersParam], fields, "PageResponse");
            var query = new QueryBuilder([fragment]).GetQuery();

            return (fragment, query);
        });

        public BankCache(
            IRepository<BankGraphQLModel> service,
            IEventAggregator eventAggregator)
        {
            _service = service;
            eventAggregator.SubscribeOnUIThread(this);
            Items = new ReadOnlyObservableCollection<BankGraphQLModel>(_items);
        }

        public async Task EnsureLoadedAsync()
        {
            if (IsInitialized) return;

            try
            {
                var (fragment, query) = _loadQuery.Value;
                var variables = new GraphQLVariables()
                    .For(fragment, "pagination", new { PageSize = -1 })
                    .For(fragment, "filters", new { })
                    .Build();

                var result = await _service.GetPageAsync(query, variables);

                UiDispatcher.Invoke(() =>
                {
                    lock (_lock)
                    {
                        _items.Clear();
                        foreach (var item in result.Entries)
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
            variables.For(batchFragment, "filters", new { });
        }

        public void PopulateFromBatchResponse(JToken data)
        {
            var page = data.ToObject<PageType<BankGraphQLModel>>();
            if (page == null) return;

            UiDispatcher.Invoke(() =>
            {
                lock (_lock)
                {
                    _items.Clear();
                    foreach (var item in page.Entries)
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

        public void Add(BankGraphQLModel item)
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

        public void Update(BankGraphQLModel item)
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

        public Task HandleAsync(BankCreateMessage message, CancellationToken cancellationToken)
        {
            var created = message.CreatedBank?.Entity;
            if (created != null) Add(created);
            return Task.CompletedTask;
        }

        public Task HandleAsync(BankUpdateMessage message, CancellationToken cancellationToken)
        {
            var updated = message.UpdatedBank?.Entity;
            if (updated != null) Update(updated);
            return Task.CompletedTask;
        }

        public Task HandleAsync(BankDeleteMessage message, CancellationToken cancellationToken)
        {
            if (message.DeletedBank?.DeletedId > 0)
                Remove(message.DeletedBank.DeletedId.Value);
            return Task.CompletedTask;
        }

        #endregion
    }
}
