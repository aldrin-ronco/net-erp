﻿using Caliburn.Micro;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Models.Billing;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NetErp.Billing.CreditLimit.ViewModels
{
    public class CreditLimitMasterViewModel: Screen
    {
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

            private decimal _available;

            public decimal Available
            {
                get { return _available; }
                set 
                {
                    if (_available != value)
                    {
                        _available = value;
                        NotifyOfPropertyChange(nameof(Available));
                    }
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


        public CreditLimitViewModel Context { get; set; }
        public CreditLimitMasterViewModel(CreditLimitViewModel context)
        {
            Context = context;
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
                if (_paginationCommand == null) this._paginationCommand = new AsyncCommand(ExecuteChangeIndex, CanExecuteChangeIndex);
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

        private async Task ExecuteChangeIndex()
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
                variables.filter.searchName = FilterSearch;
                variables.filter.onlyCustomersWithCreditLimit = OnlyCustomersWithCreditLimit;
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
            catch (Exception)
            {
                throw;
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
                //IsBusy = true;
                var managedCreditLimits = await ExecuteSaveAsync();
                if (!managedCreditLimits.Any()) return;
                await Context.EventAggregator.PublishOnUIThreadAsync(new CreditLimitManagerMessage { ManagedCreditLimits = managedCreditLimits });
            }
            catch (Exception)
            {

                throw;
            }
            finally
            {
                //IsBusy = false;
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
                return [];
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
