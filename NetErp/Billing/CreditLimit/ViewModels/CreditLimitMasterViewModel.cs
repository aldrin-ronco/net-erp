using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Core;
using Models.Billing;
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

namespace NetErp.Billing.CreditLimit.ViewModels
{
    public class CreditLimitMasterViewModel: Screen,
        IHandle<CreditLimitManagerMessage>
    {
        private readonly Helpers.Services.INotificationService _notificationService = IoC.Get<Helpers.Services.INotificationService>();
        public IGenericDataAccess<CreditLimitGraphQLModel> CreditLimitService = IoC.Get<IGenericDataAccess<CreditLimitGraphQLModel>>();
        public class CreditLimitDTO: PropertyChangedBase
        {
            private int _id;

            public int Id
            {
                get { return _id; }
                set 
                {
                    if (_id != value)
                    {
                        _id = value;
                        NotifyOfPropertyChange(nameof(Id));
                    }
                }
            }

            private CustomerGraphQLModel _customer = new();

            public CustomerGraphQLModel Customer
            {
                get { return _customer; }
                set 
                {
                    if (_customer != value)
                    {
                        _customer = value;
                        NotifyOfPropertyChange(nameof(Customer));
                    }
                }
            }

            private decimal _originalLimit;

            public decimal OriginalLimit
            {
                get { return _originalLimit; }
                set 
                {
                    if (_originalLimit != value)
                    {
                        _originalLimit = value;
                        NotifyOfPropertyChange(nameof(OriginalLimit));
                    }
                }
            }

            private decimal _limit;

            public decimal Limit
            {
                get { return _limit; }
                set 
                {
                    if(value < Used)
                    {
                        ThemedMessageBox.Show("Error", "El valor autorizado no puede ser menor al valor utilizado", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    if (_limit != value)
                    {
                        _limit = value;
                        NotifyOfPropertyChange(nameof(Limit));
                        if(Context != null)
                        {
                            var shadowCreditLimit = Context.ShadowCreditLimits.FirstOrDefault(x => x.Id == this.Id);
                            if (shadowCreditLimit == null)
                            {
                                if (_limit != _originalLimit) 
                                {
                                    Context.ShadowCreditLimits.Add(this);
                                    Context.NotifyOfPropertyChange(nameof(Context.CanSave));
                                }
                            }
                            else
                            {
                                shadowCreditLimit.Limit = _limit;
                            }
                        }
                    }
                }
            }

            private decimal _used;

            public decimal Used
            {
                get { return _used; }
                set 
                {
                    if (_used != value)
                    {
                        _used = value;
                        NotifyOfPropertyChange(nameof(Used));
                    }
                }
            }

            public decimal Available
            {
                get 
                {
                    return Limit - Used; 
                }
            }

            public CreditLimitMasterViewModel Context { get; set; }

            public CreditLimitDTO()
            {

            }
            public CreditLimitDTO(CreditLimitMasterViewModel context)
            {
                Context = context;
            }
        }

        private ObservableCollection<CreditLimitDTO> _creditLimits = [];

        public ObservableCollection<CreditLimitDTO> CreditLimits
        {
            get { return _creditLimits; }
            set 
            {
                if(_creditLimits != value)
                {
                    _creditLimits = value;
                    NotifyOfPropertyChange(nameof(CreditLimits));
                }
            }
        }

        public List<CreditLimitDTO> ShadowCreditLimits { get; set; } = [];


        public string Mask { get; set; } = "n2";

        private string _filterSearch = "";

        public string FilterSearch
        {
            get { return _filterSearch; }
            set 
            {
                if(_filterSearch != value)
                {
                    _filterSearch = value;
                    NotifyOfPropertyChange(nameof(FilterSearch));
                    if (string.IsNullOrEmpty(value) || value.Length >= 3) _ = Task.Run(LoadCreditLimitsAsync);
                }
            }
        }

        private bool _onlyCustomersWithCreditLimit = true;

        public bool OnlyCustomersWithCreditLimit
        {
            get { return _onlyCustomersWithCreditLimit; }
            set 
            {
                if (_onlyCustomersWithCreditLimit != value)
                {
                    _onlyCustomersWithCreditLimit = value;
                    NotifyOfPropertyChange(nameof(OnlyCustomersWithCreditLimit));
                    _ = Task.Run(LoadCreditLimitsAsync);
                }
            }
        }

        public bool CanSave
        {
            get 
            { 
                return ShadowCreditLimits.Count > 0;
            }
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
        public CreditLimitViewModel Context { get; set; }
        public CreditLimitMasterViewModel(CreditLimitViewModel context)
        {
            Context = context;
            Context.EventAggregator.SubscribeOnUIThread(this);
        }

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            _ = Task.Run(LoadCreditLimitsAsync);
        }

        #region Paginacion

        /// <summary>
        /// PageIndex
        /// </summary>
        private int _pageIndex = 1; // DefaultPageIndex = 1
        public int PageIndex
        {
            get { return _pageIndex; }
            set
            {
                if (_pageIndex != value)
                {
                    _pageIndex = value;
                    NotifyOfPropertyChange(() => PageIndex);
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
                    NotifyOfPropertyChange(() => PageSize);
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
                    NotifyOfPropertyChange(() => TotalCount);
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
                if (_paginationCommand == null) this._paginationCommand = new AsyncCommand(ExecuteChangeIndexAsync, CanExecuteChangeIndex);
                return _paginationCommand;
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
                    NotifyOfPropertyChange(() => ResponseTime);
                }
            }
        }

        private async Task ExecuteChangeIndexAsync()
        {
            await LoadCreditLimitsAsync();
        }

        private bool CanExecuteChangeIndex()
        {
            return true;
        }

        #endregion

        public async Task LoadCreditLimitsAsync()
        {
            try
            {
                IsBusy = true;
                string query = @"
                query($filter: CreditLimitFilterInput!){
                  PageResponse: creditLimitPage(filter: $filter){
                    count
                    rows{
                      id
                      limit
                      originalLimit
                      used
                      available
                      customer{
                        id
                        entity{
                          id
                          searchName
                          telephonicInformation
                          identificationNumber
                          verificationDigit
                        }
                      }
                    }
                  }
                }";

                dynamic variables = new ExpandoObject();
                variables.filter = new ExpandoObject();
                variables.filter.and = new ExpandoObject[]
                {
                    new(),
                    new()
                };
                variables.filter.and[0].or = new ExpandoObject[]
                {
                    new(),
                    new()
                };

                //filtro searchName 
                variables.filter.and[0].or[0].searchName = new ExpandoObject();
                variables.filter.and[0].or[0].searchName.@operator = "like";
                variables.filter.and[0].or[0].searchName.value = FilterSearch.Trim().RemoveExtraSpaces();

                //filtro identificatioNumber
                variables.filter.and[0].or[1].identificationNumber = new ExpandoObject();
                variables.filter.and[0].or[1].identificationNumber.@operator = "like";
                variables.filter.and[0].or[1].identificationNumber.value = FilterSearch.Trim().RemoveExtraSpaces();

                //filtro limite
                if(OnlyCustomersWithCreditLimit)
                {
                    variables.filter.and[1].limit = new ExpandoObject();
                    variables.filter.and[1].limit.@operator = ">";
                    variables.filter.and[1].limit.value = 0;
                }

                variables.filter.pagination = new ExpandoObject();
                variables.filter.pagination.page = PageIndex;
                variables.filter.pagination.pageSize = PageSize;
                // Iniciar cronometro
                Stopwatch stopwatch = new();
                stopwatch.Start();
                var result = await CreditLimitService.GetPage(query, variables);
                TotalCount = result.PageResponse.Count;
                var loadedCreditLimits = new ObservableCollection<CreditLimitGraphQLModel>(result.PageResponse.Rows);
                //TODO evaluar comportamiento
                if (ShadowCreditLimits.Count > 0)
                {
                    foreach (var shadowCreditLimit in ShadowCreditLimits)
                    {
                        var creditLimit = loadedCreditLimits.FirstOrDefault(x => x.Id == shadowCreditLimit.Id);
                        if (creditLimit != null)
                        {
                            creditLimit.Limit = shadowCreditLimit.Limit;
                        }
                    }
                }
                CreditLimits = new ObservableCollection<CreditLimitDTO>(Context.AutoMapper.Map <ObservableCollection<CreditLimitDTO>>(loadedCreditLimits));
                foreach (var creditLimit in CreditLimits)
                {
                    creditLimit.Context = this;
                }
                stopwatch.Stop();
                ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";
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

        private ICommand _saveCommand;

        public ICommand SaveCommand
        {
            get 
            {
                if (_saveCommand is null) _saveCommand = new AsyncCommand(SaveAsync);
                return _saveCommand;
            }
        }

        public async Task SaveAsync()
        {
            try
            {
                IsBusy = true;
                var managedCreditLimits = await ExecuteSaveAsync();
                if (!managedCreditLimits.Any()) return;
                await Context.EventAggregator.PublishOnUIThreadAsync(new CreditLimitManagerMessage { ManagedCreditLimits = managedCreditLimits });
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

        public async Task<IEnumerable<CreditLimitGraphQLModel>> ExecuteSaveAsync()
        {
            try
            {
                string query;

                List<object> creditLimits = ShadowCreditLimits.Where(creditLimit => creditLimit.Limit != creditLimit.OriginalLimit).Select(credit => new
                {
                    customerId = credit.Customer.Id,
                    limit = credit.Limit
                }).ToList<object>();

                if (creditLimits.Count == 0) return [];

                query = @"mutation($data: CreditLimitMaganerInput!){
                          ListResponse: managerCreditLimit(data: $data){
                            id
                            limit
                            used
                            available
                            originalLimit
                          }
                        }";

                dynamic variables = new ExpandoObject();
                variables.data = new ExpandoObject();
                variables.data.creditLimits = creditLimits;

                var result = await CreditLimitService.SendMutationList(query, variables);
                return result;
            }
            catch (Exception ex)
            {

                throw new AsyncException(innerException: ex);
            }
        }

        public Task HandleAsync(CreditLimitManagerMessage message, CancellationToken cancellationToken)
        {
            foreach(var creditLimit in CreditLimits)
            {
                creditLimit.OriginalLimit = creditLimit.Limit;
            }
            ShadowCreditLimits.Clear();
            NotifyOfPropertyChange(nameof(CanSave));
            _notificationService.ShowSuccess("Guardado exitoso");
            return Task.CompletedTask;
        }
    }
}
