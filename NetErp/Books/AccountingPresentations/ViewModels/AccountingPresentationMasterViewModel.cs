using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Models.Billing;
using Models.Books;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Xceed.Wpf.Toolkit.Primitives;

namespace NetErp.Books.AccountingPresentations.ViewModels
{
    class AccountingPresentationMasterViewModel : Screen, IHandle<AccountingPresentationCreateMessage>,
        IHandle<AccountingPresentationUpdateMessage>, IHandle<AccountingPresentationDeleteMessage>
    {
        public IGenericDataAccess<AccountingPresentationGraphQLModel> AccountingPresentationService { get; set; } = IoC.Get<IGenericDataAccess<AccountingPresentationGraphQLModel>>();

        private readonly Helpers.Services.INotificationService _notificationService = IoC.Get<Helpers.Services.INotificationService>();

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

        private ObservableCollection<AccountingBookDTO> _accountingBooks = [];
        public ObservableCollection<AccountingBookDTO> AccountingBooks
        {
            get { return _accountingBooks; }
            set
            {
                if (_accountingBooks != value)
                {
                    _accountingBooks = value;
                    NotifyOfPropertyChange(nameof(AccountingBooks));
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
            await Context.ActivateDetailForNewAsync(AccountingBooks);
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
                IsBusy = true;
                int id = SelectedItem!.Id;


                string query = @"
                  query($id:Int!) {
                  CanDeleteModel: canDeleteAccountingPresentation(id:$id) {
                    canDelete
                    message
                  }
                }";

                dynamic variables = new ExpandoObject();
                variables.id = id;

                var validation = await AccountingPresentationService.CanDelete(query, variables);

                if (validation.CanDelete)
                {
                    IsBusy = false;
                    MessageBoxResult result = ThemedMessageBox.Show(title: "Atención!", text: $"¿Confirma que desea eliminar el registro {SelectedItem.Name}?", messageBoxButtons: MessageBoxButton.YesNo, image: MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes) return;
                }
                else
                {
                    IsBusy = false;
                    Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: "El registro no puede ser eliminado" +
                        (char)13 + (char)13 + validation.Message, messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                    return;
                }

                var deletedRecord = await ExecuteDeleteAccountingPresentationAsync(id);

                await Context.EventAggregator.PublishOnCurrentThreadAsync(new AccountingPresentationDeleteMessage() { DeletedAccountingPresentation = deletedRecord });

                NotifyOfPropertyChange(nameof(CanDeleteAccountingPresentation));
            }
            catch (AsyncException ex)
            {
                await Execute.OnUIThreadAsync(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{ex.MethodOrigin} \r\n{ex.InnerException?.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return Task.CompletedTask;
                });
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

        public async Task<AccountingPresentationGraphQLModel> ExecuteDeleteAccountingPresentationAsync(int id)
        {
            try
            {
                string query = @"
                    mutation($id:Int!) {
                      DeleteResponse: deleteAccountingPresentation(id:$id) {
                        id
                        name
                      }
                    }";
                dynamic variables = new ExpandoObject();
                variables.id = id;
                AccountingPresentationGraphQLModel result = await AccountingPresentationService.Delete(query, variables);
                SelectedItem = null;
                return result;
            }
            catch (AsyncException ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }

        public async Task EditAccountingPresentationAsync()
        {
            await Context.ActivateDetailViewForEditAsync(SelectedItem!, AccountingBooks);
        }

        public async Task LoadAccountingPresentationsAsync()
        {
            try
            {
                IsBusy = true;
                string query = @"query ($filter: AccountingPresentationFilterInput) {
                  ListResponse: accountingPresentations(filter: $filter) {
                    id
                    name
                    allowsAccountingClosure
                    accountingBookClosure {
                      id
                      name
                    }
                    accountingBooks {
                      id
                      name
                    }
                  }
                }";
                dynamic variables = new ExpandoObject();
                variables.filter = new ExpandoObject();
                variables.filter.name = new ExpandoObject();
                variables.filter.name.@operator = "like";
                variables.filter.name.value = string.IsNullOrEmpty(FilterSearch) ? "" : FilterSearch.Trim().RemoveExtraSpaces();
                var results = await AccountingPresentationService.GetList(query, variables);
                AccountingPresentations = new ObservableCollection<AccountingPresentationGraphQLModel>(results);
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
                string query = @"query ($filter: AccountingPresentationFilterInput) {
                  accountingPresentations(filter: $filter) {
                    id
                    name
                    allowsAccountingClosure
                    accountingBookClosure {
                      id
                      name
                    }
                    accountingBooks {
                      id
                      name
                    }
                  }
                  accountingBooks {
                    id
                    name
                  }
                }";
                dynamic variables = new ExpandoObject();
                variables.filter = new ExpandoObject();
                variables.filter.name = new ExpandoObject();
                variables.filter.name.@operator = "like";
                variables.filter.name.value = string.IsNullOrEmpty(FilterSearch) ? "" : FilterSearch.Trim().RemoveExtraSpaces();
                var results = await AccountingPresentationService.GetDataContext<AccountingPresentationDataContext>(query, variables);
                AccountingPresentations = new ObservableCollection<AccountingPresentationGraphQLModel>(results.AccountingPresentations);
                AccountingBooks = Context.AutoMapper.Map<ObservableCollection<AccountingBookDTO>>(results.AccountingBooks);
                IsInitialized = true;
            }
            catch (AsyncException ex)
            {
                throw new AsyncException(innerException: ex);
            }
            finally
            {
                IsBusy = false;
            }
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

        public AccountingPresentationMasterViewModel(AccountingPresentationViewModel context)
        {
            Context = context;
            Context.EventAggregator.SubscribeOnPublishedThread(this);
        }

        public async Task HandleAsync(AccountingPresentationUpdateMessage message, CancellationToken cancellationToken)
        {
            try
            {
                await LoadAccountingPresentationsAsync();
                _notificationService.ShowSuccess("Presentación contable actualizada correctamente");
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
                _notificationService.ShowSuccess("Presentación contable creada correctamente");
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
                _notificationService.ShowSuccess("Presentación contable eliminada correctamente");
                return;
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
