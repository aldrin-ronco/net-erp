using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using Models.Global;
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
    public class CostCenterCache : IEntityCache<CostCenterGraphQLModel>,
        IHandle<CostCenterCreateMessage>,
        IHandle<CostCenterUpdateMessage>,
        IHandle<CostCenterDeleteMessage>
    {
        private readonly IRepository<CostCenterGraphQLModel> _service;
        private readonly object _lock = new();

        private readonly ObservableCollection<CostCenterGraphQLModel> _items = [];
        public ReadOnlyObservableCollection<CostCenterGraphQLModel> Items { get; }
        public bool IsInitialized { get; private set; }

        public CostCenterCache(
            IRepository<CostCenterGraphQLModel> service,
            IEventAggregator eventAggregator)
        {
            _service = service;
            eventAggregator.SubscribeOnUIThread(this);
            Items = new ReadOnlyObservableCollection<CostCenterGraphQLModel>(_items);
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

        public void Add(CostCenterGraphQLModel item)
        {
            lock (_lock)
            {
                if (!_items.Any(x => x.Id == item.Id))
                    _items.Add(item);
            }
        }

        public void Update(CostCenterGraphQLModel item)
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

        public Task HandleAsync(CostCenterCreateMessage message, CancellationToken cancellationToken)
        {
            if (message.CreatedCostCenter?.Entity != null)
            {
                Add(message.CreatedCostCenter.Entity);
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(CostCenterUpdateMessage message, CancellationToken cancellationToken)
        {
            if (message.UpdatedCostCenter?.Entity != null)
            {
                Update(message.UpdatedCostCenter.Entity);
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(CostCenterDeleteMessage message, CancellationToken cancellationToken)
        {
            if (message.DeletedCostCenter?.DeletedId > 0)
            {
                Remove(message.DeletedCostCenter.DeletedId.Value);
            }
            return Task.CompletedTask;
        }

        #endregion

        #region Query Builder

        private string BuildQuery()
        {
            var fields = FieldSpec<PageType<CostCenterGraphQLModel>>
                .Create()
                .SelectList(x => x.Entries, entries => entries
                    .Field(x => x.Id)
                    .Field(x => x.Name)
                    .Field(x => x.IsTaxable)
                    .Field(x => x.PriceListIncludeTax)
                )
                .Build();

            var parameter = new GraphQLQueryParameter("pagination", "Pagination");
            var fragment = new GraphQLQueryFragment("costCentersPage", [parameter], fields, "PageResponse");
            var builder = new QueryBuilder([fragment]);

            return builder.GetQuery();
        }

        #endregion
    }
}
