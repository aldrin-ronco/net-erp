using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
using Models.Billing;
using Models.Books;
using Models.Global;
using NetErp.Helpers.GraphQLQueryBuilder;
using Services.Books.DAL.PostgreSQL;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Xceed.Wpf.Toolkit.Primitives;
using static DevExpress.Drawing.Printing.Internal.DXPageSizeInfo;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Books.AccountingPresentations.ViewModels
{
    class AccountingPresentationMasterViewModel : Screen, IHandle<AccountingPresentationCreateMessage>,
        IHandle<AccountingPresentationUpdateMessage>, IHandle<AccountingPresentationDeleteMessage>
    {

        private readonly Helpers.Services.INotificationService _notificationService;

        private readonly IRepository<AccountingPresentationGraphQLModel> _accountingPresentationService;
        public new bool IsInitialized { get; set; } = false;

        private ObservableCollection<AccountingPresentationGraphQLModel> _accountingPresentations = [];
        public ObservableCollection<AccountingPresentationGraphQLModel> AccountingPresentations
        {
            get { return _accountingPresentations; }
            set
            {
                if (_accountingPresentations != value)
                {
                    _accountingPresentations = value;
                    NotifyOfPropertyChange(nameof(AccountingPresentations));
                }
            }
        }

       

        private AccountingPresentationGraphQLModel? _selectedItem;
        public AccountingPresentationGraphQLModel? SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                if (_selectedItem != value)
                {
                    _selectedItem = value;
                    NotifyOfPropertyChange(nameof(SelectedItem));
                    NotifyOfPropertyChange(nameof(CanDeleteAccountingPresentation));
                }
            }
        }

        private string _filterSearch = string.Empty;
        public string FilterSearch
        {
            get { return _filterSearch; }
            set
            {
                if (_filterSearch != value)
                {
                    _filterSearch = value;
                    NotifyOfPropertyChange(nameof(FilterSearch));
                    if (string.IsNullOrEmpty(value) || value.Length >= 3) _ = Task.Run(() => LoadAccountingPresentationsAsync());
                }
            }
        }

        public bool CanDeleteAccountingPresentation
        {
            get
            {
                return SelectedItem != null;
            }
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get { return _isBusy; }
            set
            {
                if(_isBusy != value)
                {
                    _isBusy = value;
                    NotifyOfPropertyChange(nameof(IsBusy));
                }
            }
        }

        public AccountingPresentationViewModel Context { get; set; }

        public ICommand? _createAccountingPresentationCommand;
        public ICommand CreateAccountingPresentationCommand
        {
            get
            {
                if (_createAccountingPresentationCommand is null) _createAccountingPresentationCommand = new AsyncCommand(CreateAccountingPresentationAsync);
                return _createAccountingPresentationCommand;
            }

        }

        public async Task CreateAccountingPresentationAsync()
        {
            await Context.ActivateDetailForNewAsync();
        }

        private ICommand? _deleteAccountingPresentationCommand;
        public ICommand DeleteAccountingPresentationCommand
        {
            get
            {
                if (_deleteAccountingPresentationCommand is null) _deleteAccountingPresentationCommand = new AsyncCommand(DeleteAccountingPresentationAsync);
                return _deleteAccountingPresentationCommand;
            }
        }

        public async Task DeleteAccountingPresentationAsync()
        {
            try
            {

                if (SelectedItem is null) return;
                int id = SelectedItem.Id;
                this.IsBusy = true;
                this.Refresh();

                string query = GetCanDeleteAccountingPresentationQuery();

                object variables = new { canDeleteResponseId = SelectedItem.Id };

                var validation = await _accountingPresentationService.CanDeleteAsync(query, variables);

                if (validation.CanDelete)
                {
                    IsBusy = false;
                    if (ThemedMessageBox.Show("Atención !", "¿Confirma que desea eliminar el registro seleccionado?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No) return;
                }
                else
                {
                    IsBusy = false;
                    ThemedMessageBox.Show("Atención !", "El registro no puede ser eliminado" +
                        (char)13 + (char)13 + validation.Message, MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                IsBusy = true;
                DeleteResponseType deletedAccountingPresentation = await Task.Run(() => this.ExecuteDeleteAccountingPresentationAsync(id));

                if (!deletedAccountingPresentation.Success)
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"No pudo ser eliminado el registro \n\n {deletedAccountingPresentation.Message} \n\n Verifica la información e intenta más tarde.");
                    return;
                }

                await Context.EventAggregator.PublishOnUIThreadAsync(new AccountingPresentationDeleteMessage { DeletedAccountingPresentation = deletedAccountingPresentation });

                NotifyOfPropertyChange(nameof(CanDeleteAccountingPresentation));

            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
            
        }

        public string GetCanDeleteAccountingPresentationQuery()
        {
            var fields = FieldSpec<CanDeleteType>
                .Create()
                .Field(f => f.CanDelete)
                .Field(f => f.Message)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");

            var fragment = new GraphQLQueryFragment("canDeleteAccountingPresentation", [parameter], fields, alias: "CanDeleteResponse");

            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery();
        }
        public async Task<DeleteResponseType> ExecuteDeleteAccountingPresentationAsync(int id)
        {
            try
            {

                 string query = GetDeleteAccountingPresentationQuery();
                
                object variables = new
                {
                    deleteResponseId = id
                };

                // Eliminar registros
                DeleteResponseType deletedRecord = await _accountingPresentationService.DeleteAsync<DeleteResponseType>(query, variables);
                return deletedRecord;
            }
            catch (Exception)
            {
                throw;
            }
        }
        public string GetDeleteAccountingPresentationQuery()
        {
            var fields = FieldSpec<DeleteResponseType>
                .Create()
                .Field(f => f.DeletedId)
                .Field(f => f.Message)
                .Field(f => f.Success)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");

            var fragment = new GraphQLQueryFragment("deleteAccountingPresentation", [parameter], fields, alias: "DeleteResponse");

            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }
        public async Task EditAccountingPresentationAsync()
        {
            await Context.ActivateDetailViewForEditAsync(SelectedItem!);
        }

        public async Task LoadAccountingPresentationsAsync()
        {
            try
            {
                IsBusy = true;
                string query = GetLoadAccountingPresentationsQuery();
                dynamic variables = new ExpandoObject();
                variables.pageResponseFilters = new ExpandoObject();
                variables.pageResponseFilters.name =  string.IsNullOrEmpty(FilterSearch) ? "" : FilterSearch.Trim().RemoveExtraSpaces();
                PageType<AccountingPresentationGraphQLModel> results = await _accountingPresentationService.GetPageAsync(query, variables);
                AccountingPresentations =[.. results.Entries];
            }
            catch (Exception ex)
            {
                await Execute.OnUIThreadAsync(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{GetCurrentMethodName.Get()} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return Task.CompletedTask;
                });
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task InitializeAsync()
        {

            try
            {
                IsBusy = true;
                Refresh();

                string query = GetLoadAccountingPresentationsQuery();

                //AccountingSource Filter
                dynamic variables = new ExpandoObject();
                variables.accountingPresentationsFilters = new ExpandoObject();

                variables.accountingPresentationsFilters.name = string.IsNullOrEmpty(FilterSearch) ? "" : FilterSearch.Trim().RemoveExtraSpaces();

                //Pagination
                variables.accountingPresentations = new ExpandoObject();
                variables.accountingPresentations.page = 1;
                variables.accountingPresentations.pageSize = 100;

                // Iniciar cronometro
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                PageType<AccountingPresentationGraphQLModel> results = await _accountingPresentationService.GetPageAsync(query, variables);
                AccountingPresentations = results.Entries;
                IsInitialized = true;
                
              
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
           
        }
        public string GetLoadAccountingPresentationsQuery()
        {

            var accountingPresentationFields = FieldSpec<PageType<AccountingPresentationGraphQLModel>>
              .Create()
              .SelectList(it => it.Entries, entries => entries
                  .Field(e => e.Id)

                  .Field(e => e.AllowsClosure)
                  .Field(e => e.Name)
                 
                  .Select(e => e.ClosureAccountingBook, acc => acc
                            .Field(c => c.Id)
                            .Field(c => c.Name)
                            )
              )
              .Field(o => o.PageNumber)
              .Field(o => o.PageSize)
              .Field(o => o.TotalPages)
              .Field(o => o.TotalEntries)
              .Build();


            var accountingPresentationPagParameters = new GraphQLQueryParameter("pagination", "Pagination");
            var accountingPresentationParameters = new GraphQLQueryParameter("filters", "AccountingPresentationFilters");

            var accountingPresentationFragment = new GraphQLQueryFragment("accountingPresentationsPage", [accountingPresentationPagParameters, accountingPresentationParameters], accountingPresentationFields,  "PageResponse");
            

            var builder = new GraphQLQueryBuilder([accountingPresentationFragment]);
            return builder.GetQuery();
        }
        protected override async Task OnInitializeAsync(CancellationToken cancellationToken)
        {
            await Execute.OnUIThreadAsync(async () =>
            {
                try
                {
                    await InitializeAsync();
                }
                catch (AsyncException ex)
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{ex.MethodOrigin} \r\n{ex.InnerException?.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{GetCurrentMethodName.Get()} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                }
            });

            await base.OnInitializeAsync(cancellationToken);
        }

        protected override async Task OnActivateAsync(CancellationToken cancellationToken)
        {
            await Execute.OnUIThreadAsync(async () =>
            {
                try
                {
                   if(!IsInitialized) await LoadAccountingPresentationsAsync();
                }
                catch (Exception)
                {
                    throw;
                }
            });
            await base.OnActivateAsync(cancellationToken);
        }

        public AccountingPresentationMasterViewModel(AccountingPresentationViewModel context, Helpers.Services.INotificationService notificationService,
            IRepository<AccountingPresentationGraphQLModel> accountingPresentationService)
        {
            Context = context;
            _notificationService = notificationService;
            _accountingPresentationService = accountingPresentationService;
            Context.EventAggregator.SubscribeOnPublishedThread(this);
        }

        public async Task HandleAsync(AccountingPresentationUpdateMessage message, CancellationToken cancellationToken)
        {
            try
            {
                await LoadAccountingPresentationsAsync();
                _notificationService.ShowSuccess(message.UpdatedAccountingPresentation.Message);
                return;
            }
            catch (Exception)
            {

                throw;
            }
        }
        public async Task HandleAsync(AccountingPresentationCreateMessage message, CancellationToken cancellationToken)
        {
            try
            {
                await LoadAccountingPresentationsAsync();
                _notificationService.ShowSuccess(message.CreatedAccountingPresentation.Message);
                return;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task HandleAsync(AccountingPresentationDeleteMessage message, CancellationToken cancellationToken)
        {
            try
            {
                await LoadAccountingPresentationsAsync();
                _notificationService.ShowSuccess(message.DeletedAccountingPresentation.Message);
                return;
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
