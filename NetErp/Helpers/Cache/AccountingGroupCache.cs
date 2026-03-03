using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using Models.Books;
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
    public class AccountingGroupCache : IEntityCache<AccountingGroupGraphQLModel>,
        IHandle<AccountingGroupCreateMessage>,
        IHandle<AccountingGroupUpdateMessage>,
        IHandle<AccountingGroupDeleteMessage>
    {
        private readonly IRepository<AccountingGroupGraphQLModel> _service;
        private readonly object _lock = new();

        public ObservableCollection<AccountingGroupGraphQLModel> Items { get; } = [];
        public bool IsInitialized { get; private set; }

        public AccountingGroupCache(
            IRepository<AccountingGroupGraphQLModel> service,
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
                variables.pageResponsePagination = new ExpandoObject();
                variables.pageResponsePagination.pageSize = -1;

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

        public void Add(AccountingGroupGraphQLModel item)
        {
            lock (_lock)
            {
                if (!Items.Any(x => x.Id == item.Id))
                    Items.Add(item);
            }
        }

        public void Update(AccountingGroupGraphQLModel item)
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

        public Task HandleAsync(AccountingGroupCreateMessage message, CancellationToken cancellationToken)
        {
            if (message.CreatedAccountingGroup?.Entity != null)
            {
                Add(message.CreatedAccountingGroup.Entity);
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(AccountingGroupUpdateMessage message, CancellationToken cancellationToken)
        {
            if (message.UpdatedAccountingGroup?.Entity != null)
            {
                Update(message.UpdatedAccountingGroup.Entity);
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(AccountingGroupDeleteMessage message, CancellationToken cancellationToken)
        {
            if (message.DeletedAccountingGroup?.DeletedId > 0)
            {
                Remove(message.DeletedAccountingGroup.DeletedId.Value);
            }
            return Task.CompletedTask;
        }

        #endregion

        #region Query Builder

        private string BuildQuery()
        {
            var fields = FieldSpec<PageType<AccountingGroupGraphQLModel>>
                .Create()
                .SelectList(x => x.Entries, entries => entries
                    .Field(x => x.Id)
                    .Field(x => x.Name)
                )
                .Build();

            var parameter = new GraphQLQueryParameter("pagination", "Pagination");
            var fragment = new GraphQLQueryFragment("accountingGroupsPage", [parameter], fields, "PageResponse");
            var builder = new QueryBuilder([fragment]);

            return builder.GetQuery();
        }

        #endregion
    }
}
