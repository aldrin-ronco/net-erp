using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using Models.Inventory;
using NetErp.Helpers.GraphQLQueryBuilder;
using Newtonsoft.Json.Linq;
using QueryBuilder = NetErp.Helpers.GraphQLQueryBuilder.GraphQLQueryBuilder;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Helpers.Cache
{
    public class MeasurementUnitCache : IEntityCache<MeasurementUnitGraphQLModel>, IBatchLoadableCache,
        IHandle<MeasurementUnitCreateMessage>,
        IHandle<MeasurementUnitUpdateMessage>,
        IHandle<MeasurementUnitDeleteMessage>
    {
        private readonly IRepository<MeasurementUnitGraphQLModel> _service;
        private readonly Lock _lock = new();

        private readonly ObservableCollection<MeasurementUnitGraphQLModel> _items = [];
        public ReadOnlyObservableCollection<MeasurementUnitGraphQLModel> Items { get; }
        public bool IsInitialized { get; private set; }

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadQuery = new(() =>
        {
            var fields = FieldSpec<PageType<MeasurementUnitGraphQLModel>>
                .Create()
                .SelectList(x => x.Entries, entries => entries
                    .Field(x => x.Id)
                    .Field(x => x.Name)
                )
                .Build();

            List<GraphQLQueryParameter> parameters =
            [
                new("pagination", "Pagination"),
                new("sort", "[MeasurementUnitSortInput]")
            ];
            var fragment = new GraphQLQueryFragment("measurementUnitsPage", parameters, fields, "PageResponse");
            var query = new QueryBuilder([fragment]).GetQuery();

            return (fragment, query);
        });

        // Alphabetical (by NAME) so combo boxes always show units in a predictable order.
        private static readonly object[] _defaultSort =
        [
            new { field = "NAME", direction = "ASC" }
        ];

        public MeasurementUnitCache(
            IRepository<MeasurementUnitGraphQLModel> service,
            IEventAggregator eventAggregator)
        {
            _service = service;
            eventAggregator.SubscribeOnUIThread(this);
            Items = new ReadOnlyObservableCollection<MeasurementUnitGraphQLModel>(_items);
        }

        public async Task EnsureLoadedAsync()
        {
            if (IsInitialized) return;

            try
            {
                var (fragment, query) = _loadQuery.Value;
                var variables = new GraphQLVariables()
                    .For(fragment, "pagination", new { PageSize = -1 })
                    .For(fragment, "sort", _defaultSort)
                    .Build();

                var result = await _service.GetPageAsync(query, variables);

                UiDispatcher.Invoke(() =>
                {
                    lock (_lock)
                    {
                        _items.Clear();
                        foreach (var item in result.Entries)
                        {
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
            variables.For(batchFragment, "sort", _defaultSort);
        }

        public void PopulateFromBatchResponse(JToken data)
        {
            var page = data.ToObject<PageType<MeasurementUnitGraphQLModel>>();
            if (page == null) return;

            UiDispatcher.Invoke(() =>
            {
                lock (_lock)
                {
                    _items.Clear();
                    foreach (var item in page.Entries)
                        _items.Add(item);
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

        public void Add(MeasurementUnitGraphQLModel item)
        {
            UiDispatcher.Invoke(() =>
            {
                lock (_lock)
                {
                    if (!_items.Any(x => x.Id == item.Id))
                        _items.Add(item);
                }
            });
        }

        public void Update(MeasurementUnitGraphQLModel item)
        {
            UiDispatcher.Invoke(() =>
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

        #region IHandle Implementations

        public Task HandleAsync(MeasurementUnitCreateMessage message, CancellationToken cancellationToken)
        {
            if (message.CreatedMeasurementUnit?.Entity != null)
            {
                Add(message.CreatedMeasurementUnit.Entity);
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(MeasurementUnitUpdateMessage message, CancellationToken cancellationToken)
        {
            if (message.UpdatedMeasurementUnit?.Entity != null)
            {
                Update(message.UpdatedMeasurementUnit.Entity);
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(MeasurementUnitDeleteMessage message, CancellationToken cancellationToken)
        {
            if (message.DeletedMeasurementUnit?.DeletedId > 0)
            {
                Remove(message.DeletedMeasurementUnit.DeletedId.Value);
            }
            return Task.CompletedTask;
        }

        #endregion
    }
}
