using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using Models.Billing;
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
    public class TaxCategoryCache : IEntityCache<TaxCategoryGraphQLModel>,
         IHandle<TaxCategoryCreateMessage>,
        IHandle<TaxCategoryUpdateMessage>,
        IHandle<TaxCategoryDeleteMessage>
    {
        private readonly IRepository<TaxCategoryGraphQLModel> _service;
        private readonly object _lock = new();

        public ObservableCollection<TaxCategoryGraphQLModel> Items { get; } = [];

        public bool IsInitialized { get; private set; }
        public TaxCategoryCache(IRepository<TaxCategoryGraphQLModel> service, IEventAggregator eventAggregator)
        {
            this._service = service;
            eventAggregator.SubscribeOnUIThread(this);
        }



        public void Add(TaxCategoryGraphQLModel item)
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
            var fields = FieldSpec<PageType<TaxCategoryGraphQLModel>>
              .Create()
              .SelectList(it => it.Entries, entries => entries
                  .Field(e => e.Id)
                  .Field(e => e.Name)
                  .Field(e => e.Prefix)
               .Field(e => e.GeneratedTaxRefundAccountIsRequired)
               .Field(e => e.GeneratedTaxAccountIsRequired)
               .Field(e => e.DeductibleTaxRefundAccountIsRequired)
               .Field(e => e.DeductibleTaxAccountIsRequired)
              )
              .Field(o => o.PageNumber)
              .Field(o => o.PageSize)
              .Field(o => o.TotalPages)
              .Field(o => o.TotalEntries)
              .Build();


            var paginationParam = new GraphQLQueryParameter("pagination", "Pagination");
            var filtersParam = new GraphQLQueryParameter("filters", "TaxCategoryFilters");
            var fragment = new GraphQLQueryFragment("taxCategoriesPage", [paginationParam, filtersParam], fields, "PageResponse");
            var builder = new QueryBuilder([fragment]);

            return builder.GetQuery();
        }

        public Task HandleAsync(TaxCategoryDeleteMessage message, CancellationToken cancellationToken)
        {
            if (message.DeletedTaxCategory?.DeletedId > 0)
            {
                Remove(message.DeletedTaxCategory.DeletedId.Value);
            }

            return Task.CompletedTask;
        }

        public Task HandleAsync(TaxCategoryCreateMessage message, CancellationToken cancellationToken)
        {
            if (message.CreatedTaxCategory != null
               )
            {
                Add(message.CreatedTaxCategory.Entity);
            }
            return Task.CompletedTask;
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

        public void Update(TaxCategoryGraphQLModel item)
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

        Task IHandle<TaxCategoryUpdateMessage>.HandleAsync(TaxCategoryUpdateMessage message, CancellationToken cancellationToken)
        {
            var entity = message.UpdatedTaxCategory?.Entity;

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
