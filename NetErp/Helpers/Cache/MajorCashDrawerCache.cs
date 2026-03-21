using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using Models.Treasury;
using NetErp.Helpers.GraphQLQueryBuilder;
using QueryBuilder = NetErp.Helpers.GraphQLQueryBuilder.GraphQLQueryBuilder;
using System;
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

        private readonly ObservableCollection<CashDrawerGraphQLModel> _items = [];
        public ReadOnlyObservableCollection<CashDrawerGraphQLModel> Items { get; }
        public bool IsInitialized { get; private set; }

        public MajorCashDrawerCache(
            IRepository<CashDrawerGraphQLModel> service,
            IEventAggregator eventAggregator)
        {
            _service = service;
            eventAggregator.SubscribeOnUIThread(this);
            Items = new ReadOnlyObservableCollection<CashDrawerGraphQLModel>(_items);
        }

        public async Task EnsureLoadedAsync()
        {
            if (IsInitialized) return;

            try
            {
                var query = BuildQuery();
                dynamic variables = new ExpandoObject();
                variables.pageResponsePagination = new ExpandoObject();
                variables.pageResponsePagination.pageSize = -1;
                variables.pageResponseFilters = new ExpandoObject();
                variables.pageResponseFilters.isPettyCash = false;

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

        public void Add(CashDrawerGraphQLModel item)
        {
            lock (_lock)
            {
                if (!_items.Any(x => x.Id == item.Id))
                    _items.Add(item);
            }
        }

        public void Update(CashDrawerGraphQLModel item)
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

        public Task HandleAsync(TreasuryCashDrawerCreateMessage message, CancellationToken cancellationToken)
        {
            var createdCashDrawer = message.CreatedCashDrawer?.Entity;
            // Solo agregar si es caja general (no caja menor)
            if (createdCashDrawer != null && !createdCashDrawer.IsPettyCash)
            {
                Add(createdCashDrawer);
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(TreasuryCashDrawerUpdateMessage message, CancellationToken cancellationToken)
        {
            var updatedCashDrawer = message.UpdatedCashDrawer?.Entity;
            if (updatedCashDrawer != null)
            {
                if (!updatedCashDrawer.IsPettyCash)
                {
                    // Si es caja general, actualizar o agregar
                    var existing = _items.FirstOrDefault(x => x.Id == updatedCashDrawer.Id);
                    if (existing != null)
                        Update(updatedCashDrawer);
                    else
                        Add(updatedCashDrawer);
                }
                else
                {
                    // Si cambió a caja menor, remover de la lista de cajas generales
                    Remove(updatedCashDrawer.Id);
                }
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(TreasuryCashDrawerDeleteMessage message, CancellationToken cancellationToken)
        {
            if (message.DeletedCashDrawer?.DeletedId > 0)
            {
                Remove(message.DeletedCashDrawer.DeletedId.Value);
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
            var filtersParam = new GraphQLQueryParameter("filters", "CashDrawerFilters");
            var fragment = new GraphQLQueryFragment("CashDrawersPage", [paginationParam, filtersParam], fields, "PageResponse");
            var builder = new QueryBuilder([fragment]);

            return builder.GetQuery();
        }

        #endregion
    }
}
