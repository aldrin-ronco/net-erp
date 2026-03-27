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
    public class AccountingBookCache : IEntityCache<AccountingBookGraphQLModel>, IBatchLoadableCache,
        IHandle<AccountingBookCreateMessage>,
        IHandle<AccountingBookUpdateMessage>,
        IHandle<AccountingBookDeleteMessage>
    {
        private readonly IRepository<AccountingBookGraphQLModel> _service;
        private readonly Lock _lock = new();

        private readonly ObservableCollection<AccountingBookGraphQLModel> _items = [];
        public ReadOnlyObservableCollection<AccountingBookGraphQLModel> Items { get; }
        public bool IsInitialized { get; private set; }

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadQuery = new(() =>
        {
            var fields = FieldSpec<PageType<AccountingBookGraphQLModel>>
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
            var filtersParam = new GraphQLQueryParameter("filters", "AccountingBookFilters");
            var fragment = new GraphQLQueryFragment("accountingBooksPage", [paginationParam, filtersParam], fields, "PageResponse");
            var query = new QueryBuilder([fragment]).GetQuery();

            return (fragment, query);
        });

        public AccountingBookCache(
            IRepository<AccountingBookGraphQLModel> service,
            IEventAggregator eventAggregator)
        {
            _service = service;
            eventAggregator.SubscribeOnUIThread(this);
            Items = new ReadOnlyObservableCollection<AccountingBookGraphQLModel>(_items);
        }

        public async Task EnsureLoadedAsync()
        {
            if (IsInitialized) return;

            try
            {
                var (fragment, query) = _loadQuery.Value;
                dynamic variables = new GraphQLVariables().Build();

                var result = await _service.GetPageAsync(query, variables);

                lock (_lock)
                {
                    _items.Clear();
                    foreach (var item in result.Entries)
                    {
                        _items.Add(item);
                    }
                    IsInitialized = true;
                }
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }

        #region IBatchLoadableCache

        public GraphQLQueryFragment LoadFragment => _loadQuery.Value.Fragment;

        public void ApplyVariables(GraphQLVariables variables, GraphQLQueryFragment batchFragment) { }

        public void PopulateFromBatchResponse(JToken data)
        {
            var page = data.ToObject<PageType<AccountingBookGraphQLModel>>();
            if (page == null) return;

            lock (_lock)
            {
                _items.Clear();
                foreach (var item in page.Entries)
                    _items.Add(item);
                IsInitialized = true;
            }
        }

        #endregion

        public void Clear()
        {
            lock (_lock)
            {
                _items.Clear();
                IsInitialized = false;
            }
        }

        public void Add(AccountingBookGraphQLModel item)
        {
            lock (_lock)
            {
                if (!_items.Any(x => x.Id == item.Id))
                    _items.Add(item);
            }
        }

        public void Update(AccountingBookGraphQLModel item)
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
        }

        public void Remove(int id)
        {
            lock (_lock)
            {
                var item = _items.FirstOrDefault(x => x.Id == id);
                if (item != null)
                    _items.Remove(item);
            }
        }

        #region IHandle Implementations

        public Task HandleAsync(AccountingBookDeleteMessage message, CancellationToken cancellationToken)
        {
            if (message.DeletedAccountingBook?.DeletedId > 0)
            {
                Remove(message.DeletedAccountingBook.DeletedId.Value);
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(AccountingBookCreateMessage message, CancellationToken cancellationToken)
        {
            if (message.CreatedAccountingBook != null)
            {
                Add(message.CreatedAccountingBook.Entity);
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(AccountingBookUpdateMessage message, CancellationToken cancellationToken)
        {
            var book = message.UpdatedAccountingBook?.Entity;

            if (book != null)
            {
                var existing = _items.FirstOrDefault(x => x.Id == book.Id);
                if (existing != null)
                    Update(book);
                else
                    Add(book);
            }
            return Task.CompletedTask;
        }

        #endregion
    }
}
