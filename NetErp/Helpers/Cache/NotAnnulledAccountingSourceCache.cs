using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using Models.Books;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Models.Global.GraphQLResponseTypes;
using QueryBuilder = NetErp.Helpers.GraphQLQueryBuilder.GraphQLQueryBuilder;

namespace NetErp.Helpers.Cache
{
    public class NotAnnulledAccountingSourceCache : IEntityCache<AccountingSourceGraphQLModel>,
         IHandle<AccountingSourceUpdateMessage>,
         IHandle<AccountingSourceCreateMessage>,
         IHandle<AccountingSourceDeleteMessage>
    {
        private readonly IRepository<AccountingSourceGraphQLModel> _service;
        private readonly object _lock = new();
        public ObservableCollection<AccountingSourceGraphQLModel> Items { get; } = [];

        public bool IsInitialized { get; private set; }
        public NotAnnulledAccountingSourceCache(
            IRepository<AccountingSourceGraphQLModel> service,
            IEventAggregator eventAggregator)
        {
            _service = service;
            eventAggregator.SubscribeOnUIThread(this);
        }

        public void Add(AccountingSourceGraphQLModel item)
        {
            lock (_lock)
            {
                if (!Items.Any(x => x.Id == item.Id))
                    Items.Add(item);
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

        public async Task EnsureLoadedAsync()
        {
            if (IsInitialized) return;

            try
            {
                var query = BuildQuery();
                dynamic variables = new ExpandoObject();
                variables.pageResponseFilters = new ExpandoObject();
                variables.pageResponseFilters.annulment = false;

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
        private string BuildQuery()
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
            var builder = new QueryBuilder([fragment]);

            return builder.GetQuery();
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

        public void Update(AccountingSourceGraphQLModel item)
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

        public Task HandleAsync(AccountingSourceDeleteMessage message, CancellationToken cancellationToken)
        {
            if (message.DeletedAccountingSource?.DeletedId > 0)
            {
                Remove(message.DeletedAccountingSource.DeletedId);
            }

            return Task.CompletedTask;
        }

        public Task HandleAsync(AccountingSourceCreateMessage message, CancellationToken cancellationToken)
        {
            if (message.CreatedAccountingSource != null
               )
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

                var existing = Items.FirstOrDefault(x => x.Id == entity.Id);


                if (existing != null)
                    Update(entity);
                else
                    Add(entity);


            }
            return Task.CompletedTask;
        }
    }
}
