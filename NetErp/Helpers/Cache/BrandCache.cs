using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using Models.Inventory;
using NetErp.Helpers.GraphQLQueryBuilder;
using QueryBuilder = NetErp.Helpers.GraphQLQueryBuilder.GraphQLQueryBuilder;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Helpers.Cache
{
    public class ItemBrandCache : IEntityCache<ItemBrandGraphQLModel>,
        IHandle<ItemBrandCreateMessage>,
        IHandle<ItemBrandUpdateMessage>,
        IHandle<ItemBrandDeleteMessage>
    {
        private readonly IRepository<ItemBrandGraphQLModel> _service;
        private readonly Lock _lock = new();

        private readonly ObservableCollection<ItemBrandGraphQLModel> _items = [];
        public ReadOnlyObservableCollection<ItemBrandGraphQLModel> Items { get; }
        public bool IsInitialized { get; private set; }

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadQuery = new(() =>
        {
            var fields = FieldSpec<PageType<ItemBrandGraphQLModel>>
                .Create()
                .SelectList(x => x.Entries, entries => entries
                    .Field(x => x.Id)
                    .Field(x => x.Name)
                )
                .Build();

            var parameter = new GraphQLQueryParameter("pagination", "Pagination");
            var fragment = new GraphQLQueryFragment("itemBrandsPage", [parameter], fields, "PageResponse");
            var query = new QueryBuilder([fragment]).GetQuery();

            return (fragment, query);
        });

        public ItemBrandCache(
            IRepository<ItemBrandGraphQLModel> service,
            IEventAggregator eventAggregator)
        {
            _service = service;
            eventAggregator.SubscribeOnUIThread(this);
            Items = new ReadOnlyObservableCollection<ItemBrandGraphQLModel>(_items);
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

        public void Add(ItemBrandGraphQLModel item)
        {
            lock (_lock)
            {
                if (!_items.Any(x => x.Id == item.Id))
                    _items.Add(item);
            }
        }

        public void Update(ItemBrandGraphQLModel item)
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

        public Task HandleAsync(ItemBrandCreateMessage message, CancellationToken cancellationToken)
        {
            if (message.CreatedItemBrand?.Entity != null)
            {
                Add(message.CreatedItemBrand.Entity);
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(ItemBrandUpdateMessage message, CancellationToken cancellationToken)
        {
            if (message.UpdatedItemBrand?.Entity != null)
            {
                Update(message.UpdatedItemBrand.Entity);
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(ItemBrandDeleteMessage message, CancellationToken cancellationToken)
        {
            if (message.DeletedItemBrand?.DeletedId > 0)
            {
                Remove(message.DeletedItemBrand.DeletedId.Value);
            }
            return Task.CompletedTask;
        }

        #endregion
    }
}
