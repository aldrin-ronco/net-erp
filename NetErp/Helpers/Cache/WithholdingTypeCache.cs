using Caliburn.Micro;
using Common.Interfaces;
using Models.Books;
using Models.Global;
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
    public class WithholdingTypeCache : IEntityCache<WithholdingTypeGraphQLModel>, IBatchLoadableCache
    {
        private readonly IRepository<WithholdingTypeGraphQLModel> _service;
        private readonly Lock _lock = new();

        private readonly ObservableCollection<WithholdingTypeGraphQLModel> _items = [];
        public ReadOnlyObservableCollection<WithholdingTypeGraphQLModel> Items { get; }
        public bool IsInitialized { get; private set; }

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadQuery = new(() =>
        {
            var fields = FieldSpec<PageType<WithholdingTypeGraphQLModel>>
                .Create()
                .SelectList(x => x.Entries, entries => entries
                    .Field(x => x.Id)
                    .Field(x => x.Name)
                )
                .Build();

            var parameter = new GraphQLQueryParameter("pagination", "Pagination");
            var fragment = new GraphQLQueryFragment("WithholdingTypesPage", [parameter], fields, "PageResponse");
            var query = new QueryBuilder([fragment]).GetQuery();

            return (fragment, query);
        });

        public WithholdingTypeCache(
            IRepository<WithholdingTypeGraphQLModel> service,
            IEventAggregator eventAggregator)
        {
            _service = service;
            eventAggregator.SubscribeOnUIThread(this);
            Items = new ReadOnlyObservableCollection<WithholdingTypeGraphQLModel>(_items);
        }

        public async Task EnsureLoadedAsync()
        {
            if (IsInitialized) return;

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

        #region IBatchLoadableCache

        public GraphQLQueryFragment LoadFragment => _loadQuery.Value.Fragment;

        public void ApplyVariables(GraphQLVariables variables, GraphQLQueryFragment batchFragment)
        {
            variables.For(batchFragment, "pagination", new { PageSize = -1 });
        }

        public void PopulateFromBatchResponse(JToken data)
        {
            var page = data.ToObject<PageType<WithholdingTypeGraphQLModel>>();
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

        public void Add(WithholdingTypeGraphQLModel item)
        {
            lock (_lock)
            {
                if (!_items.Any(x => x.Id == item.Id))
                    _items.Add(item);
            }
        }

        public void Update(WithholdingTypeGraphQLModel item)
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
    }
}
