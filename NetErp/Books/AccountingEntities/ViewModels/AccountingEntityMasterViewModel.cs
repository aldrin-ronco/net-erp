using Caliburn.Micro;
using Common.Extensions;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Models.Books;
using NetErp.Helpers;
using System;
using Extensions.Books;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using DTOLibrary.Books;
using System.Dynamic;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Xpf;
using DevExpress.Xpf.Data;
using Services.Books.DAL.PostgreSQL;
using Common.Helpers;
using Microsoft.VisualStudio.Threading;
using Newtonsoft.Json.Linq;
using Models.Billing;
using Models.Suppliers;

namespace NetErp.Books.AccountingEntities.ViewModels
{
    public class AccountingEntityMasterViewModel : ViewModelBase, IHandle<CustomerCreateMessage>, IHandle<SupplierCreateMessage>
    {

        public readonly IGenericDataAccess<AccountingEntityGraphQLModel> AccountingEntityService = IoC.Get<IGenericDataAccess<AccountingEntityGraphQLModel>>();
        // Context
        private AccountingEntityViewModel _context;
        public AccountingEntityViewModel Context
        {
            get { return _context; }
            set
            {
                SetValue(ref _context, value);
            }
        }

        /// <summary>
        /// Establece cuando la aplicacion esta ocupada
        /// </summary>
        private bool _isBusy;
        public bool IsBusy
        {
            get { return _isBusy; }
            set
            {
                SetValue(ref _isBusy, value);          
            }
        }

        #region Paginacion
        /// <summary>
        /// PageIndex
        /// </summary>
        private int _pageIndex = 1; // DevExpress first page is index zero
        public int PageIndex
        {
            get { return _pageIndex; }
            set
            {
                SetValue(ref _pageIndex, value);
            }
        }

        /// <summary>
        /// PageSize
        /// </summary>
        private int _pageSize = 50; // Default PageSize 50
        public int PageSize
        {
            get { return _pageSize; }
            set
            {
                SetValue(ref _pageSize, value);
            }
            
        }


        /// <summary>
        /// TotalCount
        /// </summary>
        private int _totalCount = 0;
        public int TotalCount
        {
            get { return _totalCount; }
            set
            {
                SetValue(ref _totalCount, value);
            }
        }

        /// <summary>
        /// PaginationCommand para controlar evento
        /// </summary>
        private ICommand _paginationCommand;
        public ICommand PaginationCommand
        {
            get
            {
                if (_paginationCommand == null) this._paginationCommand = new AsyncCommand(ExecuteChangeIndex, CanExecuteChangeIndex);
                return _paginationCommand;
            }
        }

        private ICommand _createAccountingEntityCommand;
        public ICommand CreateAccountingEntityCommand
        {
            get
            {
                if (_createAccountingEntityCommand is null) _createAccountingEntityCommand = new AsyncCommand(CreateAccountingEntity, CanCreateAccountingEntity);
                return _createAccountingEntityCommand;
            }

        }

        private ICommand _deleteAccountingEntityCommand;
        public ICommand DeleteAccountingEntityCommand
        {
            get
            {
                if (_deleteAccountingEntityCommand is null) _deleteAccountingEntityCommand = new AsyncCommand(DeleteAccountingEntity, CanDeleteAccountingEntity);
                return _deleteAccountingEntityCommand;
            }
        }

        #endregion

        #region Propiedades

        // Tiempo de respuesta
        private string _responseTime;
        public string ResponseTime
        {
            get { return _responseTime; }
            set
            {
                SetValue(ref _responseTime, value);
            }
        }

        // Filtro de busqueda
        private string _filterSearch = "";
        public string FilterSearch
        {
            get { return _filterSearch; }
            set
            {
                SetValue(ref _filterSearch, value, changedCallback: OnFilterSearchChanged);                   
            }
        }

        public async void OnFilterSearchChanged()
        {
            // Solo ejecutamos la busqueda si esta vacio el filtro o si hay por lo menos 3 caracteres digitados
            if (string.IsNullOrEmpty(FilterSearch) || FilterSearch.Length >= 3)
            {
                IsBusy = true;
                PageIndex = 1;
                await LoadAccountingEntities();
                IsBusy = false;
            };
        }
        public bool CanCreateAccountingEntity() => !IsBusy;

        #endregion

        #region Colecciones

        private AccountingEntityDTO? _selectedAccountingEntity;
        public AccountingEntityDTO? SelectedAccountingEntity
        {
            get { return _selectedAccountingEntity; }
            set
            {
                SetValue(ref _selectedAccountingEntity, value, changedCallback: OnSelecteAccountingEntityChanged);
            }
        }

        public void OnSelecteAccountingEntityChanged()
        {
            RaisePropertyChanged(nameof(CanDeleteAccountingEntity));
        }

        private ObservableCollection<AccountingEntityDTO> _accountingEntities = [];
        public ObservableCollection<AccountingEntityDTO> AccountingEntities
        {
            get { return this._accountingEntities; }
            set
            {
                SetValue(ref _accountingEntities, value, changedCallback: OnAccountingEntitiesChanged); 
            }
        }

        public void OnAccountingEntitiesChanged()
        {
            RaisePropertyChanged(nameof(CanDeleteAccountingEntity));
        }

        #endregion


        [Command]
        public async void Initialize()
        {
            if (AccountingEntities.Count > 0) return;
            try
            {
                IsBusy = true;
                await LoadAccountingEntities();
            }
            catch (Exception)
            {

                throw;
            }
            finally
            {
                IsBusy = false;
            }
        }
        public AccountingEntityMasterViewModel(AccountingEntityViewModel context)
        {
            try
            {
                Context = context;
                Messenger.Default.Register<AccountingEntityCreateMessage>(this, OnAccountingEntityCreateMessage);
                Messenger.Default.Register<AccountingentityUpdateMessage>(this, OnAccountingEntityUpdateMessage);
                Messenger.Default.Register<AccountingEntityDeleteMessage>(this, OnAccountingEntityDeleteMessage);
                Context.EventAggregator.SubscribeOnUIThread(this);
            }
            catch (Exception)
            {

                throw;
            }

        }

        private async Task ExecuteChangeIndex()
        {
            IsBusy = true;
            await LoadAccountingEntities();
            IsBusy = false;
        }
        private bool CanExecuteChangeIndex()
        {
            return true;
        }

        #region Metodos 

        public async Task LoadAccountingEntities()
        {

            try
            {
                string query = @"
                query ($filter: AccountingEntityFilterInput) {
                  PageResponse:accountingEntityPage(filter: $filter) {
		                count
                        rows {
                            id
                            identificationNumber
                            verificationDigit
                            captureType
                            businessName
                            firstName
                            middleName
                            firstLastName
                            middleLastName
                            phone1
                            phone2
                            cellPhone1
                            cellPhone2
                            address
                            regime
                            fullName
                            tradeName
                            searchName
                            telephonicInformation
                            commercialCode
                            identificationType {
                               id
                            }
                            country {
                               id 
                            }
                            department {
                               id
                            }
                            city {
                               id 
                            }
                            emails {
                              id
                              name
                              email
                            }
                        }
                    }
                 }";

                dynamic variables = new ExpandoObject();
                variables.filter = new ExpandoObject();
                variables.filter.Pagination = new ExpandoObject();
                variables.filter.Pagination.Page = PageIndex;
                variables.filter.Pagination.PageSize = PageSize;
                variables.filter.QueryFilter = FilterSearch == "" ? "" : $"WHERE entity.identification_number like '%{FilterSearch.Trim().Replace(" ", "%")}%' OR entity.search_name like '%{FilterSearch.Trim().Replace(" ", "%")}%' ";
                // Iniciar cronometro
                Stopwatch stopwatch = new();
                stopwatch.Start();

                var source = await AccountingEntityService.GetPage(query, variables);
                TotalCount = source.PageResponse.Count;
                AccountingEntities = Context.AutoMapper.Map<ObservableCollection<AccountingEntityDTO>>(source.PageResponse.Rows);
                stopwatch.Stop();

                // Detener cronometro
                ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => DXMessageBox.Show(caption: "Atención!", messageBoxText: $"{this.GetType().Name}.{(currentMethod is null ? "LoadAccountingEntities" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", button: MessageBoxButton.OK, icon: MessageBoxImage.Error));
            }
        }

        public async Task EditAccountingEntity()
        {
            try
            {
                IsBusy = true;
                await Task.Run(() => ExecuteEditAccountingEntity());
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => DXMessageBox.Show(caption: "Atención!", messageBoxText: $"{this.GetType().Name}.{(currentMethod is null ? "EditAccountingEntity" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", button: MessageBoxButton.OK, icon: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task ExecuteEditAccountingEntity()
        {
            await Context.ActivateDetailViewForEdit(SelectedAccountingEntity);
        }

        public async Task CreateAccountingEntity()
        {
            try
            {
                IsBusy = true;
                await Task.Run(() => ExecuteCreateAccountingEntity());
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task ExecuteCreateAccountingEntity()
        {
            await Context.ActivateDetailViewForNew(); // Mostrar la Vista
        }

        public async Task DeleteAccountingEntity()
        {
            try
            {
                IsBusy = true;

                int id = SelectedAccountingEntity.Id;

                string query = @"
                query($id:Int!) {
                  CanDeleteModel: canDeleteAccountingEntity(id:$id) {
                    canDelete
                    message
                  }
                }";

                object variables = new { Id = id };

                var validation = await this.AccountingEntityService.CanDelete(query, variables);

                if (validation.CanDelete)
                {
                    IsBusy = false;
                    MessageBoxResult result = DXMessageBox.Show(caption: "Atención!", messageBoxText: "¿Confirma que desea eliminar la cuenta contable?", button: MessageBoxButton.YesNo, icon: MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes) return;
                }
                else
                {
                    IsBusy = false;
                    Application.Current.Dispatcher.Invoke(() => DXMessageBox.Show(caption: "Atención!", messageBoxText: "El tercero no puede ser eliminado" +
                        (char)13 + (char)13 + validation.Message, button: MessageBoxButton.OK, icon: MessageBoxImage.Error));
                    return;
                }

                IsBusy = true;

                var deletedAccountingEntity = await ExecuteDeleteAccountingEntity(id);

                Messenger.Default.Send(new AccountingEntityDeleteMessage() { DeletedAccountingEntity = deletedAccountingEntity});

                // Desactivar opcion de eliminar registros
                RaisePropertyChanged(nameof(CanDeleteAccountingEntity));

                IsBusy = false;
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                Application.Current.Dispatcher.Invoke(() => DXMessageBox.Show(caption: "Atención!", messageBoxText: $"{this.GetType().Name}.{(currentMethod is null ? "DeleteAccountingEntity" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", button: MessageBoxButton.OK, icon: MessageBoxImage.Error));
            }

        }

        public async Task<AccountingEntityGraphQLModel> ExecuteDeleteAccountingEntity(int id)
        {
            try
            {
                string query = @"
                mutation ($id: Int!) {
                  DeleteResponse: deleteAccountingEntity(id: $id) {
                    id
                  }
                }";
                object variables = new { Id = id };
                var deletedEntity = await this.AccountingEntityService.Delete(query, variables);
                this.SelectedAccountingEntity = null;
                return deletedEntity;
            }
            catch (Exception)
            {
                throw;
            }
        }
        public async void OnAccountingEntityDeleteMessage(AccountingEntityDeleteMessage message)
        {
            await OnAccountingEntityDeleteMessageAsync(message);
        }

        public async Task OnAccountingEntityDeleteMessageAsync(AccountingEntityDeleteMessage message)
        {
            AccountingEntityDTO entityToDelete = this.AccountingEntities.Where(x => x.Id == message.DeletedAccountingEntity.Id).FirstOrDefault();
            if (entityToDelete != null) AccountingEntities.RemoveById(message.DeletedAccountingEntity.Id);
            await LoadAccountingEntities();
        }

        public async void OnAccountingEntityCreateMessage(AccountingEntityCreateMessage message)
        {
            await OnAccountingEntityCreateMessageAsync(message);
        }

        public async Task OnAccountingEntityCreateMessageAsync(AccountingEntityCreateMessage message)
        {
            PageIndex = 1;
            await LoadAccountingEntities();
        }

        public async void OnAccountingEntityUpdateMessage(AccountingentityUpdateMessage message)
        {
            await OnAccountingEntityUpdateMessageAsync(message);
        }
        async Task OnAccountingEntityUpdateMessageAsync(AccountingentityUpdateMessage message)
        {
            PageIndex = 1;
            await LoadAccountingEntities();
        }

        public async Task HandleAsync(CustomerCreateMessage message, CancellationToken cancellationToken)
        {
            PageIndex = 1;
            await LoadAccountingEntities();
        }

        public async Task HandleAsync(SupplierCreateMessage message, CancellationToken cancellationToken)
        {
            PageIndex = 1;
            await LoadAccountingEntities();
        }

        public bool CanDeleteAccountingEntity
        {
            get
            {
                if (SelectedAccountingEntity is null) return false;
                return true;
            }
        }

        #endregion
    }
}
