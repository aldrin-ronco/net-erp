using Caliburn.Micro;
using Common.Interfaces;
using Models.Treasury;
using NetErp.Helpers.GraphQLQueryBuilder;
using QueryBuilder = NetErp.Helpers.GraphQLQueryBuilder.GraphQLQueryBuilder;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Helpers.Cache
{
    /// <summary>
    /// Cache para cajas generales (no caja menor).
    /// Solo incluye CashDrawers donde IsPettyCash = false.
    /// </summary>
    public class MajorCashDrawerCache : IEntityCache<CashDrawerGraphQLModel>,
        IHandle<TreasuryCashDrawerCreateMessage>,
        IHandle<TreasuryCashDrawerUpdateMessage>,
        IHandle<TreasuryCashDrawerDeleteMessage>
    {
        private readonly IRepository<CashDrawerGraphQLModel> _service;
        private readonly object _lock = new();

        public ObservableCollection<CashDrawerGraphQLModel> Items { get; } = [];
        public bool IsInitialized { get; private set; }

        public MajorCashDrawerCache(
            IRepository<CashDrawerGraphQLModel> service,
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
            variables.majorCashDrawersPagePagination = new ExpandoObject();
            variables.majorCashDrawersPagePagination.pageSize = -1;
            variables.majorCashDrawersPageFilters = new ExpandoObject();
            variables.majorCashDrawersPageFilters.isPettyCash = false;

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

        public void Add(CashDrawerGraphQLModel item)
        {
            lock (_lock)
            {
                if (!Items.Any(x => x.Id == item.Id))
                    Items.Add(item);
            }
        }

        public void Update(CashDrawerGraphQLModel item)
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

        #region IHandle Implementations

        public Task HandleAsync(TreasuryCashDrawerCreateMessage message, CancellationToken cancellationToken)
        {
            // Solo agregar si es caja general (no caja menor)
            if (message.CreatedCashDrawer != null && !message.CreatedCashDrawer.IsPettyCash)
            {
                Add(message.CreatedCashDrawer);
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(TreasuryCashDrawerUpdateMessage message, CancellationToken cancellationToken)
        {
            if (message.UpdatedCashDrawer != null)
            {
                if (!message.UpdatedCashDrawer.IsPettyCash)
                {
                    // Si es caja general, actualizar o agregar
                    var existing = Items.FirstOrDefault(x => x.Id == message.UpdatedCashDrawer.Id);
                    if (existing != null)
                        Update(message.UpdatedCashDrawer);
                    else
                        Add(message.UpdatedCashDrawer);
                }
                else
                {
                    // Si cambió a caja menor, remover de la lista de cajas generales
                    Remove(message.UpdatedCashDrawer.Id);
                }
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(TreasuryCashDrawerDeleteMessage message, CancellationToken cancellationToken)
        {
            if (message.DeletedCashDrawer != null)
            {
                Remove(message.DeletedCashDrawer.Id);
            }
            return Task.CompletedTask;
        }

        #endregion

        #region Query Builder

        private string BuildQuery()
        {
            var fields = FieldSpec<PageType<CashDrawerGraphQLModel>>
                .Create()
                .SelectList(x => x.Entries, entries => entries
                    .Field(x => x.Id)
                    .Field(x => x.Name)
                )
                .Build();

            var paginationParam = new GraphQLQueryParameter("pagination", "Pagination");
            var filtersParam = new GraphQLQueryParameter("filters", "CashDrawerFilterInput");
            var fragment = new GraphQLQueryFragment("majorCashDrawersPage", [paginationParam, filtersParam], fields, "PageResponse");
            var builder = new QueryBuilder([fragment]);

            return builder.GetQuery();
        }

        #endregion
    }
}
