using Caliburn.Micro;
using Common.Interfaces;
using Models.Books;
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
    public class WithholdingTypeCache : IEntityCache<WithholdingTypeGraphQLModel>
    {
        private readonly IRepository<WithholdingTypeGraphQLModel> _service;
        private readonly object _lock = new();

        public ObservableCollection<WithholdingTypeGraphQLModel> Items { get; } = [];
        public bool IsInitialized { get; private set; }

        public WithholdingTypeCache(
            IRepository<WithholdingTypeGraphQLModel> service,
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
            variables.WithholdingTypesPagePagination = new ExpandoObject();
            variables.WithholdingTypesPagePagination.pageSize = -1;

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

        public void Add(WithholdingTypeGraphQLModel item)
        {
            lock (_lock)
            {
                if (!Items.Any(x => x.Id == item.Id))
                    Items.Add(item);
            }
        }

        public void Update(WithholdingTypeGraphQLModel item)
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
            var fields = FieldSpec<PageType<WithholdingTypeGraphQLModel>>
                .Create()
                .SelectList(x => x.Entries, entries => entries
                    .Field(x => x.Id)
                    .Field(x => x.Name)
                    
                )
                .Build();

            var parameter = new GraphQLQueryParameter("pagination", "Pagination");
            var fragment = new GraphQLQueryFragment("WithholdingTypesPage", [parameter], fields, "PageResponse");
            var builder = new QueryBuilder([fragment]);

            return builder.GetQuery();
        }

        #endregion
    }
}
