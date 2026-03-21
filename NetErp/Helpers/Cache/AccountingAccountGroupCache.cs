using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using Models.Books;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Models.Global.GraphQLResponseTypes;
using QueryBuilder = NetErp.Helpers.GraphQLQueryBuilder.GraphQLQueryBuilder;

namespace NetErp.Helpers.Cache
{
    public class AccountingAccountGroupCache : IEntityCache<AccountingAccountGroupGraphQLModel>,
        IHandle<AccountingAccountGroupUpdateMessage>
    {
        private readonly IRepository<AccountingAccountGroupGraphQLModel> _service;
        private readonly object _lock = new();

        private readonly ObservableCollection<AccountingAccountGroupGraphQLModel> _items = [];
        public ReadOnlyObservableCollection<AccountingAccountGroupGraphQLModel> Items { get; }
        public bool IsInitialized { get; private set; }

        public AccountingAccountGroupCache(
            IRepository<AccountingAccountGroupGraphQLModel> service,
            IEventAggregator eventAggregator)
        {
            _service = service;
            eventAggregator.SubscribeOnUIThread(this);
            Items = new ReadOnlyObservableCollection<AccountingAccountGroupGraphQLModel>(_items);
        }

        public async Task EnsureLoadedAsync()
        {
            if (IsInitialized) return;

            try
            {
                var query = BuildQuery();
                dynamic variables = new ExpandoObject();
                variables.pageResponsePagination = new ExpandoObject();
                variables.pageResponsePagination.pageSize = -1;

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
                var existing = _items.FirstOrDefault(x => x.Id == accountingAccountGroup.Id);
                if (existing != null)
                    Update(accountingAccountGroup);
                else
                    Add(accountingAccountGroup);
            }
            return Task.CompletedTask;
        }

        #endregion

        #region Query Builder

        private string BuildQuery()
        {
            var fields = FieldSpec<PageType<AccountingAccountGroupGraphQLModel>>
                .Create()
                .Field(o => o.TotalEntries)
                .SelectList(it => it.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Field(e => e.Key)
                    .SelectList(e => e.Accounts, accounts => accounts
                        .Field(a => a.Id)))
                .Build();

            var paginationParam = new GraphQLQueryParameter("pagination", "Pagination");
            var fragment = new GraphQLQueryFragment("accountingAccountGroupsPage", [paginationParam], fields, "PageResponse");
            return new QueryBuilder([fragment]).GetQuery();
        }

        #endregion
    }
}
