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
    public class CtasRestVtasAccountingAccountGroupCache : IEntityCache<AccountingAccountGroupGraphQLModel>,
        IHandle<AccountingAccountGroupUpdateMessage>
    {
        private readonly IRepository<AccountingAccountGroupGraphQLModel> _service;
        private readonly object _lock = new();

        public ObservableCollection<AccountingAccountGroupGraphQLModel> Items { get; } = [];
        public bool IsInitialized { get; private set; }

        ObservableCollection<AccountingAccountGroupGraphQLModel> IEntityCache<AccountingAccountGroupGraphQLModel>.Items => throw new NotImplementedException();

        public CtasRestVtasAccountingAccountGroupCache(
            IRepository<AccountingAccountGroupGraphQLModel> service,
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
                variables.accountingAccountGroupFilterInput = new ExpandoObject();
                variables.accountingAccountGroupFilterInput.key = "CTAS_RETS_VTAS";


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

        public void Add(AccountingAccountGroupGraphQLModel item)
        {
            lock (_lock)
            {
                if (!Items.Any(x => x.Id == item.Id))
                    Items.Add(item);
            }
        }

        public void Update(AccountingAccountGroupGraphQLModel item)
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

       

        

        public Task HandleAsync(AccountingAccountGroupUpdateMessage message, CancellationToken cancellationToken)
        {
            var accountingAccountGroup = message.UpsertAccountingAccountGroup?.Entity ?? message.UpdateAccountingAccountGroup;

            if (accountingAccountGroup != null)
            {
                bool isVTAS = !string.IsNullOrEmpty(accountingAccountGroup.Key) && accountingAccountGroup.Key == "CTAS_RETS_VTAS";
                var existing = Items.FirstOrDefault(x => x.Id == accountingAccountGroup.Id);

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

        #region Query Builder

        private string BuildQuery()
        {
            var accountingAccountGroupFields = FieldSpec<PageType<AccountingAccountGroupGraphQLModel>>
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

            var accountingAccountGroupParameters = new GraphQLQueryParameter("pagination", "Pagination");
            var accountingAccountGroupFilterParameters = new GraphQLQueryParameter("filters", "AccountingAccountGroupFilters");
            var accountingAccountGroupFragment = new GraphQLQueryFragment("accountingAccountGroupsPage", [accountingAccountGroupParameters, accountingAccountGroupFilterParameters], accountingAccountGroupFields, "PageResponse");



            var builder = new QueryBuilder([accountingAccountGroupFragment]);
            return builder.GetQuery();

           
        }

        Task IEntityCache<AccountingAccountGroupGraphQLModel>.EnsureLoadedAsync()
        {
            throw new NotImplementedException();
        }

       

       

        #endregion
    }
}
