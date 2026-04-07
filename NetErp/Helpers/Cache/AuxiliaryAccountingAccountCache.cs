using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using Models.Books;
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
    /// <summary>
    /// Cache para cuentas contables auxiliares (código >= 8 dígitos).
    /// </summary>
    public class AuxiliaryAccountingAccountCache : IEntityCache<AccountingAccountGraphQLModel>, IBatchLoadableCache,
        IHandle<AccountingAccountCreateMessage>,
        IHandle<AccountingAccountUpdateMessage>,
        IHandle<AccountingAccountDeleteMessage>
    {
        private readonly IRepository<AccountingAccountGraphQLModel> _service;
        private readonly Lock _lock = new();

        private readonly ObservableCollection<AccountingAccountGraphQLModel> _items = [];
        public ReadOnlyObservableCollection<AccountingAccountGraphQLModel> Items { get; }
        public bool IsInitialized { get; private set; }

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadQuery = new(() =>
        {
            var fields = FieldSpec<PageType<AccountingAccountGraphQLModel>>
                .Create()
                .SelectList(x => x.Entries, entries => entries
                    .Field(x => x.Id)
                    .Field(x => x.Code)
                    .Field(x => x.Name)
                )
                .Build();

            var paginationParam = new GraphQLQueryParameter("pagination", "Pagination");
            var filtersParam = new GraphQLQueryParameter("filters", "AccountingAccountFilters");
            var fragment = new GraphQLQueryFragment("AccountingAccountsPage", [paginationParam, filtersParam], fields, "PageResponse");
            var query = new QueryBuilder([fragment]).GetQuery();

            return (fragment, query);
        });

        public AuxiliaryAccountingAccountCache(
            IRepository<AccountingAccountGraphQLModel> service,
            IEventAggregator eventAggregator)
        {
            _service = service;
            Items = new ReadOnlyObservableCollection<AccountingAccountGraphQLModel>(_items);
            eventAggregator.SubscribeOnUIThread(this);
        }

        public async Task EnsureLoadedAsync()
        {
            if (IsInitialized) return;

            try
            {
                var (fragment, query) = _loadQuery.Value;
                var variables = new GraphQLVariables()
                    .For(fragment, "pagination", new { PageSize = -1 })
                    .For(fragment, "filters", new { OnlyAuxiliaryAccounts = true })
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
            variables.For(batchFragment, "filters", new { OnlyAuxiliaryAccounts = true });
        }

        public void PopulateFromBatchResponse(JToken data)
        {
            var page = data.ToObject<PageType<AccountingAccountGraphQLModel>>();
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

        public void Add(AccountingAccountGraphQLModel item)
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

        public void Update(AccountingAccountGraphQLModel item)
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

        public Task HandleAsync(AccountingAccountCreateMessage message, CancellationToken cancellationToken)
        {
            // Solo agregar si es cuenta auxiliar (código >= 8 dígitos)
            if (message.CreatedAccountingAccount != null &&
                !string.IsNullOrEmpty(message.CreatedAccountingAccount.Code) &&
                message.CreatedAccountingAccount.Code.Length >= 8)
            {
                Add(message.CreatedAccountingAccount);
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(AccountingAccountUpdateMessage message, CancellationToken cancellationToken)
        {
            // Usar UpsertAccount.Entity si existe, sino UpdatedAccountingAccount
            var account = message.UpsertAccount?.Entity ?? message.UpdatedAccountingAccount;

            if (account != null)
            {
                bool isAuxiliary = !string.IsNullOrEmpty(account.Code) && account.Code.Length >= 8;
                var existing = _items.FirstOrDefault(x => x.Id == account.Id);

                if (isAuxiliary)
                {
                    if (existing != null)
                        Update(account);
                    else
                        Add(account);
                }
                else if (existing != null)
                {
                    // Si ya no es auxiliar, remover del cache
                    Remove(account.Id);
                }
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(AccountingAccountDeleteMessage message, CancellationToken cancellationToken)
        {
            if (message.DeletedResponseType?.DeletedId > 0)
            {
                Remove(message.DeletedResponseType.DeletedId.Value);
            }
            else if (message.DeletedAccountingAccount != null)
            {
                Remove(message.DeletedAccountingAccount.Id);
            }
            return Task.CompletedTask;
        }

        #endregion
    }
}
