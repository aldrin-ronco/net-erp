using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Models.Books;
using Models.Global;
using NetErp.Books.AccountingAccountGroups.DTO;
using NetErp.Books.AccountingAccountGroups.ViewModels;
using NetErp.Books.AccountingEntities.ViewModels;
using Ninject.Activation;
using Services.Books.DAL.PostgreSQL;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using static DevExpress.Drawing.Printing.Internal.DXPageSizeInfo;

namespace NetErp.Books.WithholdingCertificateConfig.ViewModels
{
   public class WithholdingCertificateConfigMasterViewModel : Screen
    {
        public IGenericDataAccess<WithholdingCertificateConfigGraphQLModel> WithholdingCertificateConfigService { get; set; } = IoC.Get<IGenericDataAccess<WithholdingCertificateConfigGraphQLModel>>();
       

        public WithholdingCertificateConfigViewModel Context { get; set; }
        public WithholdingCertificateConfigMasterViewModel(WithholdingCertificateConfigViewModel context)
        {
            Context = context;
            Context.EventAggregator.SubscribeOnUIThread(this);
        }
        private ObservableCollection<WithholdingCertificateConfigGraphQLModel> _certificates;

        public ObservableCollection<WithholdingCertificateConfigGraphQLModel> Certificates
        {
            get { return _certificates; }
            set
            {
                if (_certificates != value)
                {
                    _certificates = value;
                    NotifyOfPropertyChange(nameof(Certificates));
                }
            }
        }

        private WithholdingCertificateConfigGraphQLModel? _selectedWithholdingCertificateConfigGraphQLModel;
        public WithholdingCertificateConfigGraphQLModel? SelectedWithholdingCertificateConfigGraphQLModel
        {
            get { return _selectedWithholdingCertificateConfigGraphQLModel; }
            set
            {
                if (_selectedWithholdingCertificateConfigGraphQLModel != value)
                {
                    _selectedWithholdingCertificateConfigGraphQLModel = value;
                    NotifyOfPropertyChange(nameof(_selectedWithholdingCertificateConfigGraphQLModel));
                    // NotifyOfPropertyChange(nameof(CanDelete_selectedWithholdingCertificateConfigGraphQLModel));
                }
            }
        }
        protected override void OnViewAttached(object view, object context)
        {
            base.OnViewAttached(view, context);
            _ = Task.Run(() => InitializeAsync());
        }
        private bool _isBusy;

        public bool IsBusy
        {
            get { return _isBusy; }
            set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    NotifyOfPropertyChange(nameof(IsBusy));
                }
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
                if (_pageIndex != value)
                {
                    _pageIndex = value;
                    NotifyOfPropertyChange(nameof(PageIndex));
                }
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
                if (_pageSize != value)
                {
                    _pageSize = value;
                    NotifyOfPropertyChange(nameof(PageSize));
                }
            }

        }
        // Tiempo de respuesta
        private string _responseTime;
        public string ResponseTime
        {
            get { return _responseTime; }
            set
            {
                if (_responseTime != value)
                {
                    _responseTime = value;
                    NotifyOfPropertyChange(nameof(ResponseTime));
                }
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
                if (_totalCount != value)
                {
                    _totalCount = value;
                    NotifyOfPropertyChange(nameof(TotalCount));
                }
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
                if (_paginationCommand == null) this._paginationCommand = new RelayCommand(CanExecuteChangeIndex, ExecuteChangeIndex);
                return _paginationCommand;
            }
        }
        private async void ExecuteChangeIndex(object parameter)
        {
            await LoadWithholdingCertificateConfig();
        }

        private bool CanExecuteChangeIndex(object parameter)
        {
            return true;
        }
       
       
        #endregion

        private ICommand _newCommand;

        public ICommand NewCommand
        {
            get
            {
                if (_newCommand is null) _newCommand = new AsyncCommand(NewAsync);
                return _newCommand;
            }
        }

        public async Task NewAsync()
        {
            try
            {
                IsBusy = true;
                Refresh();
                SelectedWithholdingCertificateConfigGraphQLModel = null;
                await Task.Run(() => ExecuteActivateWithholdingCertificateConfig());
                SelectedWithholdingCertificateConfigGraphQLModel = null;
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "NewWithholdingCertificateConfigEntity" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }
        public async Task EditWithholdingCertificateConfig()
        {
            try
            {
                IsBusy = true;
                Refresh();
                await Task.Run(() => ExecuteActivateWithholdingCertificateConfig());
                SelectedWithholdingCertificateConfigGraphQLModel = null;
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "EditWithholdingCertificateConfig" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }
        public async Task ExecuteActivateWithholdingCertificateConfig()
        {
            await Context.ActivateDetailViewForEdit(SelectedWithholdingCertificateConfigGraphQLModel);
        }
       
        public async Task InitializeAsync()
        {
           await LoadWithholdingCertificateConfig();
        }
        public async Task LoadWithholdingCertificateConfig()
        {
            try
            {
                this.Refresh();

                // Iniciar cronometro
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                IsBusy = true;
                string query = @"
               query( $filter: WithholdingCertificateConfigFilterInput!){
                      PageResponse: withholdingCertificateConfigPage(filter: $filter){
                        count
                        rows {
                          id
                          name,
                          description,
                          accountingAccounts  {
                                    name
                                    id
                          },
                          costCenter {
                            id
                            name
                            address
                            city { 
                              name
                              department {
                                name
                              }
                            }
                          }
                        }
                      }
                    }
                ";

                dynamic variables = new ExpandoObject();
                variables.filter = new ExpandoObject();
                variables.filter.Pagination = new ExpandoObject();
                variables.filter.Pagination.Page = PageIndex;
                variables.filter.Pagination.PageSize = PageSize;

                var result = await WithholdingCertificateConfigService.GetPage(query, variables);
                stopwatch.Stop();
                this.ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";
                Certificates = Context.AutoMapper.Map<ObservableCollection<WithholdingCertificateConfigGraphQLModel>>(result.PageResponse.Rows);
                TotalCount = result.PageResponse.Count;

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
                //Initialized = true;
                IsBusy = false;
            }
        }
    }
}
