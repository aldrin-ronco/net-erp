using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using Models.Global;
using NetErp.Helpers.GraphQLQueryBuilder;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Models.Global.AccessProfileGraphQLModel;
using static Models.Global.GraphQLResponseTypes;
using QueryBuilder = NetErp.Helpers.GraphQLQueryBuilder.GraphQLQueryBuilder;
using System.Windows;

namespace NetErp.Helpers.Cache
{
    public class AccessProfileCache : IEntityCache<AccessProfileGraphQLModel>, IBatchLoadableCache,
        IHandle<AccessProfileCreateMessage>,
        IHandle<AccessProfileUpdateMessage>,
        IHandle<AccessProfileDeleteMessage>
    {
        private readonly IRepository<AccessProfileGraphQLModel> _service;
        private readonly Lock _lock = new();
        private readonly ObservableCollection<AccessProfileGraphQLModel> _items = [];
        public ReadOnlyObservableCollection<AccessProfileGraphQLModel> Items { get; }
        public bool IsInitialized { get; private set; }

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadQuery = new(() =>
        {
            var fields = FieldSpec<PageType<AccessProfileGraphQLModel>>
                .Create()
                .SelectList(x => x.Entries, entries => entries
                    .Field(x => x.Id)
                    .Field(x => x.Name)
                    .Field(x => x.Description)
                    .Field(x => x.IsActive)
                    .Field(x => x.IsSystemAdmin))
                .Build();

            var parameter = new GraphQLQueryParameter("pagination", "Pagination");
            var fragment = new GraphQLQueryFragment("accessProfilesPage", [parameter], fields, "PageResponse");
            return (fragment, new QueryBuilder([fragment]).GetQuery());
        });

        public AccessProfileCache(
            IRepository<AccessProfileGraphQLModel> service,
            IEventAggregator eventAggregator)
        {
            _service = service;
            eventAggregator.SubscribeOnUIThread(this);
            Items = new ReadOnlyObservableCollection<AccessProfileGraphQLModel>(_items);
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

                PageType<AccessProfileGraphQLModel> result = await _service.GetPageAsync(query, variables);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    lock (_lock)
                    {
                        _items.Clear();
                        foreach (AccessProfileGraphQLModel item in result.Entries)
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
            PageType<AccessProfileGraphQLModel>? page = data.ToObject<PageType<AccessProfileGraphQLModel>>();
            if (page == null) return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                lock (_lock)
                {
                    _items.Clear();
                    foreach (AccessProfileGraphQLModel item in page.Entries)
                        _items.Add(item);
                    IsInitialized = true;
                }
            });
        }

        #endregion

        public void Add(AccessProfileGraphQLModel item)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                lock (_lock)
                {
                    if (!_items.Any(x => x.Id == item.Id))
                        _items.Add(item);
                }
            });
        }

        public void Update(AccessProfileGraphQLModel item)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                lock (_lock)
                {
                    AccessProfileGraphQLModel? existing = _items.FirstOrDefault(x => x.Id == item.Id);
                    if (existing != null)
                    {
                        int index = _items.IndexOf(existing);
                        _items[index] = item;
                    }
                }
            });
        }

        public void Remove(int id)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                lock (_lock)
                {
                    AccessProfileGraphQLModel? item = _items.FirstOrDefault(x => x.Id == id);
                    if (item != null) _items.Remove(item);
                }
            });
        }

        public void Clear()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                lock (_lock)
                {
                    _items.Clear();
                    IsInitialized = false;
                }
            });
        }

        public Task HandleAsync(AccessProfileCreateMessage message, CancellationToken cancellationToken)
        {
            if (message.CreatedAccessProfile.Entity != null) Add(message.CreatedAccessProfile.Entity);
            return Task.CompletedTask;
        }

        public Task HandleAsync(AccessProfileUpdateMessage message, CancellationToken cancellationToken)
        {
            if (message.UpdatedAccessProfile.Entity != null) Update(message.UpdatedAccessProfile.Entity);
            return Task.CompletedTask;
        }

        public Task HandleAsync(AccessProfileDeleteMessage message, CancellationToken cancellationToken)
        {
            if (message.DeletedAccessProfile.DeletedId is > 0) Remove(message.DeletedAccessProfile.DeletedId.Value);
            return Task.CompletedTask;
        }
    }
}
