using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using Models.Treasury;
using NetErp.Helpers.GraphQLQueryBuilder;
using QueryBuilder = NetErp.Helpers.GraphQLQueryBuilder.GraphQLQueryBuilder;
using System;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Helpers.Cache
{
    public class BankAccountCache : IEntityCache<BankAccountGraphQLModel>,
        IHandle<BankAccountCreateMessage>,
        IHandle<BankAccountUpdateMessage>,
        IHandle<BankAccountDeleteMessage>
    {
        private readonly IRepository<BankAccountGraphQLModel> _service;
        private readonly object _lock = new();

        public ObservableCollection<BankAccountGraphQLModel> Items { get; } = [];
        public bool IsInitialized { get; private set; }

        public BankAccountCache(
            IRepository<BankAccountGraphQLModel> service,
            IEventAggregator eventAggregator)
        {
            _service = service;
            eventAggregator.SubscribeOnUIThread(this);
        }

        public async Task EnsureLoadedAsync()
        {
            if (IsInitialized) return;

            try
            {
                var query = BuildQuery();
                dynamic variables = new ExpandoObject();
                variables.bankAccountsPagePagination = new ExpandoObject();
                variables.bankAccountsPagePagination.pageSize = -1;
                variables.bankAccountsPageFilters = new ExpandoObject();
                variables.bankAccountsPageFilters.types = new[] { "A", "C" };

                var result = await _service.GetPageAsync(query, variables);

                lock (_lock)
                {
                    Items.Clear();
                    foreach (var item in result.Entries)
                    {
                        Items.Add(item);
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
                Items.Clear();
                IsInitialized = false;
            }
        }

        public void Add(BankAccountGraphQLModel item)
        {
            lock (_lock)
            {
                if (!Items.Any(x => x.Id == item.Id))
                    Items.Add(item);
            }
        }

        public void Update(BankAccountGraphQLModel item)
        {
            lock (_lock)
            {
                var existing = Items.FirstOrDefault(x => x.Id == item.Id);
                if (existing != null)
                {
                    var index = Items.IndexOf(existing);
                    Items[index] = item;
                }
            }
        }

        public void Remove(int id)
        {
            lock (_lock)
            {
                var item = Items.FirstOrDefault(x => x.Id == id);
                if (item != null)
                    Items.Remove(item);
            }
        }

        #region IHandle Implementations

        public Task HandleAsync(BankAccountCreateMessage message, CancellationToken cancellationToken)
        {
            if (message.CreatedBankAccount != null)
            {
                Add(message.CreatedBankAccount);
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(BankAccountUpdateMessage message, CancellationToken cancellationToken)
        {
            if (message.UpdatedBankAccount != null)
            {
                Update(message.UpdatedBankAccount);
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(BankAccountDeleteMessage message, CancellationToken cancellationToken)
        {
            if (message.DeletedBankAccount?.DeletedId > 0)
            {
                Remove(message.DeletedBankAccount.DeletedId);
            }
            return Task.CompletedTask;
        }

        #endregion

        #region Query Builder

        private string BuildQuery()
        {
            var fields = FieldSpec<PageType<BankAccountGraphQLModel>>
                .Create()
                .SelectList(x => x.Entries, entries => entries
                    .Field(x => x.Id)
                    .Field(x => x.Description)
                )
                .Build();

            var paginationParam = new GraphQLQueryParameter("pagination", "Pagination");
            var filtersParam = new GraphQLQueryParameter("filters", "BankAccountFilters");
            var fragment = new GraphQLQueryFragment("bankAccountsPage", [paginationParam, filtersParam], fields, "PageResponse");
            var builder = new QueryBuilder([fragment]);

            return builder.GetQuery();
        }

        #endregion
    }
}
