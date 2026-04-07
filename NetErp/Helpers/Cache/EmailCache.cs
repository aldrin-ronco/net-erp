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
using static Models.Global.EmailGraphQLModel;
using static Models.Global.GraphQLResponseTypes;
using QueryBuilder = NetErp.Helpers.GraphQLQueryBuilder.GraphQLQueryBuilder;

namespace NetErp.Helpers.Cache
{
    public class EmailCache : IEntityCache<EmailGraphQLModel>, IBatchLoadableCache,
        IHandle<EmailCreateMessage>,
        IHandle<EmailUpdateMessage>,
        IHandle<EmailDeleteMessage>
    {
        private readonly IRepository<EmailGraphQLModel> _service;
        private readonly Lock _lock = new();
        private readonly ObservableCollection<EmailGraphQLModel> _items = [];
        public ReadOnlyObservableCollection<EmailGraphQLModel> Items { get; }
        public bool IsInitialized { get; private set; }

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadQuery = new(() =>
        {
            var fields = FieldSpec<PageType<EmailGraphQLModel>>
                .Create()
                .SelectList(x => x.Entries, entries => entries
                    .Field(x => x.Id)
                    .Field(x => x.Description)
                    .Field(x => x.Email)
                    .Field(x => x.IsActive))
                .Build();

            var parameter = new GraphQLQueryParameter("pagination", "Pagination");
            var fragment = new GraphQLQueryFragment("emailsPage", [parameter], fields, "PageResponse");
            return (fragment, new QueryBuilder([fragment]).GetQuery());
        });

        public EmailCache(
            IRepository<EmailGraphQLModel> service,
            IEventAggregator eventAggregator)
        {
            _service = service;
            eventAggregator.SubscribeOnUIThread(this);
            Items = new ReadOnlyObservableCollection<EmailGraphQLModel>(_items);
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

                PageType<EmailGraphQLModel> result = await _service.GetPageAsync(query, variables);

                UiDispatcher.Invoke(() =>
                {
                    lock (_lock)
                    {
                        _items.Clear();
                        foreach (EmailGraphQLModel item in result.Entries)
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
            PageType<EmailGraphQLModel>? page = data.ToObject<PageType<EmailGraphQLModel>>();
            if (page == null) return;

            UiDispatcher.Invoke(() =>
            {
                lock (_lock)
                {
                    _items.Clear();
                    foreach (EmailGraphQLModel item in page.Entries)
                        _items.Add(item);
                    IsInitialized = true;
                }
            });
        }

        #endregion

        public void Add(EmailGraphQLModel item)
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

        public void Update(EmailGraphQLModel item)
        {
            UiDispatcher.Invoke(() =>
            {
                lock (_lock)
                {
                    EmailGraphQLModel? existing = _items.FirstOrDefault(x => x.Id == item.Id);
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
            UiDispatcher.Invoke(() =>
            {
                lock (_lock)
                {
                    EmailGraphQLModel? item = _items.FirstOrDefault(x => x.Id == id);
                    if (item != null) _items.Remove(item);
                }
            });
        }

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

        public Task HandleAsync(EmailCreateMessage message, CancellationToken cancellationToken)
        {
            if (message.CreatedEmail.Entity != null) Add(message.CreatedEmail.Entity);
            return Task.CompletedTask;
        }

        public Task HandleAsync(EmailUpdateMessage message, CancellationToken cancellationToken)
        {
            if (message.UpdatedEmail.Entity != null) Update(message.UpdatedEmail.Entity);
            return Task.CompletedTask;
        }

        public Task HandleAsync(EmailDeleteMessage message, CancellationToken cancellationToken)
        {
            if (message.DeletedEmail.DeletedId is > 0) Remove(message.DeletedEmail.DeletedId.Value);
            return Task.CompletedTask;
        }
    }
}
