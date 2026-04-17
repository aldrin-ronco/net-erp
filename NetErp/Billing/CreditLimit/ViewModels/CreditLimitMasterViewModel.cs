using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using Common.Validators;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Microsoft.VisualStudio.Threading;
using Models.Billing;
using Models.Global;
using NetErp.Billing.CreditLimit.DTO;
using NetErp.Helpers;
using NetErp.Helpers.GraphQLQueryBuilder;
using NetErp.Helpers.Messages;
using NetErp.Helpers.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;
using INotificationService = NetErp.Helpers.Services.INotificationService;

namespace NetErp.Billing.CreditLimit.ViewModels
{
    public class CreditLimitMasterViewModel : Screen,
        IHandle<OperationCompletedMessage>
    {
        private const int SavedStatusResetDelayMs = 5000;

        private readonly INotificationService _notificationService;
        private readonly ICreditLimitValidator _validator;
        private readonly IRepository<CreditLimitGraphQLModel> _creditLimitService;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly IBackgroundQueueService _backgroundQueueService;
        private readonly DebouncedAction _searchDebounce;
        private readonly Dictionary<Guid, int> _operationItemMapping = [];
        private readonly Dictionary<int, CancellationTokenSource> _statusResetTokens = [];

        public ObservableCollection<CreditLimitDTO> CreditLimits
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(CreditLimits));
                }
            }
        } = [];

        public string Mask { get; } = "n2";

        public string FilterSearch
        {
            get;
            set
            {
                if (field == value) return;
                if (HasPendingOperations)
                {
                    _notificationService.ShowWarning(
                        "Espere a que se confirmen los cambios pendientes antes de aplicar filtros.",
                        "Operaciones en curso");
                    NotifyOfPropertyChange(nameof(FilterSearch));
                    return;
                }
                field = value;
                NotifyOfPropertyChange(nameof(FilterSearch));
                if (string.IsNullOrEmpty(value) || value.Length >= 3)
                {
                    PageIndex = 1;
                    _ = _searchDebounce.RunAsync(LoadCreditLimitsAsync);
                }
            }
        } = string.Empty;

        public bool OnlyCustomersWithCreditLimit
        {
            get;
            set
            {
                if (field == value) return;
                if (HasPendingOperations)
                {
                    _notificationService.ShowWarning(
                        "Espere a que se confirmen los cambios pendientes antes de aplicar filtros.",
                        "Operaciones en curso");
                    NotifyOfPropertyChange(nameof(OnlyCustomersWithCreditLimit));
                    return;
                }
                field = value;
                NotifyOfPropertyChange(nameof(OnlyCustomersWithCreditLimit));
                _ = LoadCreditLimitsAsync();
            }
        } = true;

        public bool IsBusy
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IsBusy));
                    NotifyOfPropertyChange(nameof(IsFilteringEnabled));
                }
            }
        }

        public bool HasPendingOperations => _operationItemMapping.Count > 0;

        public bool IsFilteringEnabled => !IsBusy && !HasPendingOperations;

        public CreditLimitViewModel Context { get; }

        public CreditLimitMasterViewModel(
            CreditLimitViewModel context,
            INotificationService notificationService,
            ICreditLimitValidator validator,
            IBackgroundQueueService backgroundQueueService,
            IRepository<CreditLimitGraphQLModel> creditLimitService,
            JoinableTaskFactory joinableTaskFactory,
            DebouncedAction searchDebounce)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _creditLimitService = creditLimitService ?? throw new ArgumentNullException(nameof(creditLimitService));
            _backgroundQueueService = backgroundQueueService ?? throw new ArgumentNullException(nameof(backgroundQueueService));
            _searchDebounce = searchDebounce ?? throw new ArgumentNullException(nameof(searchDebounce));
            _joinableTaskFactory = joinableTaskFactory;
            Context.EventAggregator.SubscribeOnUIThread(this);
        }

        protected override async Task OnInitializedAsync(CancellationToken cancellationToken)
        {
            await LoadCreditLimitsAsync();
            await base.OnInitializedAsync(cancellationToken);
        }

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            this.SetFocus(() => FilterSearch);
        }

        #region Paginacion

        public int PageIndex
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(PageIndex));
                }
            }
        } = 1;

        public int PageSize
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(PageSize));
                }
            }
        } = 50;

        public int TotalCount
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(TotalCount));
                }
            }
        }

        public ICommand PaginationCommand
        {
            get => field ??= new AsyncCommand(ExecuteChangeIndexAsync, CanExecuteChangeIndex);
        }

        public string? ResponseTime
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(ResponseTime));
                }
            }
        }

        private async Task ExecuteChangeIndexAsync() => await LoadCreditLimitsAsync();

        private bool CanExecuteChangeIndex() => true;

        #endregion

        #region Load

        public async Task LoadCreditLimitsAsync()
        {
            try
            {
                IsBusy = true;
                Stopwatch stopwatch = Stopwatch.StartNew();

                (GraphQLQueryFragment fragment, string query) = _loadCreditLimitQuery.Value;

                dynamic filters = new ExpandoObject();
                if (OnlyCustomersWithCreditLimit) filters.hasCreditLimit = true;
                if (!string.IsNullOrEmpty(FilterSearch)) filters.matching = FilterSearch.Trim().RemoveExtraSpaces();

                dynamic variables = new GraphQLVariables()
                    .For(fragment, "pagination", new { Page = PageIndex, PageSize })
                    .For(fragment, "filters", filters)
                    .Build();

                PageType<CreditLimitGraphQLModel> result = await _creditLimitService.GetPageAsync(query, variables);

                TotalCount = result.TotalEntries;
                UpdateCreditLimitsCollection(new ObservableCollection<CreditLimitGraphQLModel>(result.Entries));

                stopwatch.Stop();
                ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(LoadCreditLimitsAsync)}: {ex.GetErrorMessage()}",
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
            if (CreditLimits.Count > 0)
            {
                foreach (CreditLimitDTO old in CreditLimits)
                    old.LimitChanged -= OnCreditLimitChanged!;
            }
            CancelAllStatusResets();

            List<CreditLimitDTO> newItems = new(loadedCreditLimits.Count);
            foreach (CreditLimitGraphQLModel item in loadedCreditLimits)
            {
                CreditLimitDTO dto = Context.AutoMapper.Map<CreditLimitDTO>(item);
                dto.LimitChanged += OnCreditLimitChanged!;
                dto.Context = this;
                newItems.Add(dto);
            }

            CreditLimits = new ObservableCollection<CreditLimitDTO>(newItems);
        }

        public CreditLimitDTO? SelectedCreditLimitItem
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedCreditLimitItem));
                }
            }
        }

        private void SetStatus(CreditLimitDTO item, OperationStatus status)
        {
            item.Status = status;
            if (status == OperationStatus.Saved)
                ScheduleStatusReset(item);
            else
                CancelStatusReset(item.Customer.Id);
        }

        private void ScheduleStatusReset(CreditLimitDTO item)
        {
            int key = item.Customer.Id;
            CancelStatusReset(key);

            CancellationTokenSource cts = new();
            _statusResetTokens[key] = cts;
            _ = ResetStatusAfterDelayAsync(item, cts.Token);
        }

        private async Task ResetStatusAfterDelayAsync(CreditLimitDTO item, CancellationToken token)
        {
            try
            {
                await Task.Delay(SavedStatusResetDelayMs, token);
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                if (token.IsCancellationRequested) return;
                if (item.Status == OperationStatus.Saved)
                    item.Status = OperationStatus.Unchanged;
            }
            catch (TaskCanceledException) { }
        }

        private void CancelStatusReset(int customerId)
        {
            if (_statusResetTokens.Remove(customerId, out CancellationTokenSource? cts))
            {
                cts.Cancel();
                cts.Dispose();
            }
        }

        private void CancelAllStatusResets()
        {
            foreach (CancellationTokenSource cts in _statusResetTokens.Values)
            {
                cts.Cancel();
                cts.Dispose();
            }
            _statusResetTokens.Clear();
        }

        private async Task EnqueueUpdateAsync(CreditLimitDTO limit)
        {
            try
            {
                SetStatus(limit, OperationStatus.Pending);
                CreditLimitUpdateOperation operation = new(_creditLimitService)
                {
                    NewLimit = limit.CreditLimit,
                    CustomerId = limit.Customer.Id
                };
                TrackOperation(operation.OperationId, limit.Customer.Id);
                await _backgroundQueueService.EnqueueOperationAsync(operation);
            }
            catch (InvalidOperationException)
            {
                SetStatus(limit, OperationStatus.Failed);
                _notificationService.ShowError(_backgroundQueueService.GetCriticalErrorMessage());
            }
            catch (Exception ex)
            {
                SetStatus(limit, OperationStatus.Failed);
                string name = limit.Customer?.AccountingEntity?.FullName ?? $"#{limit.Customer?.Id}";
                _notificationService.ShowError($"Error inesperado al procesar \"{name}\": {ex.GetErrorMessage()}", durationMs: 8000);
            }
        }

        private void TrackOperation(Guid operationId, int customerId)
        {
            _operationItemMapping[operationId] = customerId;
            NotifyOfPropertyChange(nameof(HasPendingOperations));
            NotifyOfPropertyChange(nameof(IsFilteringEnabled));
        }

        private void UntrackOperation(Guid operationId)
        {
            if (_operationItemMapping.Remove(operationId))
            {
                NotifyOfPropertyChange(nameof(HasPendingOperations));
                NotifyOfPropertyChange(nameof(IsFilteringEnabled));
            }
        }

        private void ClearOperationMapping()
        {
            if (_operationItemMapping.Count == 0) return;
            _operationItemMapping.Clear();
            NotifyOfPropertyChange(nameof(HasPendingOperations));
            NotifyOfPropertyChange(nameof(IsFilteringEnabled));
        }

        private void OnCreditLimitChanged(object sender, LimitChangedEventArgs e)
        {
            if (sender is not CreditLimitDTO creditLimit) return;

            if (creditLimit.Customer == null)
            {
                _notificationService.ShowError("Debe especificar un cliente válido", "Error de Validación");
                creditLimit.SetCreditLimitSilently(e.OldValue);
                return;
            }

            ValidationResult validationResult = _validator.ValidateLimit(e.NewValue, creditLimit.Used, creditLimit.OriginalLimit);

            if (!validationResult.IsValid)
            {
                _notificationService.ShowError(validationResult.ErrorMessage, "Error de Validación");
                creditLimit.SetCreditLimitSilently(e.OldValue);
                return;
            }

            if (validationResult.Severity == ValidationSeverity.Warning)
            {
                _notificationService.ShowWarning(validationResult.ErrorMessage, "Advertencia");
            }

            _ = EnqueueUpdateAsync(creditLimit);
        }

        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            if (close)
            {
                foreach (CreditLimitDTO creditLimit in CreditLimits)
                    creditLimit.LimitChanged -= OnCreditLimitChanged!;
                CancelAllStatusResets();
                Context.EventAggregator.Unsubscribe(this);
                CreditLimits.Clear();
                ClearOperationMapping();
            }
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

        public Task HandleAsync(OperationCompletedMessage message, CancellationToken cancellationToken)
        {
            if (!_operationItemMapping.TryGetValue(message.OperationId, out int itemId))
                return Task.CompletedTask;

            CreditLimitDTO? item = CreditLimits.FirstOrDefault(i => i.Customer.Id == itemId);
            if (item == null) return Task.CompletedTask;

            if (message.Success)
            {
                item.OriginalLimit = item.CreditLimit;
                SetStatus(item, OperationStatus.Saved);
                UntrackOperation(message.OperationId);
            }
            else if (message.IsRetrying)
            {
                SetStatus(item, OperationStatus.Retrying);
                item.StatusTooltip = message.ErrorDetail;
            }
            else
            {
                SetStatus(item, OperationStatus.Failed);
                item.StatusTooltip = message.ErrorDetail ?? message.Exception?.Message;
                UntrackOperation(message.OperationId);
                _notificationService.ShowError(
                    $"Error al guardar \"{item.Customer.AccountingEntity.FullName}\": {message.ErrorDetail ?? message.Exception?.Message}\n\nSi el problema persiste, comuníquese con soporte técnico.",
                    durationMs: 6000);
            }

            return Task.CompletedTask;
        }

        #endregion
    }
}
