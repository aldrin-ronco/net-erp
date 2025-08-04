using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using Common.Validators;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Core;
using Models.Billing;
using Models.DTO.Billing;
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
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly ICreditLimitValidator _validator;
        private readonly IRepository<CreditLimitGraphQLModel> _creditLimitService;

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
                    if (string.IsNullOrEmpty(value) || value.Length >= 3) 
                    {
                        _ = LoadCreditLimitsAsync();
                    }
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
                    _ = LoadCreditLimitsAsync();
                }
            }
        }

        public bool CanSave => ShadowCreditLimits.Count > 0 && !IsBusy;

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
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }
        public CreditLimitViewModel Context { get; set; }
        public CreditLimitMasterViewModel(
            CreditLimitViewModel context,
            Helpers.Services.INotificationService notificationService,
            ICreditLimitValidator validator,
            IRepository<CreditLimitGraphQLModel> creditLimitService)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _creditLimitService = creditLimitService ?? throw new ArgumentNullException(nameof(creditLimitService));
            Context.EventAggregator.SubscribeOnUIThread(this);
        }

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            _ = LoadCreditLimitsAsync();
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
                var result = await _creditLimitService.GetPageAsync(query, variables);
                TotalCount = result.Count;
                var loadedCreditLimits = new ObservableCollection<CreditLimitGraphQLModel>(result.Rows);
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
                UpdateCreditLimitsCollection(loadedCreditLimits);
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

        private void UpdateCreditLimitsCollection(ObservableCollection<CreditLimitGraphQLModel> loadedCreditLimits)
        {
            // 1. Limpiar eventos de colección anterior (si existe)
            if (CreditLimits?.Count > 0)
            {
                foreach (var old in CreditLimits)
                    old.LimitChanged -= OnCreditLimitChanged;
                CreditLimits.Clear(); // Liberar referencias inmediatamente
            }

            // 2. Pre-allocar con capacidad conocida para evitar reallocations
            var newItems = new List<CreditLimitDTO>(loadedCreditLimits.Count);

            // 3. Mapear y conectar en una sola pasada
            foreach (var item in loadedCreditLimits)
            {
                var dto = Context.AutoMapper.Map<CreditLimitDTO>(item);
                dto.LimitChanged += OnCreditLimitChanged;
                newItems.Add(dto);
            }

            // 4. Asignar nueva colección
            CreditLimits = new ObservableCollection<CreditLimitDTO>(newItems);
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

                var result = await _creditLimitService.SendMutationListAsync(query, variables);
                return result;
            }
            catch (Exception ex)
            {

                throw new AsyncException(innerException: ex);
            }
        }

        private void OnCreditLimitChanged(object sender, LimitChangedEventArgs e)
        {
            var creditLimit = sender as CreditLimitDTO;
            if (creditLimit == null) return;

            // Validaciones básicas del DTO antes de llamar al validator
            if (creditLimit.Customer == null)
            {
                _notificationService.ShowError("Debe especificar un cliente válido", "Error de Validación");
                
                // Revertir el cambio
                creditLimit.LimitChanged -= OnCreditLimitChanged;
                creditLimit.Limit = e.OldValue;
                creditLimit.LimitChanged += OnCreditLimitChanged;
                return;
            }

            // 1. VALIDAR usando el validator híbrido (solo tipos primitivos)
            var validationResult = _validator.ValidateLimit(e.NewValue, creditLimit.Used, creditLimit.OriginalLimit);
            
            // 2. NOTIFICAR basado en el resultado
            if (!validationResult.IsValid)
            {
                // Error: Mostrar notificación y revertir
                Execute.OnUIThread(() =>
                {
                    _notificationService.ShowError(validationResult.ErrorMessage, "Error de Validación");
                });
                
                // Temporalmente desconectar el evento para evitar recursión
                creditLimit.LimitChanged -= OnCreditLimitChanged;
                creditLimit.Limit = e.OldValue;
                creditLimit.LimitChanged += OnCreditLimitChanged;
                return;
            }
            
            if (validationResult.Severity == ValidationSeverity.Warning)
            {
                // Warning: Mostrar pero permitir continuar
                Execute.OnUIThread(() =>
                {
                    _notificationService.ShowWarning(validationResult.ErrorMessage, "Advertencia");
                });
            }

            // 3. PROCESAR si es válido - Actualizar shadow limits
            UpdateShadowCreditLimits(creditLimit);
        }

        private void UpdateShadowCreditLimits(CreditLimitDTO creditLimit)
        {
            var existing = ShadowCreditLimits.FirstOrDefault(x => x.Id == creditLimit.Id);
            if (existing == null)
            {
                if (creditLimit.Limit != creditLimit.OriginalLimit)
                {
                    ShadowCreditLimits.Add(creditLimit);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
            else
            {
                existing.Limit = creditLimit.Limit;
                
                // Si volvió al valor original, quitarlo de shadow limits
                if (creditLimit.Limit == creditLimit.OriginalLimit)
                {
                    ShadowCreditLimits.Remove(existing);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            
            // Desconectar eventos para evitar memory leaks
            foreach (var creditLimit in CreditLimits)
            {
                creditLimit.LimitChanged -= OnCreditLimitChanged;
            }
            Context.EventAggregator.Unsubscribe(this);
            CreditLimits.Clear();
            ShadowCreditLimits.Clear();
            return base.OnDeactivateAsync(close, cancellationToken);
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
