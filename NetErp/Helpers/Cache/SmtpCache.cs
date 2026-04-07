using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.XtraPrinting.Native;
using Models.Billing;
using Models.Books;
using Models.Global;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Models.Global.GraphQLResponseTypes;
using QueryBuilder = NetErp.Helpers.GraphQLQueryBuilder.GraphQLQueryBuilder;
using System.Windows;


namespace NetErp.Helpers.Cache
{
    public class SmtpCache : IEntityCache<SmtpGraphQLModel>,
        IHandle<SmtpCreateMessage>,
        IHandle<SmtpUpdateMessage>,
        IHandle<SmtpDeleteMessage>
    {
        private readonly IRepository<SmtpGraphQLModel> _service;
        private readonly Lock _lock = new();
        private readonly ObservableCollection<SmtpGraphQLModel> _items = [];
        public ReadOnlyObservableCollection<SmtpGraphQLModel> Items { get; }
        public bool IsInitialized { get; private set; }


        public SmtpCache(IRepository<SmtpGraphQLModel> service, IEventAggregator eventAggregator)
        {
            _service = service;
            eventAggregator.SubscribeOnUIThread(this);
            Items = new ReadOnlyObservableCollection<SmtpGraphQLModel>(_items);
        }
        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadQuery = new(() =>
        {
            var fields = FieldSpec<PageType<SmtpGraphQLModel>>
                .Create()
                .SelectList(x => x.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Host)
                    .Field(e => e.Name)
                    .Field(e => e.Port)
                )
                .Build();

            var paginationParam = new GraphQLQueryParameter("pagination", "Pagination");
            var filtersParam = new GraphQLQueryParameter("filters", "SmtpFilters");
            var fragment = new GraphQLQueryFragment("smtpsPage", [paginationParam, filtersParam], fields, "PageResponse");
            var query = new QueryBuilder([fragment]).GetQuery();

            return (fragment, query);
        });
        public async Task EnsureLoadedAsync()
        {
            if (IsInitialized) return;

            try
            {
                var (fragment, query) = _loadQuery.Value;
                var variables = new GraphQLVariables()
                    .For(fragment, "pagination", new { PageSize = 100 })
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

        public void Add(SmtpGraphQLModel item)
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

        public void Update(SmtpGraphQLModel item)
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
        #region IHandle Implementations

        public Task HandleAsync(SmtpUpdateMessage message, CancellationToken cancellationToken)
        {
            if (message.UpdatedSmtp?.Entity != null)
            {
                var existing = _items.FirstOrDefault(x => x.Id == message.UpdatedSmtp.Entity.Id);
                if (existing != null)
                    Update(message.UpdatedSmtp.Entity);
                else
                    Add(message.UpdatedSmtp.Entity);
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(SmtpCreateMessage message, CancellationToken cancellationToken)
        {
            if (message.CreatedSmtp?.Entity != null)
            {
                Add(message.CreatedSmtp.Entity);
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(SmtpDeleteMessage message, CancellationToken cancellationToken)
        {
            if (message.DeletedSmtp?.DeletedId > 0)
            {
                Remove(message.DeletedSmtp.DeletedId.Value);
            }
            return Task.CompletedTask;
        }
        #endregion
    }
}
