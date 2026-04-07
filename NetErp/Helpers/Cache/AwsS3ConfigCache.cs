using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using Models.Global;
using NetErp.Helpers.GraphQLQueryBuilder;
using QueryBuilder = NetErp.Helpers.GraphQLQueryBuilder.GraphQLQueryBuilder;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Models.Global.GraphQLResponseTypes;
using System.Windows;

namespace NetErp.Helpers.Cache
{
    public class AwsS3ConfigCache : IEntityCache<AwsS3ConfigGraphQLModel>,
        IHandle<AwsS3ConfigCreateMessage>,
        IHandle<AwsS3ConfigUpdateMessage>,
        IHandle<AwsS3ConfigDeleteMessage>
    {
        private readonly IRepository<AwsS3ConfigGraphQLModel> _service;
        private readonly Lock _lock = new();

        private readonly ObservableCollection<AwsS3ConfigGraphQLModel> _items = [];
        public ReadOnlyObservableCollection<AwsS3ConfigGraphQLModel> Items { get; }
        public bool IsInitialized { get; private set; }

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadQuery = new(() =>
        {
            var fields = FieldSpec<PageType<AwsS3ConfigGraphQLModel>>
                .Create()
                .SelectList(x => x.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Description)
                    .Field(e => e.AccessKey)
                    .Field(e => e.Region)
                )
                .Build();

            var paginationParam = new GraphQLQueryParameter("pagination", "Pagination");
            var filtersParam = new GraphQLQueryParameter("filters", "AwsS3ConfigFilters");
            var fragment = new GraphQLQueryFragment("awsS3ConfigsPage", [paginationParam, filtersParam], fields, "PageResponse");
            var query = new QueryBuilder([fragment]).GetQuery();

            return (fragment, query);
        });

        public AwsS3ConfigCache(
            IRepository<AwsS3ConfigGraphQLModel> service,
            IEventAggregator eventAggregator)
        {
            _service = service;
            eventAggregator.SubscribeOnUIThread(this);
            Items = new ReadOnlyObservableCollection<AwsS3ConfigGraphQLModel>(_items);
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

                Application.Current.Dispatcher.Invoke(() =>
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

        public void Add(AwsS3ConfigGraphQLModel item)
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

        public void Update(AwsS3ConfigGraphQLModel item)
        {
            Application.Current.Dispatcher.Invoke(() =>
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
            Application.Current.Dispatcher.Invoke(() =>
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

        public Task HandleAsync(AwsS3ConfigCreateMessage message, CancellationToken cancellationToken)
        {
            if (message.CreatedAwsS3Config?.Entity != null)
            {
                Add(message.CreatedAwsS3Config.Entity);
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(AwsS3ConfigUpdateMessage message, CancellationToken cancellationToken)
        {
            if (message.UpdatedAwsS3Config?.Entity != null)
            {
                var existing = _items.FirstOrDefault(x => x.Id == message.UpdatedAwsS3Config.Entity.Id);
                if (existing != null)
                    Update(message.UpdatedAwsS3Config.Entity);
                else
                    Add(message.UpdatedAwsS3Config.Entity);
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(AwsS3ConfigDeleteMessage message, CancellationToken cancellationToken)
        {
            if (message.DeletedAwsS3Config?.DeletedId > 0)
            {
                Remove(message.DeletedAwsS3Config.DeletedId.Value);
            }
            return Task.CompletedTask;
        }

        #endregion
    }
}
