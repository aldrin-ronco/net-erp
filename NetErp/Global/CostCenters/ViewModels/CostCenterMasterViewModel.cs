using AutoMapper;
using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Microsoft.VisualStudio.Threading;
using Models.Global;
using NetErp.Global.CostCenters.DTO;
using NetErp.Global.CostCenters.Validators;
using NetErp.Helpers;
using NetErp.Helpers.Cache;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Global.CostCenters.ViewModels
{
    /// <summary>
    /// Master tree-only para CostCenters. Todos los CRUD se realizan en dialogs
    /// (Company/CompanyLocation/CostCenter/Storage). El master solo gestiona el árbol,
    /// la carga lazy de nodos hijos y refresca quirúrgicamente al recibir mensajes
    /// de Create/Update/Delete de las entidades del módulo.
    /// </summary>
    public class CostCenterMasterViewModel : Screen,
        IHandle<CostCenterCreateMessage>,
        IHandle<CostCenterUpdateMessage>,
        IHandle<CostCenterDeleteMessage>,
        IHandle<StorageCreateMessage>,
        IHandle<StorageUpdateMessage>,
        IHandle<StorageDeleteMessage>,
        IHandle<CompanyLocationCreateMessage>,
        IHandle<CompanyLocationUpdateMessage>,
        IHandle<CompanyLocationDeleteMessage>,
        IHandle<CompanyUpdateMessage>,
        IHandle<PermissionsCacheRefreshedMessage>
    {
        #region Dependencies

        public CostCenterViewModel Context { get; set; }

        private readonly IRepository<CompanyGraphQLModel> _companyService;
        private readonly IRepository<CompanyLocationGraphQLModel> _companyLocationService;
        private readonly IRepository<CostCenterGraphQLModel> _costCenterService;
        private readonly IRepository<StorageGraphQLModel> _storageService;
        private readonly NetErp.Helpers.IDialogService _dialogService;
        private readonly NetErp.Helpers.Services.INotificationService _notificationService;
        private readonly CountryCache _countryCache;
        private readonly StringLengthCache _stringLengthCache;
        private readonly PermissionCache _permissionCache;
        private readonly AuthorizationSequenceCache _authorizationSequenceCache;
        private readonly CompanyCache _companyCache;
        private readonly CompanyLocationCache _companyLocationCache;
        private readonly CostCenterCache _costCenterCache;
        private readonly StorageCache _storageCache;
        private readonly IGraphQLClient _graphQLClient;
        private readonly JoinableTaskFactory _joinableTaskFactory;

        private readonly CompanyValidator _companyValidator;
        private readonly CompanyLocationValidator _companyLocationValidator;
        private readonly CostCenterValidator _costCenterValidator;
        private readonly StorageValidator _storageValidator;

        #endregion

        #region Constructor

        public CostCenterMasterViewModel(
            CostCenterViewModel context,
            IRepository<CompanyGraphQLModel> companyService,
            IRepository<CompanyLocationGraphQLModel> companyLocationService,
            IRepository<CostCenterGraphQLModel> costCenterService,
            IRepository<StorageGraphQLModel> storageService,
            NetErp.Helpers.IDialogService dialogService,
            NetErp.Helpers.Services.INotificationService notificationService,
            CountryCache countryCache,
            StringLengthCache stringLengthCache,
            PermissionCache permissionCache,
            AuthorizationSequenceCache authorizationSequenceCache,
            CompanyCache companyCache,
            CompanyLocationCache companyLocationCache,
            CostCenterCache costCenterCache,
            StorageCache storageCache,
            IGraphQLClient graphQLClient,
            JoinableTaskFactory joinableTaskFactory,
            CompanyValidator companyValidator,
            CompanyLocationValidator companyLocationValidator,
            CostCenterValidator costCenterValidator,
            StorageValidator storageValidator)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            _companyService = companyService ?? throw new ArgumentNullException(nameof(companyService));
            _companyLocationService = companyLocationService ?? throw new ArgumentNullException(nameof(companyLocationService));
            _costCenterService = costCenterService ?? throw new ArgumentNullException(nameof(costCenterService));
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _countryCache = countryCache ?? throw new ArgumentNullException(nameof(countryCache));
            _stringLengthCache = stringLengthCache ?? throw new ArgumentNullException(nameof(stringLengthCache));
            _permissionCache = permissionCache ?? throw new ArgumentNullException(nameof(permissionCache));
            _authorizationSequenceCache = authorizationSequenceCache ?? throw new ArgumentNullException(nameof(authorizationSequenceCache));
            _companyCache = companyCache ?? throw new ArgumentNullException(nameof(companyCache));
            _companyLocationCache = companyLocationCache ?? throw new ArgumentNullException(nameof(companyLocationCache));
            _costCenterCache = costCenterCache ?? throw new ArgumentNullException(nameof(costCenterCache));
            _storageCache = storageCache ?? throw new ArgumentNullException(nameof(storageCache));
            _graphQLClient = graphQLClient ?? throw new ArgumentNullException(nameof(graphQLClient));
            _joinableTaskFactory = joinableTaskFactory ?? throw new ArgumentNullException(nameof(joinableTaskFactory));
            _companyValidator = companyValidator ?? throw new ArgumentNullException(nameof(companyValidator));
            _companyLocationValidator = companyLocationValidator ?? throw new ArgumentNullException(nameof(companyLocationValidator));
            _costCenterValidator = costCenterValidator ?? throw new ArgumentNullException(nameof(costCenterValidator));
            _storageValidator = storageValidator ?? throw new ArgumentNullException(nameof(storageValidator));

            Context.EventAggregator.SubscribeOnUIThread(this);
        }

        #endregion

        #region Properties — Tree

        public ObservableCollection<CompanyDTO> Companies
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Companies));
                }
            }
        } = [];

        public ReadOnlyObservableCollection<CountryGraphQLModel>? Countries
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Countries));
                }
            }
        }

        public ICostCentersItems? SelectedItem
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedItem));
                    HandleSelectedItemChanged();
                    NotifyOfPropertyChange(nameof(CanEditCompany));
                    NotifyOfPropertyChange(nameof(CanEditCompanyLocation));
                    NotifyOfPropertyChange(nameof(CanEditCostCenter));
                    NotifyOfPropertyChange(nameof(CanEditStorage));
                    NotifyOfPropertyChange(nameof(CanDeleteCompanyLocation));
                    NotifyOfPropertyChange(nameof(CanDeleteCostCenter));
                    NotifyOfPropertyChange(nameof(CanDeleteStorage));
                    NotifyOfPropertyChange(nameof(CanCreateCompanyLocation));
                    NotifyOfPropertyChange(nameof(CanCreateCostCenter));
                    NotifyOfPropertyChange(nameof(CanCreateStorage));
                }
            }
        }

        public int CompanyIdBeforeNewCompanyLocation
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(CompanyIdBeforeNewCompanyLocation));
                }
            }
        }

        public int CompanyLocationIdBeforeNewCostCenter { get; set; }
        public int CompanyLocationIdBeforeNewStorage { get; set; }

        public bool IsBusy
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IsBusy));
                    NotifyOfPropertyChange(nameof(CanEditCompany));
                    NotifyOfPropertyChange(nameof(CanEditCompanyLocation));
                    NotifyOfPropertyChange(nameof(CanEditCostCenter));
                    NotifyOfPropertyChange(nameof(CanEditStorage));
                    NotifyOfPropertyChange(nameof(CanCreateCompanyLocation));
                    NotifyOfPropertyChange(nameof(CanCreateCostCenter));
                    NotifyOfPropertyChange(nameof(CanCreateStorage));
                    NotifyOfPropertyChange(nameof(CanDeleteCompanyLocation));
                    NotifyOfPropertyChange(nameof(CanDeleteCostCenter));
                    NotifyOfPropertyChange(nameof(CanDeleteStorage));
                }
            }
        }

        private void HandleSelectedItemChanged()
        {
            // Capturar parent ids para los comandos "Nuevo *"
            switch (SelectedItem)
            {
                case CompanyDTO company:
                    CompanyIdBeforeNewCompanyLocation = company.Id;
                    break;
                case CostCenterDummyDTO costCenterDummy:
                    CompanyLocationIdBeforeNewCostCenter = costCenterDummy.Location.Id;
                    break;
                case StorageDummyDTO storageDummy:
                    CompanyLocationIdBeforeNewStorage = storageDummy.CompanyLocation.Id;
                    break;
            }
        }

        #endregion

        #region Properties — Permissions

        public bool HasCompanyEditPermission => _permissionCache.IsAllowed(PermissionCodes.Company.Edit);
        public bool HasCompanyLocationCreatePermission => _permissionCache.IsAllowed(PermissionCodes.CompanyLocation.Create);
        public bool HasCompanyLocationEditPermission => _permissionCache.IsAllowed(PermissionCodes.CompanyLocation.Edit);
        public bool HasCompanyLocationDeletePermission => _permissionCache.IsAllowed(PermissionCodes.CompanyLocation.Delete);
        public bool HasCostCenterCreatePermission => _permissionCache.IsAllowed(PermissionCodes.CostCenter.Create);
        public bool HasCostCenterEditPermission => _permissionCache.IsAllowed(PermissionCodes.CostCenter.Edit);
        public bool HasCostCenterDeletePermission => _permissionCache.IsAllowed(PermissionCodes.CostCenter.Delete);
        public bool HasStorageCreatePermission => _permissionCache.IsAllowed(PermissionCodes.Storage.Create);
        public bool HasStorageEditPermission => _permissionCache.IsAllowed(PermissionCodes.Storage.Edit);
        public bool HasStorageDeletePermission => _permissionCache.IsAllowed(PermissionCodes.Storage.Delete);

        #endregion

        #region Properties — Button States

        public bool CanEditCompany => HasCompanyEditPermission && SelectedItem is CompanyDTO && !IsBusy;
        public bool CanEditCompanyLocation => HasCompanyLocationEditPermission && SelectedItem is CompanyLocationDTO && !IsBusy;
        public bool CanEditCostCenter => HasCostCenterEditPermission && SelectedItem is CostCenterDTO && !IsBusy;
        public bool CanEditStorage => HasStorageEditPermission && SelectedItem is StorageDTO && !IsBusy;

        public bool CanCreateCompanyLocation => HasCompanyLocationCreatePermission && SelectedItem is CompanyDTO && !IsBusy;
        public bool CanCreateCostCenter => HasCostCenterCreatePermission && SelectedItem is CostCenterDummyDTO && !IsBusy;
        public bool CanCreateStorage => HasStorageCreatePermission && SelectedItem is StorageDummyDTO && !IsBusy;

        public bool CanDeleteCompanyLocation => HasCompanyLocationDeletePermission && SelectedItem is CompanyLocationDTO && !IsBusy;
        public bool CanDeleteCostCenter => HasCostCenterDeletePermission && SelectedItem is CostCenterDTO && !IsBusy;
        public bool CanDeleteStorage => HasStorageDeletePermission && SelectedItem is StorageDTO && !IsBusy;

        #endregion

        #region Commands

        private ICommand? _editCommand;
        public ICommand EditCommand => _editCommand ??= new AsyncCommand(EditAsync);

        private ICommand? _newCompanyLocationCommand;
        public ICommand NewCompanyLocationCommand => _newCompanyLocationCommand ??= new AsyncCommand(NewCompanyLocationAsync);

        private ICommand? _newCostCenterCommand;
        public ICommand NewCostCenterCommand => _newCostCenterCommand ??= new AsyncCommand(NewCostCenterAsync);

        private ICommand? _newStorageCommand;
        public ICommand NewStorageCommand => _newStorageCommand ??= new AsyncCommand(NewStorageAsync);

        private ICommand? _deleteCompanyLocationCommand;
        public ICommand DeleteCompanyLocationCommand => _deleteCompanyLocationCommand ??= new AsyncCommand(DeleteCompanyLocationAsync);

        private ICommand? _deleteCostCenterCommand;
        public ICommand DeleteCostCenterCommand => _deleteCostCenterCommand ??= new AsyncCommand(DeleteCostCenterAsync);

        private ICommand? _deleteStorageCommand;
        public ICommand DeleteStorageCommand => _deleteStorageCommand ??= new AsyncCommand(DeleteStorageAsync);

        #endregion

        #region Lifecycle

        protected override async void OnViewReady(object view)
        {
            base.OnViewReady(view);
            try
            {
                IsBusy = true;

                // PermissionCache ya fue pre-cargado por el Shell al seleccionar la empresa.
                // Todas las entidades del árbol (Company, CompanyLocation, CostCenter, Storage)
                // + lookups (Country, AuthorizationSequence) cargan en UNA sola HTTP request vía batch.
                await Task.WhenAll(
                    _stringLengthCache.EnsureEntitiesLoadedAsync(StringLengthEntities.CostCenters),
                    CacheBatchLoader.LoadAsync(_graphQLClient, default,
                        _countryCache, _authorizationSequenceCache,
                        _companyCache, _companyLocationCache, _costCenterCache, _storageCache));

                Application.Current.Dispatcher.Invoke(() =>
                {
                    NotifyAllPermissionStates();
                    Countries = _countryCache.Items;
                    BuildTree();
                });
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"Error al inicializar el módulo.\r\n{GetType().Name}.{nameof(OnViewReady)}: {ex.GetErrorMessage()}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
                await TryCloseAsync();
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Construye el árbol completo desde las caches. Todas las entidades ya están en memoria,
        /// así que esto es puro filtrado/mapeo sin red.
        /// </summary>
        private void BuildTree()
        {
            ObservableCollection<CompanyDTO> newCompanies = [];

            foreach (CompanyGraphQLModel companyModel in _companyCache.Items)
            {
                CompanyDTO companyDTO = Context.AutoMapper.Map<CompanyDTO>(companyModel);
                companyDTO.Context = this;
                companyDTO.Locations.Clear();

                foreach (CompanyLocationGraphQLModel locationModel in _companyLocationCache.Items
                    .Where(l => l.Company != null && l.Company.Id == companyModel.Id))
                {
                    CompanyLocationDTO locationDTO = Context.AutoMapper.Map<CompanyLocationDTO>(locationModel);
                    locationDTO.Context = this;
                    locationDTO.DummyItems.Clear();

                    CostCenterDummyDTO ccDummy = new(this, locationDTO);
                    foreach (CostCenterGraphQLModel ccModel in _costCenterCache.Items
                        .Where(c => c.CompanyLocation != null && c.CompanyLocation.Id == locationModel.Id))
                    {
                        ccDummy.CostCenters.Add(Context.AutoMapper.Map<CostCenterDTO>(ccModel));
                    }

                    StorageDummyDTO sDummy = new(this, locationDTO);
                    foreach (StorageGraphQLModel storageModel in _storageCache.Items
                        .Where(s => s.CompanyLocation != null && s.CompanyLocation.Id == locationModel.Id))
                    {
                        sDummy.Storages.Add(Context.AutoMapper.Map<StorageDTO>(storageModel));
                    }

                    locationDTO.DummyItems.Add(ccDummy);
                    locationDTO.DummyItems.Add(sDummy);
                    companyDTO.Locations.Add(locationDTO);
                }

                newCompanies.Add(companyDTO);
            }

            Companies = newCompanies;
        }

        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            if (close)
            {
                Context.EventAggregator.Unsubscribe(this);
                Companies?.Clear();
            }
            return base.OnDeactivateAsync(close, cancellationToken);
        }

        private void NotifyAllPermissionStates()
        {
            NotifyOfPropertyChange(nameof(HasCompanyEditPermission));
            NotifyOfPropertyChange(nameof(HasCompanyLocationCreatePermission));
            NotifyOfPropertyChange(nameof(HasCompanyLocationEditPermission));
            NotifyOfPropertyChange(nameof(HasCompanyLocationDeletePermission));
            NotifyOfPropertyChange(nameof(HasCostCenterCreatePermission));
            NotifyOfPropertyChange(nameof(HasCostCenterEditPermission));
            NotifyOfPropertyChange(nameof(HasCostCenterDeletePermission));
            NotifyOfPropertyChange(nameof(HasStorageCreatePermission));
            NotifyOfPropertyChange(nameof(HasStorageEditPermission));
            NotifyOfPropertyChange(nameof(HasStorageDeletePermission));
            NotifyOfPropertyChange(nameof(CanEditCompany));
            NotifyOfPropertyChange(nameof(CanEditCompanyLocation));
            NotifyOfPropertyChange(nameof(CanEditCostCenter));
            NotifyOfPropertyChange(nameof(CanEditStorage));
            NotifyOfPropertyChange(nameof(CanCreateCompanyLocation));
            NotifyOfPropertyChange(nameof(CanCreateCostCenter));
            NotifyOfPropertyChange(nameof(CanCreateStorage));
            NotifyOfPropertyChange(nameof(CanDeleteCompanyLocation));
            NotifyOfPropertyChange(nameof(CanDeleteCostCenter));
            NotifyOfPropertyChange(nameof(CanDeleteStorage));
        }

        #endregion

        #region Open Dialog Commands

        public async Task EditAsync()
        {
            if (SelectedItem is null) return;
            try
            {
                switch (SelectedItem)
                {
                    case CompanyDTO companyDto:
                        await OpenCompanyDialogAsync(companyDto);
                        break;
                    case CompanyLocationDTO locationDto:
                        await OpenCompanyLocationDialogAsync(locationDto, isNew: false);
                        break;
                    case CostCenterDTO costCenterDto:
                        await OpenCostCenterDialogAsync(costCenterDto, isNew: false);
                        break;
                    case StorageDTO storageDto:
                        await OpenStorageDialogAsync(storageDto, isNew: false);
                        break;
                }
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                    ThemedMessageBox.Show("Atención!",
                        $"{GetType().Name}.{nameof(EditAsync)}: {ex.GetErrorMessage()}",
                        MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        public async Task NewCompanyLocationAsync()
        {
            try { await OpenCompanyLocationDialogAsync(null, isNew: true); }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                    ThemedMessageBox.Show("Atención!",
                        $"{GetType().Name}.{nameof(NewCompanyLocationAsync)}: {ex.GetErrorMessage()}",
                        MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        public async Task NewCostCenterAsync()
        {
            try { await OpenCostCenterDialogAsync(null, isNew: true); }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                    ThemedMessageBox.Show("Atención!",
                        $"{GetType().Name}.{nameof(NewCostCenterAsync)}: {ex.GetErrorMessage()}",
                        MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        public async Task NewStorageAsync()
        {
            try { await OpenStorageDialogAsync(null, isNew: true); }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                    ThemedMessageBox.Show("Atención!",
                        $"{GetType().Name}.{nameof(NewStorageAsync)}: {ex.GetErrorMessage()}",
                        MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        private async Task OpenCompanyDialogAsync(CompanyDTO dto)
        {
            CompanyDetailViewModel detail = new(_companyService, Context.EventAggregator, _dialogService, _joinableTaskFactory, _companyValidator);
            CompanyGraphQLModel model = Context.AutoMapper.Map<CompanyGraphQLModel>(dto);
            detail.SetForEdit(model);
            ApplyDialogDimensions(detail);
            await _dialogService.ShowDialogAsync(detail, "Editar empresa");
        }

        private async Task OpenCompanyLocationDialogAsync(CompanyLocationDTO? dto, bool isNew)
        {
            CompanyLocationDetailViewModel detail = new(
                _companyLocationService, Context.EventAggregator, _stringLengthCache, _joinableTaskFactory, _companyLocationValidator);
            if (isNew)
                detail.SetForNew(CompanyIdBeforeNewCompanyLocation);
            else
            {
                CompanyLocationGraphQLModel model = Context.AutoMapper.Map<CompanyLocationGraphQLModel>(dto);
                detail.SetForEdit(model);
            }
            ApplyDialogDimensions(detail);
            await _dialogService.ShowDialogAsync(detail, isNew ? "Nueva sede" : "Editar sede");
        }

        private async Task OpenCostCenterDialogAsync(CostCenterDTO? dto, bool isNew)
        {
            CostCenterDetailViewModel detail = new(
                _costCenterService, Context.EventAggregator, _stringLengthCache, _authorizationSequenceCache, _joinableTaskFactory, _costCenterValidator);
            IEnumerable<CountryGraphQLModel> countries = (IEnumerable<CountryGraphQLModel>?)Countries ?? [];
            if (isNew)
                detail.SetForNew(CompanyLocationIdBeforeNewCostCenter, countries);
            else
            {
                CostCenterGraphQLModel model = Context.AutoMapper.Map<CostCenterGraphQLModel>(dto);
                detail.SetForEdit(model, countries);
            }
            ApplyDialogDimensions(detail);
            await _dialogService.ShowDialogAsync(detail, isNew ? "Nuevo centro de costo" : "Editar centro de costo");
        }

        private async Task OpenStorageDialogAsync(StorageDTO? dto, bool isNew)
        {
            StorageDetailViewModel detail = new(
                _storageService, Context.EventAggregator, _stringLengthCache, _joinableTaskFactory, _storageValidator);
            IEnumerable<CountryGraphQLModel> countries = (IEnumerable<CountryGraphQLModel>?)Countries ?? [];
            if (isNew)
                detail.SetForNew(CompanyLocationIdBeforeNewStorage, countries);
            else
            {
                StorageGraphQLModel model = Context.AutoMapper.Map<StorageGraphQLModel>(dto);
                detail.SetForEdit(model, countries);
            }
            ApplyDialogDimensions(detail);
            await _dialogService.ShowDialogAsync(detail, isNew ? "Nueva bodega" : "Editar bodega");
        }

        private void ApplyDialogDimensions(CostCentersDetailViewModelBase detail)
        {
            if (this.GetView() is FrameworkElement parentView)
            {
                detail.DialogWidth = Math.Min(parentView.ActualWidth * 0.6, detail.DialogWidth);
                detail.DialogHeight = Math.Min(parentView.ActualHeight * 0.95, detail.DialogHeight);
            }
        }

        #endregion

        #region Delete Commands

        public async Task DeleteCompanyLocationAsync()
        {
            if (SelectedItem is not CompanyLocationDTO companyLocation) return;
            try
            {
                IsBusy = true;

                (GraphQLQueryFragment canDeleteFragment, string canDeleteQuery) = _canDeleteCompanyLocationQuery.Value;
                object canDeleteVars = new GraphQLVariables().For(canDeleteFragment, "id", companyLocation.Id).Build();
                CanDeleteType validation = await _companyLocationService.CanDeleteAsync(canDeleteQuery, canDeleteVars);

                if (!validation.CanDelete)
                {
                    IsBusy = false;
                    ThemedMessageBox.Show("Atención!",
                        $"El registro no puede ser eliminado\r\n\r\n{validation.Message}",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                IsBusy = false;
                if (ThemedMessageBox.Show("Confirme...",
                    $"¿Confirma que desea eliminar la sede {companyLocation.Name}?",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

                IsBusy = true;
                (GraphQLQueryFragment deleteFragment, string deleteQuery) = _deleteCompanyLocationQuery.Value;
                object deleteVars = new GraphQLVariables().For(deleteFragment, "id", companyLocation.Id).Build();
                DeleteResponseType result = await _companyLocationService.DeleteAsync<DeleteResponseType>(deleteQuery, deleteVars);

                if (!result.Success)
                {
                    ThemedMessageBox.Show("Atención!",
                        $"No pudo ser eliminado el registro\r\n\r\n{result.Message}",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                SelectedItem = null;
                await Context.EventAggregator.PublishOnCurrentThreadAsync(
                    new CompanyLocationDeleteMessage { DeletedCompanyLocation = result },
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                    ThemedMessageBox.Show("Atención!",
                        $"{GetType().Name}.{nameof(DeleteCompanyLocationAsync)}: {ex.GetErrorMessage()}",
                        MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task DeleteCostCenterAsync()
        {
            if (SelectedItem is not CostCenterDTO costCenter) return;
            try
            {
                IsBusy = true;

                (GraphQLQueryFragment canDeleteFragment, string canDeleteQuery) = _canDeleteCostCenterQuery.Value;
                object canDeleteVars = new GraphQLVariables().For(canDeleteFragment, "id", costCenter.Id).Build();
                CanDeleteType validation = await _costCenterService.CanDeleteAsync(canDeleteQuery, canDeleteVars);

                if (!validation.CanDelete)
                {
                    IsBusy = false;
                    ThemedMessageBox.Show("Atención!",
                        $"El registro no puede ser eliminado\r\n\r\n{validation.Message}",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                IsBusy = false;
                if (ThemedMessageBox.Show("Confirme...",
                    $"¿Confirma que desea eliminar el centro de costo {costCenter.Name}?",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

                IsBusy = true;
                (GraphQLQueryFragment deleteFragment, string deleteQuery) = _deleteCostCenterQuery.Value;
                object deleteVars = new GraphQLVariables().For(deleteFragment, "id", costCenter.Id).Build();
                DeleteResponseType result = await _costCenterService.DeleteAsync<DeleteResponseType>(deleteQuery, deleteVars);

                if (!result.Success)
                {
                    ThemedMessageBox.Show("Atención!",
                        $"No pudo ser eliminado el registro\r\n\r\n{result.Message}",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                SelectedItem = null;
                await Context.EventAggregator.PublishOnCurrentThreadAsync(
                    new CostCenterDeleteMessage { DeletedCostCenter = result },
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                    ThemedMessageBox.Show("Atención!",
                        $"{GetType().Name}.{nameof(DeleteCostCenterAsync)}: {ex.GetErrorMessage()}",
                        MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task DeleteStorageAsync()
        {
            if (SelectedItem is not StorageDTO storage) return;
            try
            {
                IsBusy = true;

                (GraphQLQueryFragment canDeleteFragment, string canDeleteQuery) = _canDeleteStorageQuery.Value;
                object canDeleteVars = new GraphQLVariables().For(canDeleteFragment, "id", storage.Id).Build();
                CanDeleteType validation = await _storageService.CanDeleteAsync(canDeleteQuery, canDeleteVars);

                if (!validation.CanDelete)
                {
                    IsBusy = false;
                    ThemedMessageBox.Show("Atención!",
                        $"El registro no puede ser eliminado\r\n\r\n{validation.Message}",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                IsBusy = false;
                if (ThemedMessageBox.Show("Confirme...",
                    $"¿Confirma que desea eliminar la bodega {storage.Name}?",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

                IsBusy = true;
                (GraphQLQueryFragment deleteFragment, string deleteQuery) = _deleteStorageQuery.Value;
                object deleteVars = new GraphQLVariables().For(deleteFragment, "id", storage.Id).Build();
                DeleteResponseType result = await _storageService.DeleteAsync<DeleteResponseType>(deleteQuery, deleteVars);

                if (!result.Success)
                {
                    ThemedMessageBox.Show("Atención!",
                        $"No pudo ser eliminado el registro\r\n\r\n{result.Message}",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                SelectedItem = null;
                await Context.EventAggregator.PublishOnCurrentThreadAsync(
                    new StorageDeleteMessage { DeletedStorage = result },
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                    ThemedMessageBox.Show("Atención!",
                        $"{GetType().Name}.{nameof(DeleteStorageAsync)}: {ex.GetErrorMessage()}",
                        MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region Tree Refresh Handlers

        public Task HandleAsync(CostCenterCreateMessage message, CancellationToken cancellationToken)
        {
            CostCenterDTO costCenterDTO = Context.AutoMapper.Map<CostCenterDTO>(message.CreatedCostCenter.Entity);
            CompanyDTO? companyDTO = Companies.FirstOrDefault(c => c.Id == costCenterDTO.CompanyLocation.Company.Id);
            if (companyDTO is null) return Task.CompletedTask;
            CompanyLocationDTO? companyLocationDTO = companyDTO.Locations.FirstOrDefault(l => l.Id == costCenterDTO.CompanyLocation.Id);
            if (companyLocationDTO is null) return Task.CompletedTask;
            CostCenterDummyDTO? costCenterDummyDTO = companyLocationDTO.DummyItems.FirstOrDefault(d => d is CostCenterDummyDTO) as CostCenterDummyDTO;
            if (costCenterDummyDTO is null) return Task.CompletedTask;

            if (!costCenterDummyDTO.IsExpanded) costCenterDummyDTO.IsExpanded = true;
            costCenterDummyDTO.CostCenters.Add(costCenterDTO);
            SelectedItem = costCenterDTO;
            _notificationService.ShowSuccess(message.CreatedCostCenter.Message);
            return Task.CompletedTask;
        }

        public Task HandleAsync(CostCenterUpdateMessage message, CancellationToken cancellationToken)
        {
            CostCenterDTO costCenterDTO = Context.AutoMapper.Map<CostCenterDTO>(message.UpdatedCostCenter.Entity);
            CompanyDTO? companyDTO = Companies.FirstOrDefault(c => c.Id == costCenterDTO.CompanyLocation.Company.Id);
            if (companyDTO is null) return Task.CompletedTask;
            CompanyLocationDTO? companyLocationDTO = companyDTO.Locations.FirstOrDefault(l => l.Id == costCenterDTO.CompanyLocation.Id);
            if (companyLocationDTO is null) return Task.CompletedTask;
            CostCenterDummyDTO? costCenterDummyDTO = companyLocationDTO.DummyItems.FirstOrDefault(d => d is CostCenterDummyDTO) as CostCenterDummyDTO;
            if (costCenterDummyDTO is null) return Task.CompletedTask;
            CostCenterDTO? costCenterToUpdate = costCenterDummyDTO.CostCenters.FirstOrDefault(c => c.Id == costCenterDTO.Id);
            if (costCenterToUpdate is null) return Task.CompletedTask;

            costCenterToUpdate.Id = costCenterDTO.Id;
            costCenterToUpdate.Name = costCenterDTO.Name;
            costCenterToUpdate.TradeName = costCenterDTO.TradeName;
            costCenterToUpdate.ShortName = costCenterDTO.ShortName;
            costCenterToUpdate.Status = costCenterDTO.Status;
            costCenterToUpdate.Address = costCenterDTO.Address;
            costCenterToUpdate.PrimaryPhone = costCenterDTO.PrimaryPhone;
            costCenterToUpdate.SecondaryPhone = costCenterDTO.SecondaryPhone;
            costCenterToUpdate.PrimaryCellPhone = costCenterDTO.PrimaryCellPhone;
            costCenterToUpdate.SecondaryCellPhone = costCenterDTO.SecondaryCellPhone;
            costCenterToUpdate.DateControlType = costCenterDTO.DateControlType;
            costCenterToUpdate.ShowChangeWindowOnCash = costCenterDTO.ShowChangeWindowOnCash;
            costCenterToUpdate.AllowBuy = costCenterDTO.AllowBuy;
            costCenterToUpdate.AllowSell = costCenterDTO.AllowSell;
            costCenterToUpdate.IsTaxable = costCenterDTO.IsTaxable;
            costCenterToUpdate.PriceListIncludeTax = costCenterDTO.PriceListIncludeTax;
            costCenterToUpdate.InvoicePriceIncludeTax = costCenterDTO.InvoicePriceIncludeTax;
            costCenterToUpdate.AllowRepeatItemsOnSales = costCenterDTO.AllowRepeatItemsOnSales;
            costCenterToUpdate.InvoiceCopiesToPrint = costCenterDTO.InvoiceCopiesToPrint;
            costCenterToUpdate.RequiresConfirmationToPrintCopies = costCenterDTO.RequiresConfirmationToPrintCopies;
            costCenterToUpdate.TaxToCost = costCenterDTO.TaxToCost;
            costCenterToUpdate.DefaultInvoiceObservation = costCenterDTO.DefaultInvoiceObservation;
            costCenterToUpdate.InvoiceFooter = costCenterDTO.InvoiceFooter;
            costCenterToUpdate.RemissionFooter = costCenterDTO.RemissionFooter;
            costCenterToUpdate.Country = costCenterDTO.Country;
            costCenterToUpdate.Department = costCenterDTO.Department;
            costCenterToUpdate.City = costCenterDTO.City;
            costCenterToUpdate.CompanyLocation = costCenterDTO.CompanyLocation;
            costCenterToUpdate.FeCreditDefaultAuthorizationSequence = costCenterDTO.FeCreditDefaultAuthorizationSequence;
            costCenterToUpdate.FeCashDefaultAuthorizationSequence = costCenterDTO.FeCashDefaultAuthorizationSequence;
            costCenterToUpdate.PeDefaultAuthorizationSequence = costCenterDTO.PeDefaultAuthorizationSequence;
            costCenterToUpdate.DsDefaultAuthorizationSequence = costCenterDTO.DsDefaultAuthorizationSequence;
            _notificationService.ShowSuccess(message.UpdatedCostCenter.Message);
            return Task.CompletedTask;
        }

        public Task HandleAsync(CostCenterDeleteMessage message, CancellationToken cancellationToken)
        {
            foreach (CompanyDTO company in Companies)
            {
                foreach (CompanyLocationDTO location in company.Locations)
                {
                    CostCenterDummyDTO? costCenterDummy = location.DummyItems.FirstOrDefault(d => d is CostCenterDummyDTO) as CostCenterDummyDTO;
                    if (costCenterDummy is null) continue;
                    CostCenterDTO? toRemove = costCenterDummy.CostCenters.FirstOrDefault(cc => cc.Id == message.DeletedCostCenter.DeletedId);
                    if (toRemove is not null)
                    {
                        costCenterDummy.CostCenters.Remove(toRemove);
                        _notificationService.ShowSuccess(message.DeletedCostCenter.Message);
                        return Task.CompletedTask;
                    }
                }
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(StorageCreateMessage message, CancellationToken cancellationToken)
        {
            StorageDTO storageDTO = Context.AutoMapper.Map<StorageDTO>(message.CreatedStorage.Entity);
            CompanyDTO? companyDTO = Companies.FirstOrDefault(c => c.Id == storageDTO.CompanyLocation.Company.Id);
            if (companyDTO is null) return Task.CompletedTask;
            CompanyLocationDTO? companyLocationDTO = companyDTO.Locations.FirstOrDefault(l => l.Id == storageDTO.CompanyLocation.Id);
            if (companyLocationDTO is null) return Task.CompletedTask;
            StorageDummyDTO? storageDummyDTO = companyLocationDTO.DummyItems.FirstOrDefault(d => d is StorageDummyDTO) as StorageDummyDTO;
            if (storageDummyDTO is null) return Task.CompletedTask;

            if (!storageDummyDTO.IsExpanded) storageDummyDTO.IsExpanded = true;
            storageDummyDTO.Storages.Add(storageDTO);
            SelectedItem = storageDTO;
            _notificationService.ShowSuccess(message.CreatedStorage.Message);
            return Task.CompletedTask;
        }

        public Task HandleAsync(StorageUpdateMessage message, CancellationToken cancellationToken)
        {
            StorageDTO storageDTO = Context.AutoMapper.Map<StorageDTO>(message.UpdatedStorage.Entity);
            CompanyDTO? companyDTO = Companies.FirstOrDefault(c => c.Id == storageDTO.CompanyLocation.Company.Id);
            if (companyDTO is null) return Task.CompletedTask;
            CompanyLocationDTO? companyLocationDTO = companyDTO.Locations.FirstOrDefault(l => l.Id == storageDTO.CompanyLocation.Id);
            if (companyLocationDTO is null) return Task.CompletedTask;
            StorageDummyDTO? storageDummyDTO = companyLocationDTO.DummyItems.FirstOrDefault(d => d is StorageDummyDTO) as StorageDummyDTO;
            if (storageDummyDTO is null) return Task.CompletedTask;
            StorageDTO? storageToUpdate = storageDummyDTO.Storages.FirstOrDefault(s => s.Id == storageDTO.Id);
            if (storageToUpdate is null) return Task.CompletedTask;

            storageToUpdate.Id = storageDTO.Id;
            storageToUpdate.Name = storageDTO.Name;
            storageToUpdate.Address = storageDTO.Address;
            storageToUpdate.Status = storageDTO.Status;
            storageToUpdate.City = storageDTO.City;
            storageToUpdate.CompanyLocation = storageDTO.CompanyLocation;
            _notificationService.ShowSuccess(message.UpdatedStorage.Message);
            return Task.CompletedTask;
        }

        public Task HandleAsync(StorageDeleteMessage message, CancellationToken cancellationToken)
        {
            foreach (CompanyDTO company in Companies)
            {
                foreach (CompanyLocationDTO location in company.Locations)
                {
                    StorageDummyDTO? storageDummy = location.DummyItems.FirstOrDefault(d => d is StorageDummyDTO) as StorageDummyDTO;
                    if (storageDummy is null) continue;
                    StorageDTO? toRemove = storageDummy.Storages.FirstOrDefault(s => s.Id == message.DeletedStorage.DeletedId);
                    if (toRemove is not null)
                    {
                        storageDummy.Storages.Remove(toRemove);
                        _notificationService.ShowSuccess(message.DeletedStorage.Message);
                        return Task.CompletedTask;
                    }
                }
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(CompanyLocationCreateMessage message, CancellationToken cancellationToken)
        {
            CompanyLocationDTO companyLocationDTO = Context.AutoMapper.Map<CompanyLocationDTO>(message.CreatedCompanyLocation.Entity);
            CompanyDTO? companyDTO = Companies.FirstOrDefault(c => c.Id == companyLocationDTO.Company.Id);
            if (companyDTO is null) return Task.CompletedTask;

            if (!companyDTO.IsExpanded) companyDTO.IsExpanded = true;
            companyLocationDTO.Context = this;
            companyLocationDTO.DummyItems.Add(new CostCenterDummyDTO(this, companyLocationDTO));
            companyLocationDTO.DummyItems.Add(new StorageDummyDTO(this, companyLocationDTO));
            companyDTO.Locations.Add(companyLocationDTO);
            SelectedItem = companyLocationDTO;
            _notificationService.ShowSuccess(message.CreatedCompanyLocation.Message);
            return Task.CompletedTask;
        }

        public Task HandleAsync(CompanyLocationUpdateMessage message, CancellationToken cancellationToken)
        {
            CompanyLocationDTO companyLocationDTO = Context.AutoMapper.Map<CompanyLocationDTO>(message.UpdatedCompanyLocation.Entity);
            CompanyDTO? companyDTO = Companies.FirstOrDefault(c => c.Id == companyLocationDTO.Company.Id);
            if (companyDTO is null) return Task.CompletedTask;
            CompanyLocationDTO? toUpdate = companyDTO.Locations.FirstOrDefault(l => l.Id == companyLocationDTO.Id);
            if (toUpdate is null) return Task.CompletedTask;
            toUpdate.Id = companyLocationDTO.Id;
            toUpdate.Name = companyLocationDTO.Name;
            toUpdate.Company = companyLocationDTO.Company;
            _notificationService.ShowSuccess(message.UpdatedCompanyLocation.Message);
            return Task.CompletedTask;
        }

        public Task HandleAsync(CompanyLocationDeleteMessage message, CancellationToken cancellationToken)
        {
            foreach (CompanyDTO company in Companies)
            {
                CompanyLocationDTO? toRemove = company.Locations.FirstOrDefault(l => l.Id == message.DeletedCompanyLocation.DeletedId);
                if (toRemove is not null)
                {
                    company.Locations.Remove(toRemove);
                    _notificationService.ShowSuccess(message.DeletedCompanyLocation.Message);
                    return Task.CompletedTask;
                }
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(CompanyUpdateMessage message, CancellationToken cancellationToken)
        {
            CompanyDTO companyDTO = Context.AutoMapper.Map<CompanyDTO>(message.UpdatedCompany.Entity);
            CompanyDTO? toUpdate = Companies.FirstOrDefault(c => c.Id == companyDTO.Id);
            if (toUpdate is null) return Task.CompletedTask;
            toUpdate.Id = companyDTO.Id;
            toUpdate.CompanyEntity = companyDTO.CompanyEntity;
            _notificationService.ShowSuccess(message.UpdatedCompany.Message);
            return Task.CompletedTask;
        }

        public Task HandleAsync(PermissionsCacheRefreshedMessage message, CancellationToken cancellationToken)
        {
            NotifyAllPermissionStates();
            return Task.CompletedTask;
        }

        #endregion

        #region GraphQL Queries

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _canDeleteCompanyLocationQuery = new(() =>
        {
            Dictionary<string, object> fields = FieldSpec<CanDeleteType>
                .Create().Field(f => f.CanDelete).Field(f => f.Message).Build();
            GraphQLQueryFragment fragment = new("canDeleteCompanyLocation",
                [new("id", "ID!")], fields, "CanDeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _deleteCompanyLocationQuery = new(() =>
        {
            Dictionary<string, object> fields = FieldSpec<DeleteResponseType>
                .Create().Field(f => f.DeletedId).Field(f => f.Message).Field(f => f.Success).Build();
            GraphQLQueryFragment fragment = new("deleteCompanyLocation",
                [new("id", "ID!")], fields, "DeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _canDeleteCostCenterQuery = new(() =>
        {
            Dictionary<string, object> fields = FieldSpec<CanDeleteType>
                .Create().Field(f => f.CanDelete).Field(f => f.Message).Build();
            GraphQLQueryFragment fragment = new("canDeleteCostCenter",
                [new("id", "ID!")], fields, "CanDeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _deleteCostCenterQuery = new(() =>
        {
            Dictionary<string, object> fields = FieldSpec<DeleteResponseType>
                .Create().Field(f => f.DeletedId).Field(f => f.Message).Field(f => f.Success).Build();
            GraphQLQueryFragment fragment = new("deleteCostCenter",
                [new("id", "ID!")], fields, "DeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _canDeleteStorageQuery = new(() =>
        {
            Dictionary<string, object> fields = FieldSpec<CanDeleteType>
                .Create().Field(f => f.CanDelete).Field(f => f.Message).Build();
            GraphQLQueryFragment fragment = new("canDeleteStorage",
                [new("id", "ID!")], fields, "CanDeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _deleteStorageQuery = new(() =>
        {
            Dictionary<string, object> fields = FieldSpec<DeleteResponseType>
                .Create().Field(f => f.DeletedId).Field(f => f.Message).Field(f => f.Success).Build();
            GraphQLQueryFragment fragment = new("deleteStorage",
                [new("id", "ID!")], fields, "DeleteResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        #endregion
    }
}
