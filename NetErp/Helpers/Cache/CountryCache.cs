using Caliburn.Micro;
using Common.Interfaces;
using Models.Global;
using NetErp.Helpers.GraphQLQueryBuilder;
using QueryBuilder = NetErp.Helpers.GraphQLQueryBuilder.GraphQLQueryBuilder;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Helpers.Cache
{
    public class CountryCache : IEntityCache<CountryGraphQLModel>
    {
        private readonly IRepository<CountryGraphQLModel> _service;
        private readonly object _lock = new();

        public ObservableCollection<CountryGraphQLModel> Items { get; } = [];
        public bool IsInitialized { get; private set; }

        public CountryCache(
            IRepository<CountryGraphQLModel> service,
            IEventAggregator eventAggregator)
        {
            _service = service;
            eventAggregator.SubscribeOnUIThread(this);
        }

        public async Task EnsureLoadedAsync()
        {
            if (IsInitialized) return;

            var query = BuildQuery();
            dynamic variables = new ExpandoObject();
            variables.countriesPagePagination = new ExpandoObject();
            variables.countriesPagePagination.pageSize = -1;

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

        public void Clear()
        {
            lock (_lock)
            {
                Items.Clear();
                IsInitialized = false;
            }
        }

        public void Add(CountryGraphQLModel item)
        {
            lock (_lock)
            {
                if (!Items.Any(x => x.Id == item.Id))
                    Items.Add(item);
            }
        }

        public void Update(CountryGraphQLModel item)
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

        public void Remove(int id)
        {
            lock (_lock)
            {
                var item = Items.FirstOrDefault(x => x.Id == id);
                if (item != null)
                    Items.Remove(item);
            }
        }

        #region Query Builder

        private string BuildQuery()
        {
            var fields = FieldSpec<PageType<CountryGraphQLModel>>
                .Create()
                .SelectList(x => x.Entries, entries => entries
                    .Field(x => x.Id)
                    .Field(x => x.Name)
                    .Field(x => x.Code)
                    .SelectList(x => x.Departments, depts => depts
                        .Field(d => d.Id)
                        .Field(d => d.Name)
                        .Field(d => d.Code)
                        .SelectList(d => d.Cities, cities => cities
                            .Field(c => c.Id)
                            .Field(c => c.Name)
                            .Field(c => c.Code)
                        )
                    )
                )
                .Build();

            var parameter = new GraphQLQueryParameter("pagination", "Pagination");
            var fragment = new GraphQLQueryFragment("countriesPage", [parameter], fields, "PageResponse");
            var builder = new QueryBuilder([fragment]);

            return builder.GetQuery();
        }

        #endregion
    }
}
