using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using Models.Books;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Models.Global.GraphQLResponseTypes;
using QueryBuilder = NetErp.Helpers.GraphQLQueryBuilder.GraphQLQueryBuilder;

namespace NetErp.Helpers.Cache
{
    public class CtasRestVtasAccountingAccountGroupCache : IEntityCache<AccountingAccountGroupGraphQLModel>,
        IHandle<AccountingAccountGroupUpdateMessage>
    {
        private readonly IRepository<AccountingAccountGroupGraphQLModel> _service;
        private readonly Lock _lock = new();

        private readonly ObservableCollection<AccountingAccountGroupGraphQLModel> _items = [];
        public ReadOnlyObservableCollection<AccountingAccountGroupGraphQLModel> Items { get; }
        public bool IsInitialized { get; private set; }

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadQuery = new(() =>
        {
            var fields = FieldSpec<PageType<AccountingAccountGroupGraphQLModel>>
                .Create()
                .SelectList(it => it.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Field(e => e.Key)
                    .SelectList(e => e.Accounts, cat => cat
                        .Field(c => c.Id)
                        .Field(c => c.Name)
                        .Field(c => c.Code)
                    )
                )
                .Field(o => o.PageNumber)
                .Field(o => o.PageSize)
                .Field(o => o.TotalPages)
                .Field(o => o.TotalEntries)
                .Build();

            var paginationParam = new GraphQLQueryParameter("pagination", "Pagination");
            var filtersParam = new GraphQLQueryParameter("filters", "AccountingAccountGroupFilters");
            var fragment = new GraphQLQueryFragment("accountingAccountGroupsPage", [paginationParam, filtersParam], fields, "PageResponse");
            var query = new QueryBuilder([fragment]).GetQuery();

            return (fragment, query);
        });

        public CtasRestVtasAccountingAccountGroupCache(
            IRepository<AccountingAccountGroupGraphQLModel> service,
            IEventAggregator eventAggregator)
        {
            _service = service;
            Items = new ReadOnlyObservableCollection<AccountingAccountGroupGraphQLModel>(_items);
            eventAggregator.SubscribeOnUIThread(this);
        }

        public async Task EnsureLoadedAsync()
        {
            if (IsInitialized) return;

            try
            {
                var (fragment, query) = _loadQuery.Value;
                var variables = new GraphQLVariables()
                    .For(fragment, "filters", new { Key = "CTAS_RETS_VTAS" })
                    .Build();

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

        public void Clear()
        {
            lock (_lock)
            {
                _items.Clear();
                IsInitialized = false;
            }
        }

        public void Add(AccountingAccountGroupGraphQLModel item)
        {
            lock (_lock)
            {
                if (!_items.Any(x => x.Id == item.Id))
                    _items.Add(item);
            }
        }

        public void Update(AccountingAccountGroupGraphQLModel item)
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

        public Task HandleAsync(AccountingAccountGroupUpdateMessage message, CancellationToken cancellationToken)
        {
            var accountingAccountGroup = message.UpsertAccountingAccountGroup?.Entity ?? message.UpdateAccountingAccountGroup;

            if (accountingAccountGroup != null)
            {
                bool isVTAS = !string.IsNullOrEmpty(accountingAccountGroup.Key) && accountingAccountGroup.Key == "CTAS_RETS_VTAS";
                var existing = _items.FirstOrDefault(x => x.Id == accountingAccountGroup.Id);

                if (isVTAS)
                {
                    if (existing != null)
                        Update(accountingAccountGroup);
                    else
                        Add(accountingAccountGroup);
                }
                else if (existing != null)
                {
                    // Si ya no es auxiliar, remover del cache
                    Remove(accountingAccountGroup.Id);
                }
            }
            return Task.CompletedTask;
        }

        #endregion
    }
}
