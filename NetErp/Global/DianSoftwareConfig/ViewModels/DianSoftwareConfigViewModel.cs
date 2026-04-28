using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Extensions.Global;
using Microsoft.VisualStudio.Threading;
using Models.Global;
using NetErp.Helpers;
using NetErp.Helpers.GraphQLQueryBuilder;
using INotificationService = NetErp.Helpers.Services.INotificationService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;
using QueryBuilder = NetErp.Helpers.GraphQLQueryBuilder.GraphQLQueryBuilder;

namespace NetErp.Global.DianSoftwareConfig.ViewModels
{
    public class DianSoftwareConfigViewModel : Screen
    {
        private const string CategoryInvoice = "INVOICE";
        private const string CategoryPayroll = "PAYROLL";
        private const string CategorySupportDocument = "SUPPORT_DOCUMENT";
        private const string EnvironmentProduction = "PRODUCTION";
        private const string EnvironmentSandbox = "SANDBOX";

        private readonly IRepository<DianSoftwareConfigGraphQLModel> _service;
        private readonly IEventAggregator _eventAggregator;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly INotificationService _notificationService;

        private readonly Dictionary<(string Category, string Environment), DianSoftwareConfigGraphQLModel> _loadedConfigs = [];

        public DianSoftwareConfigViewModel(
            IRepository<DianSoftwareConfigGraphQLModel> service,
            IEventAggregator eventAggregator,
            JoinableTaskFactory joinableTaskFactory,
            INotificationService notificationService)
        {
            _service = service;
            _eventAggregator = eventAggregator;
            _joinableTaskFactory = joinableTaskFactory;
            _notificationService = notificationService;
        }

        #region State

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

        public bool IsNewRecord => !_loadedConfigs.ContainsKey((SelectedCategory, SelectedEnvironment));

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
                NotifyOfPropertyChange(nameof(CanSelectInvoiceTab));
                NotifyOfPropertyChange(nameof(CanSelectPayrollTab));
                NotifyOfPropertyChange(nameof(CanSelectSupportTab));
                NotifyOfPropertyChange(nameof(CanChangeEnvironment));
            }
        }

        public bool CanEdit => !IsEditing && !IsBusy;
        public bool CanSave => IsEditing && this.HasChanges() && !IsBusy;

        #endregion

        #region Navigation (category / environment selection)

        private int _selectedCategoryIndex;
        public int SelectedCategoryIndex
        {
            get => _selectedCategoryIndex;
            set
            {
                if (_selectedCategoryIndex == value) return;
                if (IsEditing) return;
                _selectedCategoryIndex = value;
                _selectedEnvironment = EnvironmentProduction;
                NotifyOfPropertyChange(nameof(SelectedCategoryIndex));
                NotifyOfPropertyChange(nameof(SelectedCategory));
                NotifyOfPropertyChange(nameof(SelectedEnvironment));
                NotifyOfPropertyChange(nameof(IsProductionSelected));
                NotifyOfPropertyChange(nameof(IsSandboxSelected));
                NotifyOfPropertyChange(nameof(IsNewRecord));
                NotifyOfPropertyChange(nameof(CanSelectInvoiceTab));
                NotifyOfPropertyChange(nameof(CanSelectPayrollTab));
                NotifyOfPropertyChange(nameof(CanSelectSupportTab));
                LoadCurrentSlot();
            }
        }

        public string SelectedCategory => _selectedCategoryIndex switch
        {
            0 => CategoryInvoice,
            1 => CategoryPayroll,
            2 => CategorySupportDocument,
            _ => CategoryInvoice
        };

        private string _selectedEnvironment = EnvironmentProduction;
        public string SelectedEnvironment
        {
            get => _selectedEnvironment;
            private set
            {
                if (_selectedEnvironment == value) return;
                _selectedEnvironment = value;
                NotifyOfPropertyChange(nameof(SelectedEnvironment));
                NotifyOfPropertyChange(nameof(IsProductionSelected));
                NotifyOfPropertyChange(nameof(IsSandboxSelected));
                NotifyOfPropertyChange(nameof(IsNewRecord));
                LoadCurrentSlot();
            }
        }

        public bool IsProductionSelected
        {
            get => _selectedEnvironment == EnvironmentProduction;
            set
            {
                if (!value || IsEditing) return;
                SelectedEnvironment = EnvironmentProduction;
            }
        }

        public bool IsSandboxSelected
        {
            get => _selectedEnvironment == EnvironmentSandbox;
            set
            {
                if (!value || IsEditing) return;
                SelectedEnvironment = EnvironmentSandbox;
            }
        }

        public bool CanSelectInvoiceTab => !IsEditing || SelectedCategory == CategoryInvoice;
        public bool CanSelectPayrollTab => !IsEditing || SelectedCategory == CategoryPayroll;
        public bool CanSelectSupportTab => !IsEditing || SelectedCategory == CategorySupportDocument;
        public bool CanChangeEnvironment => !IsEditing;

        #endregion

        #region Form properties

        private string _providerNit = string.Empty;
        [ExpandoPath("providerNit")]
        public string ProviderNit
        {
            get => _providerNit;
            set
            {
                if (_providerNit == value) return;
                _providerNit = value;
                NotifyOfPropertyChange(nameof(ProviderNit));
                this.TrackChange(nameof(ProviderNit));
                NotifyOfPropertyChange(nameof(CanSave));
            }
        }

        private string _providerDv = string.Empty;
        [ExpandoPath("providerDv")]
        public string ProviderDv
        {
            get => _providerDv;
            set
            {
                if (_providerDv == value) return;
                _providerDv = value;
                NotifyOfPropertyChange(nameof(ProviderDv));
                this.TrackChange(nameof(ProviderDv));
                NotifyOfPropertyChange(nameof(CanSave));
            }
        }

        private string _softwareId = string.Empty;
        [ExpandoPath("softwareId")]
        public string SoftwareId
        {
            get => _softwareId;
            set
            {
                if (_softwareId == value) return;
                _softwareId = value;
                NotifyOfPropertyChange(nameof(SoftwareId));
                this.TrackChange(nameof(SoftwareId));
                NotifyOfPropertyChange(nameof(CanSave));
            }
        }

        private string _softwarePin = string.Empty;
        [ExpandoPath("softwarePin")]
        public string SoftwarePin
        {
            get => _softwarePin;
            set
            {
                if (_softwarePin == value) return;
                _softwarePin = value;
                NotifyOfPropertyChange(nameof(SoftwarePin));
                this.TrackChange(nameof(SoftwarePin));
                NotifyOfPropertyChange(nameof(CanSave));
            }
        }

        private string _testSetId = string.Empty;
        [ExpandoPath("testSetId")]
        public string TestSetId
        {
            get => _testSetId;
            set
            {
                if (_testSetId == value) return;
                _testSetId = value;
                NotifyOfPropertyChange(nameof(TestSetId));
                this.TrackChange(nameof(TestSetId));
                NotifyOfPropertyChange(nameof(CanSave));
            }
        }

        private string _serviceUrl = string.Empty;
        [ExpandoPath("serviceUrl")]
        public string ServiceUrl
        {
            get => _serviceUrl;
            set
            {
                if (_serviceUrl == value) return;
                _serviceUrl = value;
                NotifyOfPropertyChange(nameof(ServiceUrl));
                this.TrackChange(nameof(ServiceUrl));
                NotifyOfPropertyChange(nameof(CanSave));
            }
        }

        private string _wsdlUrl = string.Empty;
        [ExpandoPath("wsdlUrl")]
        public string WsdlUrl
        {
            get => _wsdlUrl;
            set
            {
                if (_wsdlUrl == value) return;
                _wsdlUrl = value;
                NotifyOfPropertyChange(nameof(WsdlUrl));
                this.TrackChange(nameof(WsdlUrl));
                NotifyOfPropertyChange(nameof(CanSave));
            }
        }

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
                await LoadConfigsAsync();
                LoadCurrentSlot();
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
                _loadedConfigs.Clear();
            }
            return base.OnDeactivateAsync(close, cancellationToken);
        }

        #endregion

        #region Load

        private async Task LoadConfigsAsync()
        {
            var (_, query) = _loadListQuery.Value;
            IEnumerable<DianSoftwareConfigGraphQLModel> configs = await _service.GetListAsync(query, new { });

            _loadedConfigs.Clear();
            foreach (var config in configs ?? [])
            {
                if (string.IsNullOrEmpty(config.DocumentCategory) || string.IsNullOrEmpty(config.Environment))
                    continue;

                _loadedConfigs[(config.DocumentCategory, config.Environment)] = config;
            }
        }

        private void LoadCurrentSlot()
        {
            if (_loadedConfigs.TryGetValue((SelectedCategory, SelectedEnvironment), out var config))
            {
                _id = config.Id;
                _providerNit = config.ProviderNit ?? string.Empty;
                _providerDv = config.ProviderDv ?? string.Empty;
                _softwareId = config.SoftwareId ?? string.Empty;
                _softwarePin = config.SoftwarePin ?? string.Empty;
                _testSetId = config.TestSetId ?? string.Empty;
                _serviceUrl = config.ServiceUrl ?? string.Empty;
                _wsdlUrl = config.WsdlUrl ?? string.Empty;
            }
            else
            {
                _id = 0;
                _providerNit = string.Empty;
                _providerDv = string.Empty;
                _softwareId = string.Empty;
                _softwarePin = string.Empty;
                _testSetId = string.Empty;
                _serviceUrl = string.Empty;
                _wsdlUrl = string.Empty;
            }

            NotifyOfPropertyChange(nameof(Id));
            NotifyOfPropertyChange(nameof(ProviderNit));
            NotifyOfPropertyChange(nameof(ProviderDv));
            NotifyOfPropertyChange(nameof(SoftwareId));
            NotifyOfPropertyChange(nameof(SoftwarePin));
            NotifyOfPropertyChange(nameof(TestSetId));
            NotifyOfPropertyChange(nameof(ServiceUrl));
            NotifyOfPropertyChange(nameof(WsdlUrl));
            NotifyOfPropertyChange(nameof(IsNewRecord));

            SeedCurrentValues();
        }

        private void SeedCurrentValues()
        {
            this.ClearSeeds();
            this.SeedValue(nameof(ProviderNit), ProviderNit);
            this.SeedValue(nameof(ProviderDv), ProviderDv);
            this.SeedValue(nameof(SoftwareId), SoftwareId);
            this.SeedValue(nameof(SoftwarePin), SoftwarePin);
            this.SeedValue(nameof(TestSetId), TestSetId);
            this.SeedValue(nameof(ServiceUrl), ServiceUrl);
            this.SeedValue(nameof(WsdlUrl), WsdlUrl);
            this.AcceptChanges();
            NotifyOfPropertyChange(nameof(CanSave));
        }

        #endregion

        #region Undo

        private void Undo()
        {
            LoadCurrentSlot();
            IsEditing = false;
        }

        #endregion

        #region Save

        public async Task SaveAsync()
        {
            try
            {
                IsBusy = true;

                UpsertResponseType<DianSoftwareConfigGraphQLModel> result;

                if (IsNewRecord)
                {
                    var (_, query) = _createQuery.Value;
                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "createResponseInput");
                    variables.createResponseInput.documentCategory = SelectedCategory;
                    variables.createResponseInput.environment = SelectedEnvironment;
                    result = await _service.CreateAsync<UpsertResponseType<DianSoftwareConfigGraphQLModel>>(query, variables);
                }
                else
                {
                    var (_, query) = _updateQuery.Value;
                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "updateResponseData");
                    variables.updateResponseId = Id;
                    result = await _service.UpdateAsync<UpsertResponseType<DianSoftwareConfigGraphQLModel>>(query, variables);
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
                    _loadedConfigs[(SelectedCategory, SelectedEnvironment)] = result.Entity;
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

        #region GraphQL Queries

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadListQuery = new(() =>
        {
            var fields = FieldSpec<DianSoftwareConfigGraphQLModel>
                .Create()
                .Field(f => f.Id)
                .Field(f => f.DocumentCategory)
                .Field(f => f.Environment)
                .Field(f => f.ProviderNit)
                .Field(f => f.ProviderDv)
                .Field(f => f.SoftwareId)
                .Field(f => f.SoftwarePin)
                .Field(f => f.TestSetId)
                .Field(f => f.ServiceUrl)
                .Field(f => f.WsdlUrl)
                .Build();

            var fragment = new GraphQLQueryFragment("dianSoftwareConfigs", [], fields, "ListResponse");
            var query = new QueryBuilder([fragment]).GetQuery();

            return (fragment, query);
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _createQuery = new(() =>
        {
            var fields = FieldSpec<UpsertResponseType<DianSoftwareConfigGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "config", nested: sq => sq
                    .Field(f => f.Id)
                    .Field(f => f.DocumentCategory)
                    .Field(f => f.Environment)
                    .Field(f => f.ProviderNit)
                    .Field(f => f.ProviderDv)
                    .Field(f => f.SoftwareId)
                    .Field(f => f.SoftwarePin)
                    .Field(f => f.TestSetId)
                    .Field(f => f.ServiceUrl)
                    .Field(f => f.WsdlUrl))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var fragment = new GraphQLQueryFragment("createDianSoftwareConfig",
                [new("input", "CreateDianSoftwareConfigInput!")],
                fields, "CreateResponse");
            return (fragment, new QueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _updateQuery = new(() =>
        {
            var fields = FieldSpec<UpsertResponseType<DianSoftwareConfigGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "config", nested: sq => sq
                    .Field(f => f.Id)
                    .Field(f => f.DocumentCategory)
                    .Field(f => f.Environment)
                    .Field(f => f.ProviderNit)
                    .Field(f => f.ProviderDv)
                    .Field(f => f.SoftwareId)
                    .Field(f => f.SoftwarePin)
                    .Field(f => f.TestSetId)
                    .Field(f => f.ServiceUrl)
                    .Field(f => f.WsdlUrl))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var fragment = new GraphQLQueryFragment("updateDianSoftwareConfig",
                [new("id", "ID!"), new("data", "UpdateDianSoftwareConfigInput!")],
                fields, "UpdateResponse");
            return (fragment, new QueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        #endregion
    }
}
