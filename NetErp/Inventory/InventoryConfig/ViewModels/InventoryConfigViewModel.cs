using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Extensions.Global;
using Microsoft.VisualStudio.Threading;
using Models.Books;
using Models.Global;
using Models.Inventory;
using NetErp.Helpers;
using NetErp.Helpers.Cache;
using NetErp.Helpers.GraphQLQueryBuilder;
using INotificationService = NetErp.Helpers.Services.INotificationService;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;
using QueryBuilder = NetErp.Helpers.GraphQLQueryBuilder.GraphQLQueryBuilder;

namespace NetErp.Inventory.InventoryConfig.ViewModels
{
    public class InventoryConfigViewModel : Screen
    {
        private readonly IRepository<InventoryConfigGraphQLModel> _inventoryConfigService;
        private readonly AwsS3ConfigCache _awsS3ConfigCache;
        private readonly StorageCache _storageCache;
        private readonly NotAnnulledAccountingSourceCache _accountingSourceCache;
        private readonly IEventAggregator _eventAggregator;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly INotificationService _notificationService;
        private InventoryConfigGraphQLModel? _loadedConfig;

        public InventoryConfigViewModel(
            IRepository<InventoryConfigGraphQLModel> inventoryConfigService,
            AwsS3ConfigCache awsS3ConfigCache,
            StorageCache storageCache,
            NotAnnulledAccountingSourceCache accountingSourceCache,
            IEventAggregator eventAggregator,
            JoinableTaskFactory joinableTaskFactory,
            INotificationService notificationService)
        {
            _inventoryConfigService = inventoryConfigService;
            _awsS3ConfigCache = awsS3ConfigCache;
            _storageCache = storageCache;
            _accountingSourceCache = accountingSourceCache;
            _eventAggregator = eventAggregator;
            _joinableTaskFactory = joinableTaskFactory;
            _notificationService = notificationService;
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

        private AwsS3ConfigGraphQLModel? _selectedDefaultAwsS3Config;

        [ExpandoPath("defaultAwsS3ConfigId", SerializeAsId = true)]
        public AwsS3ConfigGraphQLModel? SelectedDefaultAwsS3Config
        {
            get => _selectedDefaultAwsS3Config;
            set
            {
                if (_selectedDefaultAwsS3Config == value) return;
                _selectedDefaultAwsS3Config = value;
                NotifyOfPropertyChange(nameof(SelectedDefaultAwsS3Config));
                this.TrackChange(nameof(SelectedDefaultAwsS3Config), value);
                NotifyOfPropertyChange(nameof(CanSave));
            }
        }

        private StorageGraphQLModel? _selectedTransitStorage;

        [ExpandoPath("transitStorageId", SerializeAsId = true)]
        public StorageGraphQLModel? SelectedTransitStorage
        {
            get => _selectedTransitStorage;
            set
            {
                if (_selectedTransitStorage == value) return;
                _selectedTransitStorage = value;
                NotifyOfPropertyChange(nameof(SelectedTransitStorage));
                this.TrackChange(nameof(SelectedTransitStorage), value);
                NotifyOfPropertyChange(nameof(CanSave));
            }
        }

        private AccountingSourceGraphQLModel? _selectedTransferDocAccountingSource;

        [ExpandoPath("transferDocAccountingSourceId", SerializeAsId = true)]
        public AccountingSourceGraphQLModel? SelectedTransferDocAccountingSource
        {
            get => _selectedTransferDocAccountingSource;
            set
            {
                if (_selectedTransferDocAccountingSource == value) return;
                _selectedTransferDocAccountingSource = value;
                NotifyOfPropertyChange(nameof(SelectedTransferDocAccountingSource));
                this.TrackChange(nameof(SelectedTransferDocAccountingSource), value);
                NotifyOfPropertyChange(nameof(CanSave));
            }
        }

        private AccountingSourceGraphQLModel? _selectedTransferSourceOutAccountingSource;

        [ExpandoPath("transferSourceOutAccountingSourceId", SerializeAsId = true)]
        public AccountingSourceGraphQLModel? SelectedTransferSourceOutAccountingSource
        {
            get => _selectedTransferSourceOutAccountingSource;
            set
            {
                if (_selectedTransferSourceOutAccountingSource == value) return;
                _selectedTransferSourceOutAccountingSource = value;
                NotifyOfPropertyChange(nameof(SelectedTransferSourceOutAccountingSource));
                this.TrackChange(nameof(SelectedTransferSourceOutAccountingSource), value);
                NotifyOfPropertyChange(nameof(CanSave));
            }
        }

        private AccountingSourceGraphQLModel? _selectedTransferDestinationInAccountingSource;

        [ExpandoPath("transferDestinationInAccountingSourceId", SerializeAsId = true)]
        public AccountingSourceGraphQLModel? SelectedTransferDestinationInAccountingSource
        {
            get => _selectedTransferDestinationInAccountingSource;
            set
            {
                if (_selectedTransferDestinationInAccountingSource == value) return;
                _selectedTransferDestinationInAccountingSource = value;
                NotifyOfPropertyChange(nameof(SelectedTransferDestinationInAccountingSource));
                this.TrackChange(nameof(SelectedTransferDestinationInAccountingSource), value);
                NotifyOfPropertyChange(nameof(CanSave));
            }
        }

        private AccountingSourceGraphQLModel? _selectedTransferTransitOutAccountingSource;

        [ExpandoPath("transferTransitOutAccountingSourceId", SerializeAsId = true)]
        public AccountingSourceGraphQLModel? SelectedTransferTransitOutAccountingSource
        {
            get => _selectedTransferTransitOutAccountingSource;
            set
            {
                if (_selectedTransferTransitOutAccountingSource == value) return;
                _selectedTransferTransitOutAccountingSource = value;
                NotifyOfPropertyChange(nameof(SelectedTransferTransitOutAccountingSource));
                this.TrackChange(nameof(SelectedTransferTransitOutAccountingSource), value);
                NotifyOfPropertyChange(nameof(CanSave));
            }
        }

        private AccountingSourceGraphQLModel? _selectedTransferTransitInAccountingSource;

        [ExpandoPath("transferTransitInAccountingSourceId", SerializeAsId = true)]
        public AccountingSourceGraphQLModel? SelectedTransferTransitInAccountingSource
        {
            get => _selectedTransferTransitInAccountingSource;
            set
            {
                if (_selectedTransferTransitInAccountingSource == value) return;
                _selectedTransferTransitInAccountingSource = value;
                NotifyOfPropertyChange(nameof(SelectedTransferTransitInAccountingSource));
                this.TrackChange(nameof(SelectedTransferTransitInAccountingSource), value);
                NotifyOfPropertyChange(nameof(CanSave));
            }
        }

        public ReadOnlyObservableCollection<AwsS3ConfigGraphQLModel> AwsS3Configs => _awsS3ConfigCache.Items;
        public ReadOnlyObservableCollection<StorageGraphQLModel> Storages => _storageCache.Items;
        public ReadOnlyObservableCollection<AccountingSourceGraphQLModel> AccountingSources => _accountingSourceCache.Items;

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

                await Task.WhenAll(
                    _awsS3ConfigCache.EnsureLoadedAsync(),
                    _storageCache.EnsureLoadedAsync(),
                    _accountingSourceCache.EnsureLoadedAsync());

                await LoadInventoryConfigAsync();
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

        #endregion

        #region Load

        private async Task LoadInventoryConfigAsync()
        {
            var (_, query) = _loadQuery.Value;
            var config = await _inventoryConfigService.GetSingleItemAsync(query, new { });

            if (config is not null)
            {
                _loadedConfig = config;
                Id = config.Id;

                _selectedDefaultAwsS3Config = AwsS3Configs.FirstOrDefault(x => x.Id == config.DefaultAwsS3Config?.Id);
                NotifyOfPropertyChange(nameof(SelectedDefaultAwsS3Config));

                _selectedTransitStorage = Storages.FirstOrDefault(x => x.Id == config.TransitStorage?.Id);
                NotifyOfPropertyChange(nameof(SelectedTransitStorage));

                _selectedTransferDocAccountingSource = AccountingSources.FirstOrDefault(x => x.Id == config.TransferDocAccountingSource?.Id);
                NotifyOfPropertyChange(nameof(SelectedTransferDocAccountingSource));

                _selectedTransferSourceOutAccountingSource = AccountingSources.FirstOrDefault(x => x.Id == config.TransferSourceOutAccountingSource?.Id);
                NotifyOfPropertyChange(nameof(SelectedTransferSourceOutAccountingSource));

                _selectedTransferDestinationInAccountingSource = AccountingSources.FirstOrDefault(x => x.Id == config.TransferDestinationInAccountingSource?.Id);
                NotifyOfPropertyChange(nameof(SelectedTransferDestinationInAccountingSource));

                _selectedTransferTransitOutAccountingSource = AccountingSources.FirstOrDefault(x => x.Id == config.TransferTransitOutAccountingSource?.Id);
                NotifyOfPropertyChange(nameof(SelectedTransferTransitOutAccountingSource));

                _selectedTransferTransitInAccountingSource = AccountingSources.FirstOrDefault(x => x.Id == config.TransferTransitInAccountingSource?.Id);
                NotifyOfPropertyChange(nameof(SelectedTransferTransitInAccountingSource));

                SeedCurrentValues();
            }
        }

        private void SeedCurrentValues()
        {
            this.SeedValue(nameof(SelectedDefaultAwsS3Config), SelectedDefaultAwsS3Config);
            this.SeedValue(nameof(SelectedTransitStorage), SelectedTransitStorage);
            this.SeedValue(nameof(SelectedTransferDocAccountingSource), SelectedTransferDocAccountingSource);
            this.SeedValue(nameof(SelectedTransferSourceOutAccountingSource), SelectedTransferSourceOutAccountingSource);
            this.SeedValue(nameof(SelectedTransferDestinationInAccountingSource), SelectedTransferDestinationInAccountingSource);
            this.SeedValue(nameof(SelectedTransferTransitOutAccountingSource), SelectedTransferTransitOutAccountingSource);
            this.SeedValue(nameof(SelectedTransferTransitInAccountingSource), SelectedTransferTransitInAccountingSource);
            this.AcceptChanges();
            NotifyOfPropertyChange(nameof(CanSave));
        }

        #endregion

        #region Undo

        private void Undo()
        {
            _selectedDefaultAwsS3Config = AwsS3Configs.FirstOrDefault(x => x.Id == _loadedConfig?.DefaultAwsS3Config?.Id);
            NotifyOfPropertyChange(nameof(SelectedDefaultAwsS3Config));

            _selectedTransitStorage = Storages.FirstOrDefault(x => x.Id == _loadedConfig?.TransitStorage?.Id);
            NotifyOfPropertyChange(nameof(SelectedTransitStorage));

            _selectedTransferDocAccountingSource = AccountingSources.FirstOrDefault(x => x.Id == _loadedConfig?.TransferDocAccountingSource?.Id);
            NotifyOfPropertyChange(nameof(SelectedTransferDocAccountingSource));

            _selectedTransferSourceOutAccountingSource = AccountingSources.FirstOrDefault(x => x.Id == _loadedConfig?.TransferSourceOutAccountingSource?.Id);
            NotifyOfPropertyChange(nameof(SelectedTransferSourceOutAccountingSource));

            _selectedTransferDestinationInAccountingSource = AccountingSources.FirstOrDefault(x => x.Id == _loadedConfig?.TransferDestinationInAccountingSource?.Id);
            NotifyOfPropertyChange(nameof(SelectedTransferDestinationInAccountingSource));

            _selectedTransferTransitOutAccountingSource = AccountingSources.FirstOrDefault(x => x.Id == _loadedConfig?.TransferTransitOutAccountingSource?.Id);
            NotifyOfPropertyChange(nameof(SelectedTransferTransitOutAccountingSource));

            _selectedTransferTransitInAccountingSource = AccountingSources.FirstOrDefault(x => x.Id == _loadedConfig?.TransferTransitInAccountingSource?.Id);
            NotifyOfPropertyChange(nameof(SelectedTransferTransitInAccountingSource));

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

                UpsertResponseType<InventoryConfigGraphQLModel> result;

                if (IsNewRecord)
                {
                    var (_, query) = _createQuery.Value;
                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "createResponseInput");
                    result = await _inventoryConfigService.CreateAsync<UpsertResponseType<InventoryConfigGraphQLModel>>(query, variables);
                }
                else
                {
                    var (_, query) = _updateQuery.Value;
                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "updateResponseData");
                    result = await _inventoryConfigService.UpdateAsync<UpsertResponseType<InventoryConfigGraphQLModel>>(query, variables);
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

        #region GraphQL Queries

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadQuery = new(() =>
        {
            var fields = FieldSpec<InventoryConfigGraphQLModel>
                .Create()
                .Field(f => f.Id)
                .Select(f => f.DefaultAwsS3Config, nested: a => a.Field(a => a.Id))
                .Select(f => f.TransitStorage, nested: s => s.Field(s => s.Id))
                .Select(f => f.TransferDocAccountingSource, nested: a => a.Field(a => a.Id))
                .Select(f => f.TransferSourceOutAccountingSource, nested: a => a.Field(a => a.Id))
                .Select(f => f.TransferDestinationInAccountingSource, nested: a => a.Field(a => a.Id))
                .Select(f => f.TransferTransitOutAccountingSource, nested: a => a.Field(a => a.Id))
                .Select(f => f.TransferTransitInAccountingSource, nested: a => a.Field(a => a.Id))
                .Build();

            var fragment = new GraphQLQueryFragment("inventoryConfig", [], fields, "SingleItemResponse");
            var query = new QueryBuilder([fragment]).GetQuery();

            return (fragment, query);
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _createQuery = new(() =>
        {
            var fields = FieldSpec<UpsertResponseType<InventoryConfigGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "inventoryConfig", nested: sq => sq
                    .Field(f => f.Id)
                    .Select(f => f.DefaultAwsS3Config, nested: a => a.Field(a => a.Id))
                    .Select(f => f.TransitStorage, nested: s => s.Field(s => s.Id))
                    .Select(f => f.TransferDocAccountingSource, nested: a => a.Field(a => a.Id))
                    .Select(f => f.TransferSourceOutAccountingSource, nested: a => a.Field(a => a.Id))
                    .Select(f => f.TransferDestinationInAccountingSource, nested: a => a.Field(a => a.Id))
                    .Select(f => f.TransferTransitOutAccountingSource, nested: a => a.Field(a => a.Id))
                    .Select(f => f.TransferTransitInAccountingSource, nested: a => a.Field(a => a.Id)))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var fragment = new GraphQLQueryFragment("createInventoryConfig",
                [new("input", "CreateInventoryConfigInput!")],
                fields, "CreateResponse");
            return (fragment, new QueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _updateQuery = new(() =>
        {
            var fields = FieldSpec<UpsertResponseType<InventoryConfigGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "inventoryConfig", nested: sq => sq
                    .Field(f => f.Id)
                    .Select(f => f.DefaultAwsS3Config, nested: a => a.Field(a => a.Id))
                    .Select(f => f.TransitStorage, nested: s => s.Field(s => s.Id))
                    .Select(f => f.TransferDocAccountingSource, nested: a => a.Field(a => a.Id))
                    .Select(f => f.TransferSourceOutAccountingSource, nested: a => a.Field(a => a.Id))
                    .Select(f => f.TransferDestinationInAccountingSource, nested: a => a.Field(a => a.Id))
                    .Select(f => f.TransferTransitOutAccountingSource, nested: a => a.Field(a => a.Id))
                    .Select(f => f.TransferTransitInAccountingSource, nested: a => a.Field(a => a.Id)))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var fragment = new GraphQLQueryFragment("updateInventoryConfig",
                [new("data", "UpdateInventoryConfigInput!")],
                fields, "UpdateResponse");
            return (fragment, new QueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        #endregion
    }
}
