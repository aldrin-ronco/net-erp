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
using System.Windows;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Helpers.Cache
{
    /// <summary>
    /// Cache de resoluciones de autorización (DIAN) scoped al company actual via header company-id.
    /// Solo carga Id + Description (lo necesario para mostrar en ComboBox de selección).
    /// Sincronización en vivo via mensajes Create/Update/Delete del módulo AuthorizationSequence.
    /// </summary>
    public class AuthorizationSequenceCache : IEntityCache<AuthorizationSequenceGraphQLModel>, IBatchLoadableCache,
        IHandle<AuthorizationSequenceCreateMessage>,
        IHandle<AuthorizationSequenceUpdateMessage>,
        IHandle<AuthorizationSequenceDeleteMessage>
    {
        private readonly IRepository<AuthorizationSequenceGraphQLModel> _service;
        private readonly Lock _lock = new();

        private readonly ObservableCollection<AuthorizationSequenceGraphQLModel> _items = [];
        public ReadOnlyObservableCollection<AuthorizationSequenceGraphQLModel> Items { get; }
        public bool IsInitialized { get; private set; }

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadQuery = new(() =>
        {
            var fields = FieldSpec<PageType<AuthorizationSequenceGraphQLModel>>
                .Create()
                .SelectList(x => x.Entries, entries => entries
                    .Field(x => x.Id)
                    .Field(x => x.Description)
                )
                .Build();

            var paginationParam = new GraphQLQueryParameter("pagination", "Pagination");
            var filtersParam = new GraphQLQueryParameter("filters", "AuthorizationSequenceFilters");
            var fragment = new GraphQLQueryFragment("authorizationSequencesPage", [paginationParam, filtersParam], fields, "PageResponse");
            var query = new QueryBuilder([fragment]).GetQuery();

            return (fragment, query);
        });

        public AuthorizationSequenceCache(
            IRepository<AuthorizationSequenceGraphQLModel> service,
            IEventAggregator eventAggregator)
        {
            _service = service;
            eventAggregator.SubscribeOnUIThread(this);
            Items = new ReadOnlyObservableCollection<AuthorizationSequenceGraphQLModel>(_items);
        }

        public async Task EnsureLoadedAsync()
        {
            if (IsInitialized) return;

            try
            {
                var (fragment, query) = _loadQuery.Value;
                var variables = new GraphQLVariables()
                    .For(fragment, "pagination", new { PageSize = -1 })
                    .For(fragment, "filters", new { IsCurrent = true })
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

        #region IBatchLoadableCache

        public GraphQLQueryFragment LoadFragment => _loadQuery.Value.Fragment;

        public void ApplyVariables(GraphQLVariables variables, GraphQLQueryFragment batchFragment)
        {
            variables.For(batchFragment, "pagination", new { PageSize = -1 });
            variables.For(batchFragment, "filters", new { IsCurrent = true });
        }

        public void PopulateFromBatchResponse(JToken data)
        {
            var page = data.ToObject<PageType<AuthorizationSequenceGraphQLModel>>();
            if (page == null) return;

            Application.Current.Dispatcher.Invoke(() =>
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
            Application.Current.Dispatcher.Invoke(() =>
            {
                lock (_lock)
                {
                    _items.Clear();
                    IsInitialized = false;
                }
            });
        }

        public void Add(AuthorizationSequenceGraphQLModel item)
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

        public void Update(AuthorizationSequenceGraphQLModel item)
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

        public Task HandleAsync(AuthorizationSequenceCreateMessage message, CancellationToken cancellationToken)
        {
            var created = message.CreatedAuthorizationSequence?.Entity;
            if (created != null)
            {
                Add(created);
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(AuthorizationSequenceUpdateMessage message, CancellationToken cancellationToken)
        {
            var updated = message.UpdatedAuthorizationSequence?.Entity;
            if (updated != null)
            {
                Update(updated);
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(AuthorizationSequenceDeleteMessage message, CancellationToken cancellationToken)
        {
            if (message.DeletedAuthorizationSequence?.DeletedId > 0)
            {
                Remove(message.DeletedAuthorizationSequence.DeletedId.Value);
            }
            return Task.CompletedTask;
        }

        #endregion
    }
}
