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

namespace NetErp.Helpers.Cache
{
    public class DianCertificateCache : IEntityCache<DianCertificateGraphQLModel>,
        IHandle<DianCertificateCreateMessage>,
        IHandle<DianCertificateUpdateMessage>,
        IHandle<DianCertificateDeleteMessage>
    {
        private readonly IRepository<DianCertificateGraphQLModel> _service;
        private readonly Lock _lock = new();

        private readonly ObservableCollection<DianCertificateGraphQLModel> _items = [];
        public ReadOnlyObservableCollection<DianCertificateGraphQLModel> Items { get; }
        public bool IsInitialized { get; private set; }

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadQuery = new(() =>
        {
            var fields = FieldSpec<PageType<DianCertificateGraphQLModel>>
                .Create()
                .SelectList(x => x.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.SerialNumber)
                    .Field(e => e.Subject)
                    .Field(e => e.ValidFrom)
                    .Field(e => e.ValidTo)
                )
                .Build();

            var paginationParam = new GraphQLQueryParameter("pagination", "Pagination");
            var filtersParam = new GraphQLQueryParameter("filters", "DianCertificateFilters");
            var fragment = new GraphQLQueryFragment("dianCertificatesPage", [paginationParam, filtersParam], fields, "PageResponse");
            var query = new QueryBuilder([fragment]).GetQuery();

            return (fragment, query);
        });

        public DianCertificateCache(
            IRepository<DianCertificateGraphQLModel> service,
            IEventAggregator eventAggregator)
        {
            _service = service;
            eventAggregator.SubscribeOnUIThread(this);
            Items = new ReadOnlyObservableCollection<DianCertificateGraphQLModel>(_items);
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
            UiDispatcher.Invoke(() =>
            {
                lock (_lock)
                {
                    _items.Clear();
                    IsInitialized = false;
                }
            });
        }

        public void Add(DianCertificateGraphQLModel item)
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

        public void Update(DianCertificateGraphQLModel item)
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

        public Task HandleAsync(DianCertificateCreateMessage message, CancellationToken cancellationToken)
        {
            if (message.CreatedCertificate?.Entity != null)
            {
                Add(message.CreatedCertificate.Entity);
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(DianCertificateUpdateMessage message, CancellationToken cancellationToken)
        {
            if (message.UpdatedCertificate?.Entity != null)
            {
                var existing = _items.FirstOrDefault(x => x.Id == message.UpdatedCertificate.Entity.Id);
                if (existing != null)
                    Update(message.UpdatedCertificate.Entity);
                else
                    Add(message.UpdatedCertificate.Entity);
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(DianCertificateDeleteMessage message, CancellationToken cancellationToken)
        {
            if (message.DeletedCertificate?.DeletedId > 0)
            {
                Remove(message.DeletedCertificate.DeletedId.Value);
            }
            return Task.CompletedTask;
        }

        #endregion
    }
}
