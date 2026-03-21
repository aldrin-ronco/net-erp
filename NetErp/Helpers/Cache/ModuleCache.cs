using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using Models.Global;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Models.Global.GraphQLResponseTypes;
using QueryBuilder = NetErp.Helpers.GraphQLQueryBuilder.GraphQLQueryBuilder;

namespace NetErp.Helpers.Cache
{
    public class ModuleCache : IEntityCache<ModuleGraphQLModel>
    {
        private readonly IRepository<ModuleGraphQLModel> _service;
        private readonly object _lock = new();
        private readonly ObservableCollection<ModuleGraphQLModel> _items = [];
        public ReadOnlyObservableCollection<ModuleGraphQLModel> Items { get; }
        public bool IsInitialized { get; private set; }

        public ModuleCache(
           IRepository<ModuleGraphQLModel> service,
           IEventAggregator eventAggregator)
        {
            _service = service;
            Items = new ReadOnlyObservableCollection<ModuleGraphQLModel>(_items);
            eventAggregator.SubscribeOnUIThread(this);
        }


        public void Add(ModuleGraphQLModel item)
        {
            lock (_lock)
            {
                if (!_items.Any(x => x.Id == item.Id))
                    _items.Add(item);
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
        private string BuildQuery()
        {
            var fields = FieldSpec<PageType<ModuleGraphQLModel>>
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
            var filtersParam = new GraphQLQueryParameter("filters", "AuthorizationSequenceTypeFilters");
            var fragment = new GraphQLQueryFragment("authorizationSequenceTypesPage", [paginationParam, filtersParam], fields, "PageResponse");
            var builder = new QueryBuilder([fragment]);

            return builder.GetQuery();
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

        public void Update(ModuleGraphQLModel item)
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
    }
}
