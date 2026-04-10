using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using Models.Global;
using Models.Inventory;
using NetErp.Helpers.GraphQLQueryBuilder;
using Newtonsoft.Json.Linq;
using QueryBuilder = NetErp.Helpers.GraphQLQueryBuilder.GraphQLQueryBuilder;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Helpers.Cache
{
    public class CatalogCache : IEntityCache<CatalogGraphQLModel>, IBatchLoadableCache,
        IHandle<CatalogCreateMessage>,
        IHandle<CatalogUpdateMessage>,
        IHandle<CatalogDeleteMessage>,
        IHandle<ItemTypeCreateMessage>,
        IHandle<ItemTypeUpdateMessage>,
        IHandle<ItemTypeDeleteMessage>
    {
        private readonly IRepository<CatalogGraphQLModel> _service;
        private readonly Lock _lock = new();

        private readonly ObservableCollection<CatalogGraphQLModel> _items = [];
        public ReadOnlyObservableCollection<CatalogGraphQLModel> Items { get; }
        public bool IsInitialized { get; private set; }

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadQuery = new(() =>
        {
            var fields = FieldSpec<PageType<CatalogGraphQLModel>>
                .Create()
                .SelectList(x => x.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .SelectList(e => e.ItemTypes, it => it
                        .Field(t => t.Id)
                        .Field(t => t.Name)
                        .Field(t => t.PrefixChar)
                        .Field(t => t.StockControl)
                        .Select(t => t.Catalog, c => c.Field(cc => cc.Id))
                        .Select(t => t.DefaultMeasurementUnit, mu => mu.Field(m => m.Id))
                        .Select(t => t.DefaultAccountingGroup, ag => ag.Field(a => a.Id))))
                .Build();

            var parameter = new GraphQLQueryParameter("pagination", "Pagination");
            var fragment = new GraphQLQueryFragment("catalogsPage", [parameter], fields, "PageResponse");
            var query = new QueryBuilder([fragment]).GetQuery();

            return (fragment, query);
        });

        public CatalogCache(
            IRepository<CatalogGraphQLModel> service,
            IEventAggregator eventAggregator)
        {
            _service = service;
            eventAggregator.SubscribeOnUIThread(this);
            Items = new ReadOnlyObservableCollection<CatalogGraphQLModel>(_items);
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

                UiDispatcher.Invoke(() =>
                {
                    lock (_lock)
                    {
                        _items.Clear();
                        foreach (var item in result.Entries)
                        {
                            if (item.ItemTypes == null) item.ItemTypes = new System.Collections.Generic.List<ItemTypeGraphQLModel>();
                            _items.Add(item);
                        }
                        IsInitialized = true;
                    }
                });
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }

        #region IBatchLoadableCache

        public GraphQLQueryFragment LoadFragment => _loadQuery.Value.Fragment;

        public void ApplyVariables(GraphQLVariables variables, GraphQLQueryFragment batchFragment)
        {
            variables.For(batchFragment, "pagination", new { PageSize = -1 });
        }

        public void PopulateFromBatchResponse(JToken data)
        {
            var page = data.ToObject<PageType<CatalogGraphQLModel>>();
            if (page == null) return;

            UiDispatcher.Invoke(() =>
            {
                lock (_lock)
                {
                    _items.Clear();
                    foreach (var item in page.Entries)
                    {
                        if (item.ItemTypes == null) item.ItemTypes = new System.Collections.Generic.List<ItemTypeGraphQLModel>();
                        _items.Add(item);
                    }
                    IsInitialized = true;
                }
            });
        }

        #endregion

        public void Clear()
        {
            UiDispatcher.Invoke(() =>
            {
                lock (_lock)
                {
                    _items.Clear();
                    IsInitialized = false;
                }
            });
        }

        public void Add(CatalogGraphQLModel item)
        {
            UiDispatcher.Invoke(() =>
            {
                lock (_lock)
                {
                    if (!_items.Any(x => x.Id == item.Id))
                    {
                        if (item.ItemTypes == null) item.ItemTypes = new System.Collections.Generic.List<ItemTypeGraphQLModel>();
                        _items.Add(item);
                    }
                }
            });
        }

        public void Update(CatalogGraphQLModel item)
        {
            UiDispatcher.Invoke(() =>
            {
                lock (_lock)
                {
                    var existing = _items.FirstOrDefault(x => x.Id == item.Id);
                    if (existing != null)
                    {
                        // Preserve existing ItemTypes if incoming update doesn't include them,
                        // so catalog renames don't wipe out the nested tree.
                        if (item.ItemTypes == null || !item.ItemTypes.Any())
                            item.ItemTypes = existing.ItemTypes;
                        var index = _items.IndexOf(existing);
                        _items[index] = item;
                    }
                }
            });
        }

        public void Remove(int id)
        {
            UiDispatcher.Invoke(() =>
            {
                lock (_lock)
                {
                    var item = _items.FirstOrDefault(x => x.Id == id);
                    if (item != null)
                        _items.Remove(item);
                }
            });
        }

        #region IHandle Catalog

        public Task HandleAsync(CatalogCreateMessage message, CancellationToken cancellationToken)
        {
            if (message.CreatedCatalog?.Entity != null)
                Add(message.CreatedCatalog.Entity);
            return Task.CompletedTask;
        }

        public Task HandleAsync(CatalogUpdateMessage message, CancellationToken cancellationToken)
        {
            if (message.UpdatedCatalog?.Entity != null)
                Update(message.UpdatedCatalog.Entity);
            return Task.CompletedTask;
        }

        public Task HandleAsync(CatalogDeleteMessage message, CancellationToken cancellationToken)
        {
            if (message.DeletedCatalog?.DeletedId > 0)
                Remove(message.DeletedCatalog.DeletedId.Value);
            return Task.CompletedTask;
        }

        #endregion

        #region IHandle ItemType (nested maintenance)

        public Task HandleAsync(ItemTypeCreateMessage message, CancellationToken cancellationToken)
        {
            var entity = message.CreatedItemType?.Entity;
            if (entity?.Catalog == null) return Task.CompletedTask;

            UiDispatcher.Invoke(() =>
            {
                lock (_lock)
                {
                    var catalog = _items.FirstOrDefault(c => c.Id == entity.Catalog.Id);
                    if (catalog == null) return;
                    var list = (catalog.ItemTypes ?? []).ToList();
                    if (!list.Any(t => t.Id == entity.Id))
                    {
                        list.Add(entity);
                        catalog.ItemTypes = list;
                    }
                }
            });
            return Task.CompletedTask;
        }

        public Task HandleAsync(ItemTypeUpdateMessage message, CancellationToken cancellationToken)
        {
            var entity = message.UpdatedItemType?.Entity;
            if (entity?.Catalog == null) return Task.CompletedTask;

            UiDispatcher.Invoke(() =>
            {
                lock (_lock)
                {
                    var catalog = _items.FirstOrDefault(c => c.Id == entity.Catalog.Id);
                    if (catalog?.ItemTypes == null) return;
                    var list = catalog.ItemTypes.ToList();
                    var index = list.FindIndex(t => t.Id == entity.Id);
                    if (index >= 0)
                    {
                        list[index] = entity;
                        catalog.ItemTypes = list;
                    }
                }
            });
            return Task.CompletedTask;
        }

        public Task HandleAsync(ItemTypeDeleteMessage message, CancellationToken cancellationToken)
        {
            var deletedId = message.DeletedItemType?.DeletedId;
            if (deletedId is null or <= 0) return Task.CompletedTask;

            UiDispatcher.Invoke(() =>
            {
                lock (_lock)
                {
                    foreach (var catalog in _items)
                    {
                        if (catalog.ItemTypes == null) continue;
                        var list = catalog.ItemTypes.ToList();
                        if (list.RemoveAll(t => t.Id == deletedId) > 0)
                            catalog.ItemTypes = list;
                    }
                }
            });
            return Task.CompletedTask;
        }

        #endregion
    }
}
