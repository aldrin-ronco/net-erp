using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using Models.Books;
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
    public class ProcessTypeCache : IEntityCache<ProcessTypeGraphQLModel>

    {
        private readonly IRepository<ProcessTypeGraphQLModel> _service;
        private readonly object _lock = new();
        private readonly ObservableCollection<ProcessTypeGraphQLModel> _items = [];
        public ReadOnlyObservableCollection<ProcessTypeGraphQLModel> Items { get; }
        public bool IsInitialized { get; private set; }

        public ProcessTypeCache(
           IRepository<ProcessTypeGraphQLModel> service,
           IEventAggregator eventAggregator)
        {
            _service = service;
            Items = new ReadOnlyObservableCollection<ProcessTypeGraphQLModel>(_items);
            eventAggregator.SubscribeOnUIThread(this);
        }
        public void Add(ProcessTypeGraphQLModel item)
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
            var fields = FieldSpec<PageType<ProcessTypeGraphQLModel>>
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
            var fragment = new GraphQLQueryFragment("processTypesPage", [paginationParam], fields, "PageResponse");
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

        public void Update(ProcessTypeGraphQLModel item)
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
