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
    public class ItemSizeCategoryCache : IEntityCache<ItemSizeCategoryGraphQLModel>,
        IHandle<ItemSizeCategoryCreateMessage>,
        IHandle<ItemSizeCategoryUpdateMessage>,
        IHandle<ItemSizeCategoryDeleteMessage>
    {
        private readonly IRepository<ItemSizeCategoryGraphQLModel> _service;
        private readonly Lock _lock = new();

        private readonly ObservableCollection<ItemSizeCategoryGraphQLModel> _items = [];
        public ReadOnlyObservableCollection<ItemSizeCategoryGraphQLModel> Items { get; }
        public bool IsInitialized { get; private set; }

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadQuery = new(() =>
        {
            var fields = FieldSpec<PageType<ItemSizeCategoryGraphQLModel>>
                .Create()
                .SelectList(x => x.Entries, entries => entries
                    .Field(x => x.Id)
                    .Field(x => x.Name)
                )
                .Build();

            var parameter = new GraphQLQueryParameter("pagination", "Pagination");
            var fragment = new GraphQLQueryFragment("itemSizeCategoriesPage", [parameter], fields, "PageResponse");
            var query = new QueryBuilder([fragment]).GetQuery();

            return (fragment, query);
        });

        public ItemSizeCategoryCache(
            IRepository<ItemSizeCategoryGraphQLModel> service,
            IEventAggregator eventAggregator)
        {
            _service = service;
            Items = new ReadOnlyObservableCollection<ItemSizeCategoryGraphQLModel>(_items);
            eventAggregator.SubscribeOnUIThread(this);
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

        public void Add(ItemSizeCategoryGraphQLModel item)
        {
            lock (_lock)
            {
                if (!_items.Any(x => x.Id == item.Id))
                    _items.Add(item);
            }
        }

        public void Update(ItemSizeCategoryGraphQLModel item)
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

        public Task HandleAsync(ItemSizeCategoryCreateMessage message, CancellationToken cancellationToken)
        {
            if (message.CreatedItemSizeCategory?.Entity != null)
            {
                Add(message.CreatedItemSizeCategory.Entity);
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(ItemSizeCategoryUpdateMessage message, CancellationToken cancellationToken)
        {
            if (message.UpdatedItemSizeCategory?.Entity != null)
            {
                Update(message.UpdatedItemSizeCategory.Entity);
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(ItemSizeCategoryDeleteMessage message, CancellationToken cancellationToken)
        {
            if (message.DeletedItemSizeCategory?.DeletedId > 0)
            {
                Remove(message.DeletedItemSizeCategory.DeletedId.Value);
            }
            return Task.CompletedTask;
        }

        #endregion
    }
}
