using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using Models.Books;
using NetErp.Helpers.GraphQLQueryBuilder;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Models.Global.GraphQLResponseTypes;
using QueryBuilder = NetErp.Helpers.GraphQLQueryBuilder.GraphQLQueryBuilder;

namespace NetErp.Helpers.Cache
{
    public class NotAnnulledAccountingSourceCache : IEntityCache<AccountingSourceGraphQLModel>, IBatchLoadableCache,
         IHandle<AccountingSourceUpdateMessage>,
         IHandle<AccountingSourceCreateMessage>,
         IHandle<AccountingSourceDeleteMessage>
    {
        private readonly IRepository<AccountingSourceGraphQLModel> _service;
        private readonly Lock _lock = new();

        private readonly ObservableCollection<AccountingSourceGraphQLModel> _items = [];
        public ReadOnlyObservableCollection<AccountingSourceGraphQLModel> Items { get; }
        public bool IsInitialized { get; private set; }

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadQuery = new(() =>
        {
            var fields = FieldSpec<PageType<AccountingSourceGraphQLModel>>
                .Create()
                .SelectList(it => it.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                )
                .Field(o => o.PageNumber)
                .Field(o => o.PageSize)
                .Field(o => o.TotalPages)
                .Field(o => o.TotalEntries)
                .Build();

            var paginationParam = new GraphQLQueryParameter("pagination", "Pagination");
            var filtersParam = new GraphQLQueryParameter("filters", "AccountingSourceFilters");
            var fragment = new GraphQLQueryFragment("accountingSourcesPage", [paginationParam, filtersParam], fields, "PageResponse");
            var query = new QueryBuilder([fragment]).GetQuery();

            return (fragment, query);
        });

        public NotAnnulledAccountingSourceCache(
            IRepository<AccountingSourceGraphQLModel> service,
            IEventAggregator eventAggregator)
        {
            _service = service;
            Items = new ReadOnlyObservableCollection<AccountingSourceGraphQLModel>(_items);
            eventAggregator.SubscribeOnUIThread(this);
        }

        public async Task EnsureLoadedAsync()
        {
            if (IsInitialized) return;

            try
            {
                var (fragment, query) = _loadQuery.Value;
                var variables = new GraphQLVariables()
                    .For(fragment, "filters", new { Annulment = false })
                    .Build();

                var result = await _service.GetPageAsync(query, variables);

                UiDispatcher.Invoke(() =>
                {
                    lock (_lock)
                    {
                        _items.Clear();
                        foreach (var item in result.Entries)
                        {
                            _items.Add(item);
                        }
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
            variables.For(batchFragment, "filters", new { Annulment = false });
        }

        public void PopulateFromBatchResponse(JToken data)
        {
            var page = data.ToObject<PageType<AccountingSourceGraphQLModel>>();
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

        public void Add(AccountingSourceGraphQLModel item)
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

        public void Update(AccountingSourceGraphQLModel item)
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

        public Task HandleAsync(AccountingSourceDeleteMessage message, CancellationToken cancellationToken)
        {
            if (message.DeletedAccountingSource?.DeletedId > 0)
            {
                Remove(message.DeletedAccountingSource.DeletedId.Value);
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(AccountingSourceCreateMessage message, CancellationToken cancellationToken)
        {
            if (message.CreatedAccountingSource != null)
            {
                Add(message.CreatedAccountingSource.Entity);
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(AccountingSourceUpdateMessage message, CancellationToken cancellationToken)
        {
            var entity = message.UpdatedAccountingSource?.Entity;

            if (entity != null)
            {
                var existing = _items.FirstOrDefault(x => x.Id == entity.Id);

                if (existing != null)
                    Update(entity);
                else
                    Add(entity);
            }
            return Task.CompletedTask;
        }

        #endregion
    }
}
