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
    public class AccountingBookCache : IEntityCache<AccountingBookGraphQLModel>,
        IHandle<AccountingBookCreateMessage>,
        IHandle<AccountingBookUpdateMessage>,
        IHandle<AccountingBookDeleteMessage>
    {
        private readonly IRepository<AccountingBookGraphQLModel> _service;
        private readonly object _lock = new();

        public ObservableCollection<AccountingBookGraphQLModel> Items  { get; } = [];
        public bool IsInitialized { get; private set; }

        public AccountingBookCache(
            IRepository<AccountingBookGraphQLModel> service,
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
        public void Add(AccountingBookGraphQLModel item)
        {
            lock (_lock)
            {
                if (!Items.Any(x => x.Id == item.Id))
                    Items.Add(item);
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

        public void Update(AccountingBookGraphQLModel item)
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
        private string BuildQuery()
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
            var builder = new QueryBuilder([fragment]);

            return builder.GetQuery();
        }

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
           
            if (message.CreatedAccountingBook != null 
               )
            {
                Add(message.CreatedAccountingBook.Entity);
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(AccountingBookUpdateMessage message, CancellationToken cancellationToken)
        {
            
            var book = message.UpdatedAccountingBook?.Entity ;

            if (book != null)
            {
                
                var existing = Items.FirstOrDefault(x => x.Id == book.Id);

                
                    if (existing != null)
                        Update(book);
                    else
                        Add(book);
                
            
            }
            return Task.CompletedTask;
        }
    }
}
