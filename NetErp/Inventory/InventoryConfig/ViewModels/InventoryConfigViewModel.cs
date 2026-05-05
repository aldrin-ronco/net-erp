using Caliburn.Micro;
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
        private readonly StorageCache _storageCache;
        private readonly NotAnnulledAccountingSourceCache _accountingSourceCache;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly INotificationService _notificationService;
        private InventoryConfigGraphQLModel? _loadedConfig;

        public InventoryConfigViewModel(
            IRepository<InventoryConfigGraphQLModel> inventoryConfigService,
            StorageCache storageCache,
            NotAnnulledAccountingSourceCache accountingSourceCache,
            JoinableTaskFactory joinableTaskFactory,
            INotificationService notificationService)
        {
            _inventoryConfigService = inventoryConfigService;
            _storageCache = storageCache;
            _accountingSourceCache = accountingSourceCache;
            _joinableTaskFactory = joinableTaskFactory;
            _notificationService = notificationService;
        }

        #region Properties

        public int Id
        {
            get;
            set
            {
                if (field == value) return;
                field = value;
                NotifyOfPropertyChange(nameof(Id));
                NotifyOfPropertyChange(nameof(IsNewRecord));
            }
        }

        public bool IsNewRecord => Id == 0;

        public bool IsBusy
        {
            get;
            set
            {
                if (field == value) return;
                field = value;
                NotifyOfPropertyChange(nameof(IsBusy));
                NotifyOfPropertyChange(nameof(CanEdit));
                NotifyOfPropertyChange(nameof(CanSave));
            }
        }

        public bool IsEditing
        {
            get;
            set
            {
                if (field == value) return;
                field = value;
                NotifyOfPropertyChange(nameof(IsEditing));
                NotifyOfPropertyChange(nameof(CanEdit));
                NotifyOfPropertyChange(nameof(CanSave));
            }
        }

        public bool CanEdit => !IsEditing && !IsBusy;

        [ExpandoPath("transitStorageId", SerializeAsId = true)]
        public StorageGraphQLModel? SelectedTransitStorage
        {
            get;
            set
            {
                if (field == value) return;
                field = value;
                NotifyOfPropertyChange(nameof(SelectedTransitStorage));
                this.TrackChange(nameof(SelectedTransitStorage), value);
                NotifyOfPropertyChange(nameof(CanSave));
            }
        }

        [ExpandoPath("transferDocAccountingSourceId", SerializeAsId = true)]
        public AccountingSourceGraphQLModel? SelectedTransferDocAccountingSource
        {
            get;
            set
            {
                if (field == value) return;
                field = value;
                NotifyOfPropertyChange(nameof(SelectedTransferDocAccountingSource));
                this.TrackChange(nameof(SelectedTransferDocAccountingSource), value);
                NotifyOfPropertyChange(nameof(CanSave));
            }
        }

        [ExpandoPath("transferSourceOutAccountingSourceId", SerializeAsId = true)]
        public AccountingSourceGraphQLModel? SelectedTransferSourceOutAccountingSource
        {
            get;
            set
            {
                if (field == value) return;
                field = value;
                NotifyOfPropertyChange(nameof(SelectedTransferSourceOutAccountingSource));
                this.TrackChange(nameof(SelectedTransferSourceOutAccountingSource), value);
                NotifyOfPropertyChange(nameof(CanSave));
            }
        }

        [ExpandoPath("transferDestinationInAccountingSourceId", SerializeAsId = true)]
        public AccountingSourceGraphQLModel? SelectedTransferDestinationInAccountingSource
        {
            get;
            set
            {
                if (field == value) return;
                field = value;
                NotifyOfPropertyChange(nameof(SelectedTransferDestinationInAccountingSource));
                this.TrackChange(nameof(SelectedTransferDestinationInAccountingSource), value);
                NotifyOfPropertyChange(nameof(CanSave));
            }
        }

        [ExpandoPath("transferTransitOutAccountingSourceId", SerializeAsId = true)]
        public AccountingSourceGraphQLModel? SelectedTransferTransitOutAccountingSource
        {
            get;
            set
            {
                if (field == value) return;
                field = value;
                NotifyOfPropertyChange(nameof(SelectedTransferTransitOutAccountingSource));
                this.TrackChange(nameof(SelectedTransferTransitOutAccountingSource), value);
                NotifyOfPropertyChange(nameof(CanSave));
            }
        }

        [ExpandoPath("transferTransitInAccountingSourceId", SerializeAsId = true)]
        public AccountingSourceGraphQLModel? SelectedTransferTransitInAccountingSource
        {
            get;
            set
            {
                if (field == value) return;
                field = value;
                NotifyOfPropertyChange(nameof(SelectedTransferTransitInAccountingSource));
                this.TrackChange(nameof(SelectedTransferTransitInAccountingSource), value);
                NotifyOfPropertyChange(nameof(CanSave));
            }
        }

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

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            this.SetFocus(() => SelectedTransitStorage);
        }

        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            if (close)
            {
                this.AcceptChanges();
            }
            return base.OnDeactivateAsync(close, cancellationToken);
        }

        #endregion

        #region Load

        private async Task LoadInventoryConfigAsync()
        {
            var (_, query) = _loadQuery.Value;
            InventoryConfigGraphQLModel? config = await _inventoryConfigService.GetSingleItemAsync(query, new { });

            if (config is not null)
            {
                _loadedConfig = config;
                Id = config.Id;

                SelectedTransitStorage = Storages.FirstOrDefault(x => x.Id == config.TransitStorage?.Id);
                SelectedTransferDocAccountingSource = AccountingSources.FirstOrDefault(x => x.Id == config.TransferDocAccountingSource?.Id);
                SelectedTransferSourceOutAccountingSource = AccountingSources.FirstOrDefault(x => x.Id == config.TransferSourceOutAccountingSource?.Id);
                SelectedTransferDestinationInAccountingSource = AccountingSources.FirstOrDefault(x => x.Id == config.TransferDestinationInAccountingSource?.Id);
                SelectedTransferTransitOutAccountingSource = AccountingSources.FirstOrDefault(x => x.Id == config.TransferTransitOutAccountingSource?.Id);
                SelectedTransferTransitInAccountingSource = AccountingSources.FirstOrDefault(x => x.Id == config.TransferTransitInAccountingSource?.Id);

                SeedCurrentValues();
            }
            else
            {
                SeedDefaultValues();
            }
        }

        private void SeedDefaultValues()
        {
            this.ClearSeeds();
            this.AcceptChanges();
            NotifyOfPropertyChange(nameof(CanSave));
        }

        private void SeedCurrentValues()
        {
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
            this.ClearSeeds();

            SelectedTransitStorage = Storages.FirstOrDefault(x => x.Id == _loadedConfig?.TransitStorage?.Id);
            SelectedTransferDocAccountingSource = AccountingSources.FirstOrDefault(x => x.Id == _loadedConfig?.TransferDocAccountingSource?.Id);
            SelectedTransferSourceOutAccountingSource = AccountingSources.FirstOrDefault(x => x.Id == _loadedConfig?.TransferSourceOutAccountingSource?.Id);
            SelectedTransferDestinationInAccountingSource = AccountingSources.FirstOrDefault(x => x.Id == _loadedConfig?.TransferDestinationInAccountingSource?.Id);
            SelectedTransferTransitOutAccountingSource = AccountingSources.FirstOrDefault(x => x.Id == _loadedConfig?.TransferTransitOutAccountingSource?.Id);
            SelectedTransferTransitInAccountingSource = AccountingSources.FirstOrDefault(x => x.Id == _loadedConfig?.TransferTransitInAccountingSource?.Id);

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
