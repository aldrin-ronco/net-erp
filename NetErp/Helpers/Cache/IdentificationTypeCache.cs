using Caliburn.Micro;
using Common.Interfaces;
using Models.Books;
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
using System.Windows;

namespace NetErp.Helpers.Cache
{
    public class IdentificationTypeCache : IEntityCache<IdentificationTypeGraphQLModel>, IBatchLoadableCache,
        IHandle<IdentificationTypeCreateMessage>,
        IHandle<IdentificationTypeUpdateMessage>,
        IHandle<IdentificationTypeDeleteMessage>
    {
        private readonly IRepository<IdentificationTypeGraphQLModel> _service;
        private readonly Lock _lock = new();

        private readonly ObservableCollection<IdentificationTypeGraphQLModel> _items = [];
        public ReadOnlyObservableCollection<IdentificationTypeGraphQLModel> Items { get; }
        public bool IsInitialized { get; private set; }

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadQuery = new(() =>
        {
            var fields = FieldSpec<PageType<IdentificationTypeGraphQLModel>>
                .Create()
                .SelectList(x => x.Entries, entries => entries
                    .Field(x => x.Id)
                    .Field(x => x.Name)
                    .Field(x => x.Code)
                    .Field(x => x.HasVerificationDigit)
                    .Field(x => x.AllowsLetters)
                    .Field(x => x.MinimumDocumentLength)
                )
                .Build();

            var parameter = new GraphQLQueryParameter("pagination", "Pagination");
            var fragment = new GraphQLQueryFragment("identificationTypesPage", [parameter], fields, "PageResponse");
            var query = new QueryBuilder([fragment]).GetQuery();

            return (fragment, query);
        });

        public IdentificationTypeCache(
            IRepository<IdentificationTypeGraphQLModel> service,
            IEventAggregator eventAggregator)
        {
            _service = service;
            eventAggregator.SubscribeOnUIThread(this);
            Items = new ReadOnlyObservableCollection<IdentificationTypeGraphQLModel>(_items);
        }

        public async Task EnsureLoadedAsync()
        {
            if (IsInitialized) return;

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

        #region IBatchLoadableCache

        public GraphQLQueryFragment LoadFragment => _loadQuery.Value.Fragment;

        public void ApplyVariables(GraphQLVariables variables, GraphQLQueryFragment batchFragment)
        {
            variables.For(batchFragment, "pagination", new { PageSize = -1 });
        }

        public void PopulateFromBatchResponse(JToken data)
        {
            var page = data.ToObject<PageType<IdentificationTypeGraphQLModel>>();
            if (page == null) return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                lock (_lock)
                {
                    _items.Clear();
                    foreach (var item in page.Entries)
                    {
                        _items.Add(item);
                    }
                    IsInitialized = true;
                }
            });
        }

        #endregion

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

        public void Add(IdentificationTypeGraphQLModel item)
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

        public void Update(IdentificationTypeGraphQLModel item)
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

        public Task HandleAsync(IdentificationTypeCreateMessage message, CancellationToken cancellationToken)
        {
            if (message.CreatedIdentificationType?.Entity != null)
            {
                Add(message.CreatedIdentificationType.Entity);
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(IdentificationTypeUpdateMessage message, CancellationToken cancellationToken)
        {
            var entity = message.UpdatedIdentificationType?.Entity;
            if (entity != null)
            {
                var existing = _items.FirstOrDefault(x => x.Id == entity.Id);
                if (existing != null)
                    Update(entity);
                else
                    Add(entity);
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(IdentificationTypeDeleteMessage message, CancellationToken cancellationToken)
        {
            if (message.DeletedIdentificationType?.DeletedId > 0)
            {
                Remove(message.DeletedIdentificationType.DeletedId.Value);
            }
            return Task.CompletedTask;
        }
    }
}
