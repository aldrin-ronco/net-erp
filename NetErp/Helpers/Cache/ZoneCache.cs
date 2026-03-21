using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using Models.Billing;
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
    public class ZoneCache : IEntityCache<ZoneGraphQLModel>,
        IHandle<ZoneCreateMessage>,
        IHandle<ZoneUpdateMessage>,
        IHandle<ZoneDeleteMessage>
    {
        private readonly IRepository<ZoneGraphQLModel> _service;
        private readonly Lock _lock = new();

        private readonly ObservableCollection<ZoneGraphQLModel> _items = [];
        public ReadOnlyObservableCollection<ZoneGraphQLModel> Items { get; }
        public bool IsInitialized { get; private set; }

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadQuery = new(() =>
        {
            var fields = FieldSpec<PageType<ZoneGraphQLModel>>
                .Create()
                .SelectList(it => it.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Field(e => e.IsActive)
                )
                .Field(o => o.PageNumber)
                .Field(o => o.PageSize)
                .Field(o => o.TotalPages)
                .Field(o => o.TotalEntries)
                .Build();

            var paginationParam = new GraphQLQueryParameter("pagination", "Pagination");
            var filtersParam = new GraphQLQueryParameter("filters", "ZoneFilters");
            var fragment = new GraphQLQueryFragment("zonesPage", [paginationParam, filtersParam], fields, "PageResponse");
            var query = new QueryBuilder([fragment]).GetQuery();

            return (fragment, query);
        });

        public ZoneCache(IRepository<ZoneGraphQLModel> service, IEventAggregator eventAggregator)
        {
            _service = service;
            eventAggregator.SubscribeOnUIThread(this);
            Items = new ReadOnlyObservableCollection<ZoneGraphQLModel>(_items);
        }

        public async Task EnsureLoadedAsync()
        {
            if (IsInitialized) return;

            try
            {
                var (fragment, query) = _loadQuery.Value;
                var variables = new GraphQLVariables()
                    .For(fragment, "pagination", new { PageSize = -1 })
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

        public void Add(ZoneGraphQLModel item)
        {
            lock (_lock)
            {
                if (!_items.Any(x => x.Id == item.Id))
                    _items.Add(item);
            }
        }

        public void Update(ZoneGraphQLModel item)
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

        public Task HandleAsync(ZoneCreateMessage message, CancellationToken cancellationToken)
        {
            if (message.CreatedZone != null)
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
                var existing = _items.FirstOrDefault(x => x.Id == entity.Id);
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

        #endregion
    }
}
