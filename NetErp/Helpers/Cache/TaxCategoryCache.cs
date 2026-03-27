using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using Models.Books;
using NetErp.Helpers.GraphQLQueryBuilder;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Models.Global.GraphQLResponseTypes;
using QueryBuilder = NetErp.Helpers.GraphQLQueryBuilder.GraphQLQueryBuilder;

namespace NetErp.Helpers.Cache
{
    public class TaxCategoryCache : IEntityCache<TaxCategoryGraphQLModel>, IBatchLoadableCache,
        IHandle<TaxCategoryCreateMessage>,
        IHandle<TaxCategoryUpdateMessage>,
        IHandle<TaxCategoryDeleteMessage>
    {
        private readonly IRepository<TaxCategoryGraphQLModel> _service;
        private readonly Lock _lock = new();

        private readonly ObservableCollection<TaxCategoryGraphQLModel> _items = [];
        public ReadOnlyObservableCollection<TaxCategoryGraphQLModel> Items { get; }
        public bool IsInitialized { get; private set; }

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadQuery = new(() =>
        {
            var fields = FieldSpec<PageType<TaxCategoryGraphQLModel>>
                .Create()
                .SelectList(it => it.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Field(e => e.Prefix)
                    .Field(e => e.UsesPercentage)
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
            var query = new QueryBuilder([fragment]).GetQuery();

            return (fragment, query);
        });

        public TaxCategoryCache(IRepository<TaxCategoryGraphQLModel> service, IEventAggregator eventAggregator)
        {
            _service = service;
            eventAggregator.SubscribeOnUIThread(this);
            Items = new ReadOnlyObservableCollection<TaxCategoryGraphQLModel>(_items);
        }

        public async Task EnsureLoadedAsync()
        {
            if (IsInitialized) return;

            try
            {
                var (fragment, query) = _loadQuery.Value;
                dynamic variables = new GraphQLVariables().Build();

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

        public void Add(TaxCategoryGraphQLModel item)
        {
            lock (_lock)
            {
                if (!_items.Any(x => x.Id == item.Id))
                    _items.Add(item);
            }
        }

        #region IBatchLoadableCache

        public GraphQLQueryFragment LoadFragment => _loadQuery.Value.Fragment;

        public void ApplyVariables(GraphQLVariables variables, GraphQLQueryFragment batchFragment) { }

        public void PopulateFromBatchResponse(JToken data)
        {
            var page = data.ToObject<PageType<TaxCategoryGraphQLModel>>();
            if (page == null) return;

            lock (_lock)
            {
                _items.Clear();
                foreach (var item in page.Entries)
                    _items.Add(item);
                IsInitialized = true;
            }
        }

        #endregion

        public void Clear()
        {
            lock (_lock)
            {
                _items.Clear();
                IsInitialized = false;
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

        public void Update(TaxCategoryGraphQLModel item)
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

        #region IHandle Implementations

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
            if (message.CreatedTaxCategory != null)
            {
                Add(message.CreatedTaxCategory.Entity);
            }
            return Task.CompletedTask;
        }

        Task IHandle<TaxCategoryUpdateMessage>.HandleAsync(TaxCategoryUpdateMessage message, CancellationToken cancellationToken)
        {
            var entity = message.UpdatedTaxCategory?.Entity;

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

        #endregion
    }
}
