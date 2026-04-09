using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using Common.Validators;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Utils.Html.Internal;
using DevExpress.Xpf.Core;
using Microsoft.Identity.Client;
using Microsoft.VisualStudio.Threading;
using Models.Billing;
using Models.Global;

//using Models.DTO.Billing;
using NetErp.Billing.CreditLimit.DTO;

using NetErp.Helpers.GraphQLQueryBuilder;
using NetErp.Helpers.Messages;
using NetErp.Helpers.Services;
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
using static Models.Global.GraphQLResponseTypes;
using static NetErp.Helpers.PermissionCodes;

namespace NetErp.Billing.CreditLimit.ViewModels
{
    public class CreditLimitMasterViewModel: Screen,
        IHandle<CreditLimitManagerMessage>,
        IHandle<OperationCompletedMessage>
    {
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly ICreditLimitValidator _validator;
        private readonly IRepository<CreditLimitGraphQLModel> _creditLimitService;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly IBackgroundQueueService _backgroundQueueService;
        private readonly Dictionary<Guid, int> _operationItemMapping = new Dictionary<Guid, int>();

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
            IBackgroundQueueService backgroundQueueService,
            IRepository<CreditLimitGraphQLModel> creditLimitService,
            JoinableTaskFactory joinableTaskFactory)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _creditLimitService = creditLimitService ?? throw new ArgumentNullException(nameof(creditLimitService));
            _joinableTaskFactory = joinableTaskFactory;
            _backgroundQueueService = backgroundQueueService;
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

        #region Load

        public async Task LoadCreditLimitsAsync()
        {
            try
            {
                IsBusy = true;

                Stopwatch stopwatch = Stopwatch.StartNew();

                var (fragment, query) = _loadCreditLimitQuery.Value;

                dynamic filters = new ExpandoObject();
                if (OnlyCustomersWithCreditLimit) filters.hasCreditLimit = true;
                if (!string.IsNullOrEmpty(FilterSearch)) filters.matching = FilterSearch.Trim().RemoveExtraSpaces();

                var variables = new GraphQLVariables()
                    .For(fragment, "pagination", new { Page = PageIndex, PageSize })
                    .For(fragment, "filters", filters)
                    .Build();

                PageType<CreditLimitGraphQLModel> result = await _creditLimitService.GetPageAsync(query, variables);

                TotalCount = result.TotalEntries;
                var loadedCreditLimits = new ObservableCollection<CreditLimitGraphQLModel>(result.Entries);
                //TODO evaluar comportamiento
                if (ShadowCreditLimits.Count > 0)
                {
                    foreach (var shadowCreditLimit in ShadowCreditLimits)
                    {
                        var creditLimit = loadedCreditLimits.FirstOrDefault(x => x.Id == shadowCreditLimit.Id);
                        if (creditLimit != null)
                        {
                            creditLimit.CreditLimit = shadowCreditLimit.CreditLimit;
                        }
                    }
                }
                UpdateCreditLimitsCollection(loadedCreditLimits);
                stopwatch.Stop();

                ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(LoadCreditLimitsAsync)}: {ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }
       
        #endregion
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
            //Context._autoMapper.Map<List<CreditLimitDTO>>(loadedCreditLimits)

            List<CreditLimitDTO> newItems = new List<CreditLimitDTO>(loadedCreditLimits.Count);

            // 3. Mapear y conectar en una sola pasada
            foreach (var item in loadedCreditLimits)
            {
                var dto = Context.AutoMapper.Map<CreditLimitDTO>(item);
                dto.LimitChanged += OnCreditLimitChanged;
                dto.Context = this;
                newItems.Add(dto);
            }

            // 4. Asignar nueva colección
            CreditLimits = new ObservableCollection<CreditLimitDTO>(newItems);
        }

       
        private CreditLimitDTO? _selectedCreditLimitItem;

        public CreditLimitDTO? SelectedCreditLimitItem
        {
            get { return _selectedCreditLimitItem; }
            set
            {
                if (_selectedCreditLimitItem != value)
                {
                    _selectedCreditLimitItem = value;
                    NotifyOfPropertyChange(nameof(SelectedCreditLimitItem));
                }
            }
        }

        public async void AddModifiedLimit(CreditLimitDTO limit, string modifiedProperty)
        {
            try
            {
                if (SelectedCreditLimitItem is null) return;
                
                /*IPriceListCalculator calculator = _calculatorFactory.GetCalculator(SelectedPriceList.UseAlternativeFormula);
                calculator.RecalculateProductValues(priceListDetail, modifiedProperty, SelectedPriceList);*/
                limit.Status = OperationStatus.Pending;

                var operation = new CreditLimitUpdateOperation(_creditLimitService)
                {
                   NewLimit = limit.CreditLimit,
                     CustomerId = limit.Customer.Id
                };

                _operationItemMapping[operation.OperationId] = limit.Customer.Id;
                await _backgroundQueueService.EnqueueOperationAsync(operation);
            }
            catch (InvalidOperationException)
            {
                limit.Status = OperationStatus.Failed;
                _notificationService.ShowError(_backgroundQueueService.GetCriticalErrorMessage());
            }
            catch (Exception ex)
            {
                limit.Status = OperationStatus.Failed;
                _notificationService.ShowError($"Error inesperado al procesar \"{limit.Customer.AccountingEntity.FullName}\": {ex.Message}", durationMs: 8000);
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
                creditLimit.CreditLimit = e.OldValue;
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
                creditLimit.CreditLimit = e.OldValue;
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
                if (creditLimit.CreditLimit != creditLimit.OriginalLimit)
                {
                    ShadowCreditLimits.Add(creditLimit);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
            else
            {
                existing.CreditLimit = creditLimit.CreditLimit;
                
                // Si volvió al valor original, quitarlo de shadow limits
                if (creditLimit.CreditLimit == creditLimit.OriginalLimit)
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
        #region GraphQL Queries

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadCreditLimitQuery = new(() =>
        {
            var fields = FieldSpec<PageType<CreditLimitGraphQLModel>>
                .Create()
                .Field(f => f.TotalEntries)
                .SelectList(f => f.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.CreditLimit)
                    .Select(selector: e => e.Customer, nested: entity => entity
                        .Field(en => en.Id)
                            .Select(selector: en => en.AccountingEntity, nested: accountingEntity => accountingEntity
                                .Field(en => en.IdentificationNumber)
                                .Field(en => en.VerificationDigit)
                                .Field(en => en.SearchName)
                                .Field(en => en.Regime)
                                .Field(en => en.TelephonicInformation)
                                .Field(en => en.Address)
                            )
                    )
                    )
                .Build();

            var fragment = new GraphQLQueryFragment("creditStatusPage",
                [new("filters", "CreditStatusFilters"), new("pagination", "Pagination")],
                fields, "PageResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        #endregion
        #region HandleAsync
        public Task HandleAsync(CreditLimitManagerMessage message, CancellationToken cancellationToken)
        {
            foreach(var creditLimit in CreditLimits)
            {
                creditLimit.OriginalLimit = creditLimit.CreditLimit;
            }
            ShadowCreditLimits.Clear();
            NotifyOfPropertyChange(nameof(CanSave));
            _notificationService.ShowSuccess("Guardado exitoso");
            return Task.CompletedTask;
        }

   
        public Task HandleAsync(OperationCompletedMessage message, CancellationToken cancellationToken)
        {
            if (_operationItemMapping.TryGetValue(message.OperationId, out int itemId))
            {
                var item = CreditLimits.FirstOrDefault(i => i.Customer.Id == itemId);
                if (item != null)
                {
                    if (message.Success)
                    {
                        item.Status = OperationStatus.Saved;
                        _operationItemMapping.Remove(message.OperationId);
                    }
                    else if (message.IsRetrying)
                    {
                        item.Status = OperationStatus.Retrying;
                        item.StatusTooltip = message.ErrorDetail;
                    }
                    else
                    {
                        item.Status = OperationStatus.Failed;
                        item.StatusTooltip = message.ErrorDetail ?? message.Exception?.Message;
                        _operationItemMapping.Remove(message.OperationId);
                        _notificationService.ShowError(
                            $"Error al guardar \"{item.Customer.AccountingEntity.FullName}\": {message.ErrorDetail ?? message.Exception?.Message}\n\nSi el problema persiste, comuníquese con soporte técnico.",
                            durationMs: 6000);
                    }
                }
            }

            return Task.CompletedTask;
        }
    }
        #endregion
    public class CreditLimitUpdateOperation : IDataOperation
    {
        private readonly IRepository<CreditLimitGraphQLModel> _repository;

        public decimal NewLimit { get; set; }
     
        public int CustomerId { get; set; }

        public CreditLimitUpdateOperation(IRepository<CreditLimitGraphQLModel> repository)
        {
            _repository = repository;
        }
        public object Variables => new
        {
            item = new
            {
                creditLimit = NewLimit,
                customerId = CustomerId

            },
            
        };
       

        public static Type OperationResponseType => typeof(CreditLimitGraphQLModel);
        public Type ResponseType => OperationResponseType;
        public Guid OperationId { get; set; } = Guid.NewGuid();

        public string DisplayName =>  $"Customer #{CustomerId}";

        public int Id => CustomerId;

        public BatchOperationInfo GetBatchInfo()
        {


            return new BatchOperationInfo
            {
                
              
                BatchQuery = _batchUpsertCreditLimitMutation.Value,

                ExtractBatchItem = (variables) =>
                {
                    return variables.GetType().GetProperty("item")!.GetValue(variables)!;
                },

                BuildBatchVariables = (batchItems) =>
                {
                    return new
                    {
                        singleItemResponseInput = new
                        {
                            items = batchItems
                        }
                    };
                },

                ExecuteBatchAsync = async (query, variables, cancellationToken) =>
                {
                    return await _repository.BatchAsync<BatchResultGraphQLModel>(query, variables, cancellationToken);
                }
            };
        }
        private static readonly Lazy<string> _batchUpsertCreditLimitMutation = new(() =>
        {
            var fields = FieldSpec<BatchResultGraphQLModel>
                .Create()
                .Field(f => f.Success)
               .Field(f => f.Message)
               .Field(f => f.TotalAffected)
               .Field(f => f.AffectedIds)
               .SelectList(f => f.Errors, sq => sq
                   .Field(e => e.Message))
               .Build();


            var parameters = new List<GraphQLQueryParameter>
            {
                new("input", "BatchUpsertCreditLimitsInput!")
            };
            var fragment = new GraphQLQueryFragment("batchUpsertCreditLimits", parameters, fields, "SingleItemResponse");
            var builder = new GraphQLQueryBuilder([fragment]);
            return builder.GetQuery(GraphQLOperations.MUTATION);
           
          

            
        });

        
    }
}

