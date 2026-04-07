using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
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
    public class CompanyCache : IEntityCache<CompanyGraphQLModel>, IBatchLoadableCache,
        IHandle<CompanyCreateMessage>,
        IHandle<CompanyUpdateMessage>,
        IHandle<CompanyDeleteMessage>
    {
        private readonly IRepository<CompanyGraphQLModel> _service;
        private readonly Lock _lock = new();

        private readonly ObservableCollection<CompanyGraphQLModel> _items = [];
        public ReadOnlyObservableCollection<CompanyGraphQLModel> Items { get; }
        public bool IsInitialized { get; private set; }

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadQuery = new(() =>
        {
            var fields = FieldSpec<PageType<CompanyGraphQLModel>>
                .Create()
                .SelectList(x => x.Entries, entries => entries
                    .Field(e => e.Id)
                    .Select(e => e.CompanyEntity, ce => ce.Field(c => c.SearchName)))
                .Build();

            var parameter = new GraphQLQueryParameter("pagination", "Pagination");
            var fragment = new GraphQLQueryFragment("companiesPage", [parameter], fields, "PageResponse");
            var query = new QueryBuilder([fragment]).GetQuery();

            return (fragment, query);
        });

        public CompanyCache(
            IRepository<CompanyGraphQLModel> service,
            IEventAggregator eventAggregator)
        {
            _service = service;
            eventAggregator.SubscribeOnUIThread(this);
            Items = new ReadOnlyObservableCollection<CompanyGraphQLModel>(_items);
        }

        public async Task EnsureLoadedAsync()
        {
            if (IsInitialized) return;

            try
            {
                var (fragment, query) = _loadQuery.Value;
                var variables = new GraphQLVariables()
                    .For(fragment, "pagination", new { PageSize = -1 })
                    .Build();

                var result = await _service.GetPageAsync(query, variables);

                UiDispatcher.Invoke(() =>
                {
                    lock (_lock)
                    {
                        _items.Clear();
                        foreach (var item in result.Entries)
                            _items.Add(item);
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
        }

        public void PopulateFromBatchResponse(JToken data)
        {
            var page = data.ToObject<PageType<CompanyGraphQLModel>>();
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

        public void Add(CompanyGraphQLModel item)
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

        public void Update(CompanyGraphQLModel item)
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

        public Task HandleAsync(CompanyCreateMessage message, CancellationToken cancellationToken)
        {
            if (message.CreatedCompany?.Entity != null)
                Add(message.CreatedCompany.Entity);
            return Task.CompletedTask;
        }

        public Task HandleAsync(CompanyUpdateMessage message, CancellationToken cancellationToken)
        {
            if (message.UpdatedCompany?.Entity != null)
                Update(message.UpdatedCompany.Entity);
            return Task.CompletedTask;
        }

        public Task HandleAsync(CompanyDeleteMessage message, CancellationToken cancellationToken)
        {
            if (message.DeletedCompany?.DeletedId > 0)
                Remove(message.DeletedCompany.DeletedId.Value);
            return Task.CompletedTask;
        }

        #endregion
    }
}
