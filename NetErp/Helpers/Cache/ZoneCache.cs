using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using Models.Billing;
using Models.Books;
using Models.Global;
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
    public class ZoneCache : IEntityCache<ZoneGraphQLModel>,
        IHandle<ZoneCreateMessage>,
        IHandle<ZoneUpdateMessage>,
        IHandle<ZoneDeleteMessage>
    {
        private readonly IRepository<ZoneGraphQLModel> _service;
        private readonly object _lock = new();

        public ObservableCollection<ZoneGraphQLModel> Items  { get; } = [];

        public bool IsInitialized { get; private set; }
        public ZoneCache(IRepository<ZoneGraphQLModel> service, IEventAggregator eventAggregator)
        {
            this._service = service;
            eventAggregator.SubscribeOnUIThread(this);
        }

     
        public void Add(ZoneGraphQLModel item)
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
            var fields = FieldSpec<PageType<ZoneGraphQLModel>>
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
            var filtersParam = new GraphQLQueryParameter("filters", "ZoneFilters");
            var fragment = new GraphQLQueryFragment("zonesPage", [paginationParam, filtersParam], fields, "PageResponse");
            var builder = new QueryBuilder([fragment]);

            return builder.GetQuery();
        }
        public Task HandleAsync(ZoneCreateMessage message, CancellationToken cancellationToken)
        {
            if (message.CreatedZone != null
              )
            {
                Add(message.CreatedZone.Entity);
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(ZoneUpdateMessage message, CancellationToken cancellationToken)
        {
            var entity = message.UpdatedZone?.Entity;

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

        public Task HandleAsync(ZoneDeleteMessage message, CancellationToken cancellationToken)
        {
            if (message.DeletedZone?.DeletedId > 0)
            {
                Remove(message.DeletedZone.DeletedId.Value);
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

        public void Update(ZoneGraphQLModel item)
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
    }
}
