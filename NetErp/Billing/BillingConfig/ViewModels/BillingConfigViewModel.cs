using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Extensions.Global;
using Microsoft.VisualStudio.Threading;
using Models.Billing;
using NetErp.Global.Modals.ViewModels;
using NetErp.Helpers;
using NetErp.Helpers.GraphQLQueryBuilder;
using INotificationService = NetErp.Helpers.Services.INotificationService;
using System;
using System.Dynamic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;
using QueryBuilder = NetErp.Helpers.GraphQLQueryBuilder.GraphQLQueryBuilder;

namespace NetErp.Billing.BillingConfig.ViewModels
{
    public class BillingConfigViewModel : Screen
    {
        private readonly IRepository<BillingConfigGraphQLModel> _billingConfigService;
        private readonly IEventAggregator _eventAggregator;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly INotificationService _notificationService;
        private readonly NetErp.Helpers.IDialogService _dialogService;
        private BillingConfigGraphQLModel? _loadedConfig;

        public BillingConfigViewModel(
            IRepository<BillingConfigGraphQLModel> billingConfigService,
            IEventAggregator eventAggregator,
            JoinableTaskFactory joinableTaskFactory,
            INotificationService notificationService,
            NetErp.Helpers.IDialogService dialogService)
        {
            _billingConfigService = billingConfigService;
            _eventAggregator = eventAggregator;
            _joinableTaskFactory = joinableTaskFactory;
            _notificationService = notificationService;
            _dialogService = dialogService;

            Messenger.Default.Register<ReturnedDataFromModalWithTwoColumnsGridViewMessage<CustomerGraphQLModel>>(
                this,
                SearchWithTwoColumnsGridMessageToken.BillingConfigDefaultCustomer,
                false,
                OnFindDefaultCustomerMessage);
        }

        #region Properties

        private int _id;
        public int Id
        {
            get => _id;
            set
            {
                if (_id == value) return;
                _id = value;
                NotifyOfPropertyChange(nameof(Id));
                NotifyOfPropertyChange(nameof(IsNewRecord));
            }
        }

        public bool IsNewRecord => Id == 0;

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (_isBusy == value) return;
                _isBusy = value;
                NotifyOfPropertyChange(nameof(IsBusy));
                NotifyOfPropertyChange(nameof(CanEdit));
                NotifyOfPropertyChange(nameof(CanSave));
            }
        }

        private bool _isEditing;
        public bool IsEditing
        {
            get => _isEditing;
            set
            {
                if (_isEditing == value) return;
                _isEditing = value;
                NotifyOfPropertyChange(nameof(IsEditing));
                NotifyOfPropertyChange(nameof(CanEdit));
                NotifyOfPropertyChange(nameof(CanSave));
            }
        }

        public bool CanEdit => !IsEditing && !IsBusy;

        private CustomerGraphQLModel? _selectedDefaultCustomer;

        [ExpandoPath("defaultCustomerId", SerializeAsId = true)]
        public CustomerGraphQLModel? SelectedDefaultCustomer
        {
            get => _selectedDefaultCustomer;
            set
            {
                if (_selectedDefaultCustomer == value) return;
                _selectedDefaultCustomer = value;
                NotifyOfPropertyChange(nameof(SelectedDefaultCustomer));
                NotifyOfPropertyChange(nameof(SelectedDefaultCustomerDisplay));
                this.TrackChange(nameof(SelectedDefaultCustomer), value);
                NotifyOfPropertyChange(nameof(CanSave));
            }
        }

        public string SelectedDefaultCustomerDisplay =>
            SelectedDefaultCustomer?.AccountingEntity is null
                ? string.Empty
                : $"{SelectedDefaultCustomer.AccountingEntity.IdentificationNumber} — {SelectedDefaultCustomer.AccountingEntity.SearchName}";

        public bool CanSave => IsEditing && this.HasChanges() && !IsBusy;

        #endregion

        #region Commands

        private ICommand? _editCommand;
        public ICommand EditCommand
        {
            get
            {
                _editCommand ??= new DelegateCommand(() => IsEditing = true);
                return _editCommand;
            }
        }

        private ICommand? _saveCommand;
        public ICommand SaveCommand
        {
            get
            {
                _saveCommand ??= new AsyncCommand(SaveAsync);
                return _saveCommand;
            }
        }

        private ICommand? _undoCommand;
        public ICommand UndoCommand
        {
            get
            {
                _undoCommand ??= new DelegateCommand(Undo);
                return _undoCommand;
            }
        }

        #endregion

        #region Lifecycle

        protected override async Task OnInitializedAsync(CancellationToken cancellationToken)
        {
            try
            {
                IsBusy = true;
                await LoadBillingConfigAsync();
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"{GetType().Name}.{nameof(OnInitializedAsync)}: {ex.GetErrorMessage()}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }

            await base.OnInitializedAsync(cancellationToken);
        }

        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            if (close)
            {
                Messenger.Default.Unregister(this);
            }
            return base.OnDeactivateAsync(close, cancellationToken);
        }

        #endregion

        #region Load

        private async Task LoadBillingConfigAsync()
        {
            var (_, query) = _loadQuery.Value;
            var config = await _billingConfigService.GetSingleItemAsync(query, new { });

            if (config is not null)
            {
                _loadedConfig = config;
                Id = config.Id;

                _selectedDefaultCustomer = config.DefaultCustomer;
                NotifyOfPropertyChange(nameof(SelectedDefaultCustomer));
                NotifyOfPropertyChange(nameof(SelectedDefaultCustomerDisplay));

                SeedCurrentValues();
            }
        }

        private void SeedCurrentValues()
        {
            this.SeedValue(nameof(SelectedDefaultCustomer), SelectedDefaultCustomer);
            this.AcceptChanges();
            NotifyOfPropertyChange(nameof(CanSave));
        }

        #endregion

        #region Undo

        private void Undo()
        {
            _selectedDefaultCustomer = _loadedConfig?.DefaultCustomer;
            NotifyOfPropertyChange(nameof(SelectedDefaultCustomer));
            NotifyOfPropertyChange(nameof(SelectedDefaultCustomerDisplay));

            SeedCurrentValues();
            IsEditing = false;
        }

        #endregion

        #region Save

        public async Task SaveAsync()
        {
            try
            {
                IsBusy = true;

                UpsertResponseType<BillingConfigGraphQLModel> result;

                if (IsNewRecord)
                {
                    var (_, query) = _createQuery.Value;
                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "createResponseInput");
                    result = await _billingConfigService.CreateAsync<UpsertResponseType<BillingConfigGraphQLModel>>(query, variables);
                }
                else
                {
                    var (_, query) = _updateQuery.Value;
                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "updateResponseData");
                    result = await _billingConfigService.UpdateAsync<UpsertResponseType<BillingConfigGraphQLModel>>(query, variables);
                }

                if (!result.Success)
                {
                    await _joinableTaskFactory.SwitchToMainThreadAsync();
                    ThemedMessageBox.Show(
                        text: $"El guardado no ha sido exitoso\r\n\r\n{result.Errors.ToUserMessage()}\r\n\r\nVerifique los datos y vuelva a intentarlo",
                        title: $"{result.Message}!",
                        messageBoxButtons: MessageBoxButton.OK,
                        icon: MessageBoxImage.Error);
                    return;
                }

                if (result.Entity is not null)
                {
                    _loadedConfig = result.Entity;
                    Id = result.Entity.Id;
                }

                _notificationService.ShowSuccess(result.Message);
                SeedCurrentValues();
                IsEditing = false;
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"{GetType().Name}.{nameof(SaveAsync)}: {ex.GetErrorMessage()}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region Customer Search

        public async Task OpenDefaultCustomerSearchAsync()
        {
            string query = GetSearchActiveCustomersQuery();

            dynamic variables = new ExpandoObject();
            variables.pageResponseFilters = new ExpandoObject();
            variables.pageResponseFilters.isActive = true;

            var viewModel = new SearchWithTwoColumnsGridViewModel<CustomerGraphQLModel>(
                query,
                fieldHeader1: "Identificación",
                fieldHeader2: "Nombre / Razón Social",
                fieldData1: "AccountingEntity.IdentificationNumber",
                fieldData2: "AccountingEntity.SearchName",
                variables: variables,
                SearchWithTwoColumnsGridMessageToken.BillingConfigDefaultCustomer,
                _dialogService);

            await _dialogService.ShowDialogAsync(viewModel, "Búsqueda de clientes");
        }

        private void OnFindDefaultCustomerMessage(ReturnedDataFromModalWithTwoColumnsGridViewMessage<CustomerGraphQLModel> message)
        {
            if (message?.ReturnedData is null) return;
            SelectedDefaultCustomer = message.ReturnedData;
        }

        #endregion

        #region GraphQL Queries

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadQuery = new(() =>
        {
            var fields = FieldSpec<BillingConfigGraphQLModel>
                .Create()
                .Field(f => f.Id)
                .Select(f => f.DefaultCustomer, nested: c => c
                    .Field(c => c.Id)
                    .Select(c => c.AccountingEntity, nested: a => a
                        .Field(a => a.Id)
                        .Field(a => a.IdentificationNumber)
                        .Field(a => a.VerificationDigit)
                        .Field(a => a.SearchName)))
                .Build();

            var fragment = new GraphQLQueryFragment("billingConfig", [], fields, "SingleItemResponse");
            var query = new QueryBuilder([fragment]).GetQuery();

            return (fragment, query);
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _createQuery = new(() =>
        {
            var fields = FieldSpec<UpsertResponseType<BillingConfigGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "billingConfig", nested: sq => sq
                    .Field(f => f.Id)
                    .Select(f => f.DefaultCustomer, nested: c => c
                        .Field(c => c.Id)
                        .Select(c => c.AccountingEntity, nested: a => a
                            .Field(a => a.Id)
                            .Field(a => a.IdentificationNumber)
                            .Field(a => a.VerificationDigit)
                            .Field(a => a.SearchName))))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var fragment = new GraphQLQueryFragment("createBillingConfig",
                [new("input", "CreateBillingConfigInput!")],
                fields, "CreateResponse");
            return (fragment, new QueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _updateQuery = new(() =>
        {
            var fields = FieldSpec<UpsertResponseType<BillingConfigGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "billingConfig", nested: sq => sq
                    .Field(f => f.Id)
                    .Select(f => f.DefaultCustomer, nested: c => c
                        .Field(c => c.Id)
                        .Select(c => c.AccountingEntity, nested: a => a
                            .Field(a => a.Id)
                            .Field(a => a.IdentificationNumber)
                            .Field(a => a.VerificationDigit)
                            .Field(a => a.SearchName))))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var fragment = new GraphQLQueryFragment("updateBillingConfig",
                [new("data", "UpdateBillingConfigInput!")],
                fields, "UpdateResponse");
            return (fragment, new QueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static string GetSearchActiveCustomersQuery()
        {
            var fields = FieldSpec<PageType<CustomerGraphQLModel>>
                .Create()
                .Field(f => f.PageNumber)
                .Field(f => f.TotalEntries)
                .Field(f => f.PageSize)
                .SelectList(f => f.Entries, e => e
                    .Field(x => x.Id)
                    .Select(x => x.AccountingEntity, nested: a => a
                        .Field(ae => ae.Id)
                        .Field(ae => ae.IdentificationNumber)
                        .Field(ae => ae.VerificationDigit)
                        .Field(ae => ae.SearchName)))
                .Build();

            var fragment = new GraphQLQueryFragment("customersPage",
                [new("filters", "CustomerFilters"), new("pagination", "Pagination")],
                fields, "PageResponse");
            return new QueryBuilder([fragment]).GetQuery();
        }

        #endregion
    }
}
