using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Microsoft.VisualStudio.Threading;
using Models.Global;
using Models.Treasury;
using NetErp.Helpers;
using NetErp.Helpers.Cache;
using NetErp.Helpers.GraphQLQueryBuilder;
using NetErp.Helpers.Services;
using NetErp.Treasury.Masters.DTO;
using NetErp.Treasury.Masters.Validators;
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using static Models.Global.GraphQLResponseTypes;
using IDialogService = NetErp.Helpers.IDialogService;
using INotificationService = NetErp.Helpers.Services.INotificationService;

namespace NetErp.Treasury.Masters.ViewModels
{
    public class TreasuryRootMasterViewModel : Screen,
        IHandle<TreasuryCashDrawerCreateMessage>,
        IHandle<TreasuryCashDrawerDeleteMessage>,
        IHandle<TreasuryCashDrawerUpdateMessage>,
        IHandle<BankCreateMessage>,
        IHandle<BankUpdateMessage>,
        IHandle<BankDeleteMessage>,
        IHandle<BankAccountCreateMessage>,
        IHandle<BankAccountDeleteMessage>,
        IHandle<BankAccountUpdateMessage>,
        IHandle<FranchiseCreateMessage>,
        IHandle<FranchiseDeleteMessage>,
        IHandle<FranchiseUpdateMessage>
    {
        public TreasuryRootViewModel Context { get; }

        private readonly IRepository<CashDrawerGraphQLModel> _cashDrawerService;
        private readonly IRepository<BankGraphQLModel> _bankService;
        private readonly IRepository<BankAccountGraphQLModel> _bankAccountService;
        private readonly IRepository<FranchiseGraphQLModel> _franchiseService;
        private readonly IDialogService _dialogService;
        private readonly INotificationService _notificationService;
        private readonly IGraphQLClient _graphQLClient;
        private readonly AuxiliaryAccountingAccountCache _auxiliaryAccountingAccountCache;
        private readonly CompanyLocationCache _companyLocationCache;
        private readonly CostCenterCache _costCenterCache;
        private readonly BankAccountCache _bankAccountCache;
        private readonly MajorCashDrawerCache _majorCashDrawerCache;
        private readonly MinorCashDrawerCache _minorCashDrawerCache;
        private readonly AuxiliaryCashDrawerCache _auxiliaryCashDrawerCache;
        private readonly BankCache _bankCache;
        private readonly FranchiseCache _franchiseCache;
        private readonly StringLengthCache _stringLengthCache;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly BankValidator _bankValidator;
        private readonly BankAccountValidator _bankAccountValidator;
        private readonly FranchiseValidator _franchiseValidator;
        private readonly MajorCashDrawerValidator _majorCashDrawerValidator;
        private readonly MinorCashDrawerValidator _minorCashDrawerValidator;
        private readonly AuxiliaryCashDrawerValidator _auxiliaryCashDrawerValidator;

        public ObservableCollection<object> DummyItems
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(DummyItems));
                }
            }
        } = [];

        public ITreasuryTreeMasterSelectedItem? SelectedItem
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedItem));
                }
            }
        }

        public bool IsBusy
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IsBusy));
                }
            }
        }

        public string ResponseTime
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
        } = string.Empty;

        #region Create Commands (tree context menus)

        public ICommand CreateMajorCashDrawerCommand =>
            field ??= new AsyncCommand(CreateMajorCashDrawerAsync);

        public async Task CreateMajorCashDrawerAsync()
        {
            if (SelectedItem is not CashDrawerCostCenterDTO costCenterDTO || costCenterDTO.Type != CashDrawerType.Major)
            {
                _notificationService.ShowInfo("Seleccione un centro de costo de caja general antes de crear una caja general.");
                return;
            }

            CostCenterGraphQLModel? costCenter = _costCenterCache.Items.FirstOrDefault(c => c.Id == costCenterDTO.Id);
            if (costCenter is null)
            {
                _notificationService.ShowInfo("No se pudo ubicar el centro de costo seleccionado.");
                return;
            }

            await OpenDialogAsync(() =>
            {
                MajorCashDrawerDetailViewModel detail = new(_cashDrawerService, Context.EventAggregator,
                    _auxiliaryAccountingAccountCache, _majorCashDrawerCache, _stringLengthCache,
                    _joinableTaskFactory, _majorCashDrawerValidator);
                detail.SetForNew(costCenter);
                ApplyDialogDimensions(detail, 650, 550);
                return detail;
            }, "Nueva caja general");
        }

        public ICommand CreateMinorCashDrawerCommand =>
            field ??= new AsyncCommand(CreateMinorCashDrawerAsync);

        public async Task CreateMinorCashDrawerAsync()
        {
            if (SelectedItem is not CashDrawerCostCenterDTO costCenterDTO || costCenterDTO.Type != CashDrawerType.Minor)
            {
                _notificationService.ShowInfo("Seleccione un centro de costo de caja menor antes de crear una caja menor.");
                return;
            }

            CostCenterGraphQLModel? costCenter = _costCenterCache.Items.FirstOrDefault(c => c.Id == costCenterDTO.Id);
            if (costCenter is null)
            {
                _notificationService.ShowInfo("No se pudo ubicar el centro de costo seleccionado.");
                return;
            }

            await OpenDialogAsync(() =>
            {
                MinorCashDrawerDetailViewModel detail = new(_cashDrawerService, Context.EventAggregator,
                    _auxiliaryAccountingAccountCache, _stringLengthCache, _joinableTaskFactory, _minorCashDrawerValidator);
                detail.SetForNew(costCenter);
                ApplyDialogDimensions(detail, 550, 400);
                return detail;
            }, "Nueva caja menor");
        }

        public ICommand CreateAuxiliaryCashDrawerCommand =>
            field ??= new AsyncCommand(CreateAuxiliaryCashDrawerAsync);

        public async Task CreateAuxiliaryCashDrawerAsync()
        {
            if (SelectedItem is not MajorCashDrawerMasterTreeDTO majorDTO)
            {
                _notificationService.ShowInfo("Seleccione una caja general antes de crear una caja auxiliar.");
                return;
            }

            CashDrawerGraphQLModel? major = _majorCashDrawerCache.Items.FirstOrDefault(c => c.Id == majorDTO.Id);
            if (major is null)
            {
                _notificationService.ShowInfo("No se pudo ubicar la caja general seleccionada.");
                return;
            }

            await OpenDialogAsync(() =>
            {
                AuxiliaryCashDrawerDetailViewModel detail = new(_cashDrawerService, Context.EventAggregator,
                    _auxiliaryAccountingAccountCache, _auxiliaryCashDrawerCache, _stringLengthCache,
                    _joinableTaskFactory, _auxiliaryCashDrawerValidator);
                detail.SetForNew(major);
                ApplyDialogDimensions(detail, 650, 600);
                return detail;
            }, "Nueva caja auxiliar");
        }

        public ICommand CreateBankCommand =>
            field ??= new AsyncCommand(CreateBankAsync);

        public async Task CreateBankAsync()
        {
            await OpenDialogAsync(() =>
            {
                BankDetailViewModel detail = new(_bankService, Context.EventAggregator, _dialogService,
                    _stringLengthCache, _joinableTaskFactory, _bankValidator);
                detail.SetForNew();
                ApplyDialogDimensions(detail, 500, 350);
                return detail;
            }, "Nuevo banco");
        }

        public ICommand CreateBankAccountCommand =>
            field ??= new AsyncCommand(CreateBankAccountAsync);

        public async Task CreateBankAccountAsync()
        {
            if (SelectedItem is not TreasuryBankMasterTreeDTO bankDTO)
            {
                _notificationService.ShowInfo("Seleccione un banco antes de crear una cuenta bancaria.");
                return;
            }

            BankGraphQLModel? bank = _bankCache.Items.FirstOrDefault(b => b.Id == bankDTO.Id);
            if (bank is null)
            {
                _notificationService.ShowInfo("No se pudo ubicar el banco seleccionado.");
                return;
            }

            await OpenDialogAsync(() =>
            {
                BankAccountDetailViewModel detail = new(_bankAccountService, Context.EventAggregator,
                    _auxiliaryAccountingAccountCache, _stringLengthCache, _joinableTaskFactory, _bankAccountValidator);
                detail.SetForNew(bank);
                ApplyDialogDimensions(detail, 650, 550);
                return detail;
            }, "Nueva cuenta bancaria");
        }

        public ICommand CreateFranchiseCommand =>
            field ??= new AsyncCommand(CreateFranchiseAsync);

        public async Task CreateFranchiseAsync()
        {
            await OpenDialogAsync(() =>
            {
                FranchiseDetailViewModel detail = new(_franchiseService, Context.EventAggregator,
                    _bankAccountCache, _auxiliaryAccountingAccountCache, _stringLengthCache,
                    _joinableTaskFactory, _franchiseValidator);
                detail.SetForNew();
                if (this.GetView() is FrameworkElement parentView)
                {
                    detail.DialogWidth = parentView.ActualWidth * 0.80;
                    detail.DialogHeight = parentView.ActualHeight * 0.90;
                }
                return detail;
            }, "Nueva franquicia");
        }

        #endregion

        #region Edit Command

        public ICommand EditCommand => field ??= new AsyncCommand(EditAsync);

        public async Task EditAsync()
        {
            if (SelectedItem is null) return;

            switch (SelectedItem)
            {
                case TreasuryBankMasterTreeDTO bankDTO:
                    await EditBankAsync(bankDTO);
                    break;
                case TreasuryBankAccountMasterTreeDTO bankAccountDTO:
                    await EditBankAccountAsync(bankAccountDTO);
                    break;
                case TreasuryFranchiseMasterTreeDTO franchiseDTO:
                    await EditFranchiseAsync(franchiseDTO);
                    break;
                case MajorCashDrawerMasterTreeDTO majorDTO:
                    await EditMajorCashDrawerAsync(majorDTO);
                    break;
                case MinorCashDrawerMasterTreeDTO minorDTO:
                    await EditMinorCashDrawerAsync(minorDTO);
                    break;
                case TreasuryAuxiliaryCashDrawerMasterTreeDTO auxDTO:
                    await EditAuxiliaryCashDrawerAsync(auxDTO);
                    break;
            }
        }

        private async Task EditBankAsync(TreasuryBankMasterTreeDTO dto)
        {
            BankGraphQLModel? bank = _bankCache.Items.FirstOrDefault(b => b.Id == dto.Id);
            if (bank is null)
            {
                _notificationService.ShowInfo("No se pudo ubicar el banco seleccionado.");
                return;
            }

            await OpenDialogAsync(() =>
            {
                BankDetailViewModel detail = new(_bankService, Context.EventAggregator, _dialogService,
                    _stringLengthCache, _joinableTaskFactory, _bankValidator);
                detail.SetForEdit(bank);
                ApplyDialogDimensions(detail, 500, 350);
                return detail;
            }, "Editar banco");
        }

        private async Task EditBankAccountAsync(TreasuryBankAccountMasterTreeDTO dto)
        {
            BankAccountGraphQLModel? bankAccount = _bankAccountCache.Items.FirstOrDefault(b => b.Id == dto.Id);
            if (bankAccount is null)
            {
                _notificationService.ShowInfo("No se pudo ubicar la cuenta bancaria seleccionada.");
                return;
            }

            await OpenDialogAsync(() =>
            {
                BankAccountDetailViewModel detail = new(_bankAccountService, Context.EventAggregator,
                    _auxiliaryAccountingAccountCache, _stringLengthCache, _joinableTaskFactory, _bankAccountValidator);
                detail.SetForEdit(bankAccount);
                ApplyDialogDimensions(detail, 650, 550);
                return detail;
            }, "Editar cuenta bancaria");
        }

        private async Task EditFranchiseAsync(TreasuryFranchiseMasterTreeDTO dto)
        {
            FranchiseGraphQLModel? franchise = _franchiseCache.Items.FirstOrDefault(f => f.Id == dto.Id);
            if (franchise is null)
            {
                _notificationService.ShowInfo("No se pudo ubicar la franquicia seleccionada.");
                return;
            }

            await OpenDialogAsync(() =>
            {
                FranchiseDetailViewModel detail = new(_franchiseService, Context.EventAggregator,
                    _bankAccountCache, _auxiliaryAccountingAccountCache, _stringLengthCache,
                    _joinableTaskFactory, _franchiseValidator);
                detail.SetForEdit(franchise);
                if (this.GetView() is FrameworkElement parentView)
                {
                    detail.DialogWidth = parentView.ActualWidth * 0.80;
                    detail.DialogHeight = parentView.ActualHeight * 0.90;
                }
                return detail;
            }, "Editar franquicia");
        }

        private async Task EditMajorCashDrawerAsync(MajorCashDrawerMasterTreeDTO dto)
        {
            CashDrawerGraphQLModel? cashDrawer = _majorCashDrawerCache.Items.FirstOrDefault(c => c.Id == dto.Id);
            if (cashDrawer is null)
            {
                _notificationService.ShowInfo("No se pudo ubicar la caja general seleccionada.");
                return;
            }

            await OpenDialogAsync(() =>
            {
                MajorCashDrawerDetailViewModel detail = new(_cashDrawerService, Context.EventAggregator,
                    _auxiliaryAccountingAccountCache, _majorCashDrawerCache, _stringLengthCache,
                    _joinableTaskFactory, _majorCashDrawerValidator);
                detail.SetForEdit(cashDrawer);
                ApplyDialogDimensions(detail, 650, 550);
                return detail;
            }, "Editar caja general");
        }

        private async Task EditMinorCashDrawerAsync(MinorCashDrawerMasterTreeDTO dto)
        {
            CashDrawerGraphQLModel? cashDrawer = _minorCashDrawerCache.Items.FirstOrDefault(c => c.Id == dto.Id);
            if (cashDrawer is null)
            {
                _notificationService.ShowInfo("No se pudo ubicar la caja menor seleccionada.");
                return;
            }

            await OpenDialogAsync(() =>
            {
                MinorCashDrawerDetailViewModel detail = new(_cashDrawerService, Context.EventAggregator,
                    _auxiliaryAccountingAccountCache, _stringLengthCache, _joinableTaskFactory, _minorCashDrawerValidator);
                detail.SetForEdit(cashDrawer);
                ApplyDialogDimensions(detail, 550, 400);
                return detail;
            }, "Editar caja menor");
        }

        private async Task EditAuxiliaryCashDrawerAsync(TreasuryAuxiliaryCashDrawerMasterTreeDTO dto)
        {
            CashDrawerGraphQLModel? cashDrawer = _auxiliaryCashDrawerCache.Items.FirstOrDefault(c => c.Id == dto.Id);
            if (cashDrawer is null)
            {
                _notificationService.ShowInfo("No se pudo ubicar la caja auxiliar seleccionada.");
                return;
            }

            await OpenDialogAsync(() =>
            {
                AuxiliaryCashDrawerDetailViewModel detail = new(_cashDrawerService, Context.EventAggregator,
                    _auxiliaryAccountingAccountCache, _auxiliaryCashDrawerCache, _stringLengthCache,
                    _joinableTaskFactory, _auxiliaryCashDrawerValidator);
                detail.SetForEdit(cashDrawer);
                ApplyDialogDimensions(detail, 650, 600);
                return detail;
            }, "Editar caja auxiliar");
        }

        #endregion

        #region Dialog helpers

        /// <summary>
        /// Aplica dimensiones al modal escalando contra la vista padre cuando es posible.
        /// </summary>
        private void ApplyDialogDimensions<T>(T detail, double defaultWidth, double defaultHeight)
            where T : Screen
        {
            dynamic dynDetail = detail!;
            dynDetail.DialogWidth = defaultWidth;
            dynDetail.DialogHeight = defaultHeight;
            if (this.GetView() is FrameworkElement parent)
            {
                dynDetail.DialogWidth = Math.Min(parent.ActualWidth * 0.7, defaultWidth);
                dynDetail.DialogHeight = Math.Min(parent.ActualHeight * 0.9, defaultHeight);
            }
        }

        /// <summary>
        /// Abre un modal tras un breve estado IsBusy que cubre la construcción síncrona
        /// del VM. Se libera IsBusy ANTES de await ShowDialogAsync para que el overlay
        /// no permanezca visible detrás del diálogo. Setup puede retornar null para abortar.
        /// </summary>
        private async Task OpenDialogAsync<T>(Func<T?> setup, string title) where T : Screen
        {
            T? detail = null;
            try
            {
                IsBusy = true;
                await Dispatcher.Yield(DispatcherPriority.Background);
                detail = setup();
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{title}: {ex.GetErrorMessage()}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            finally
            {
                IsBusy = false;
            }

            if (detail is null) return;

            try
            {
                await _dialogService.ShowDialogAsync(detail, title);
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{title}: {ex.GetErrorMessage()}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Delete Commands

        private async Task DeleteEntityAsync<TModel>(
            string displayName,
            int id,
            string canDeleteFragmentName,
            string deleteFragmentName,
            IRepository<TModel> service,
            Func<DeleteResponseType, Task> publishMessage)
        {
            try
            {
                IsBusy = true;

                string canDeleteQuery = BuildCanDeleteQuery(canDeleteFragmentName);
                object canDeleteVariables = new { canDeleteResponseId = id };
                CanDeleteType validation = await service.CanDeleteAsync(canDeleteQuery, canDeleteVariables);

                if (validation.CanDelete)
                {
                    IsBusy = false;
                    MessageBoxResult result = ThemedMessageBox.Show(
                        title: "Confirme...",
                        text: $"¿Confirma que desea eliminar el registro {displayName}?",
                        messageBoxButtons: MessageBoxButton.YesNo,
                        image: MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes) return;
                }
                else
                {
                    IsBusy = false;
                    await _joinableTaskFactory.SwitchToMainThreadAsync();
                    ThemedMessageBox.Show(
                        title: "Atención!",
                        text: $"El registro no puede ser eliminado\n\n{validation.Message}",
                        messageBoxButtons: MessageBoxButton.OK,
                        image: MessageBoxImage.Error);
                    return;
                }

                IsBusy = true;

                DeleteResponseType deleteResult = await ExecuteDeleteAsync(service, deleteFragmentName, id);

                if (!deleteResult.Success)
                {
                    ThemedMessageBox.Show(
                        title: "Atención!",
                        text: $"No se pudo eliminar el registro.\n\n{deleteResult.Message}",
                        messageBoxButtons: MessageBoxButton.OK,
                        image: MessageBoxImage.Error);
                    return;
                }

                await publishMessage(deleteResult);
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"{GetType().Name}.DeleteEntityAsync \r\n{ex.GetErrorMessage()}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private static readonly ConcurrentDictionary<string, Lazy<string>> _canDeleteQueryCache = new();

        private static string BuildCanDeleteQuery(string fragmentName)
        {
            return _canDeleteQueryCache.GetOrAdd(fragmentName, name => new Lazy<string>(() =>
            {
                FieldSpec<CanDeleteType> fields = FieldSpec<CanDeleteType>.Create()
                    .Field(f => f.CanDelete)
                    .Field(f => f.Message);

                GraphQLQueryParameter parameter = new("id", "ID!");
                GraphQLQueryFragment fragment = new(name, [parameter], fields.Build(), "CanDeleteResponse");
                return new GraphQLQueryBuilder([fragment]).GetQuery();
            })).Value;
        }

        private static readonly ConcurrentDictionary<string, Lazy<string>> _deleteMutationQueryCache = new();

        private static string BuildDeleteMutationQuery(string fragmentName)
        {
            return _deleteMutationQueryCache.GetOrAdd(fragmentName, name => new Lazy<string>(() =>
            {
                FieldSpec<DeleteResponseType> fields = FieldSpec<DeleteResponseType>.Create()
                    .Field(f => f.DeletedId)
                    .Field(f => f.Message)
                    .Field(f => f.Success);

                GraphQLQueryParameter parameter = new("id", "ID!");
                GraphQLQueryFragment fragment = new(name, [parameter], fields.Build(), "DeleteResponse");
                return new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION);
            })).Value;
        }

        private async Task<DeleteResponseType> ExecuteDeleteAsync<TModel>(
            IRepository<TModel> service, string deleteFragmentName, int id)
        {
            string query = BuildDeleteMutationQuery(deleteFragmentName);
            object variables = new { deleteResponseId = id };
            DeleteResponseType result = await service.DeleteAsync<DeleteResponseType>(query, variables);
            SelectedItem = null;
            return result;
        }

        public ICommand DeleteMajorCashDrawerCommand =>
            field ??= new AsyncCommand(DeleteMajorCashDrawer);

        public async Task DeleteMajorCashDrawer()
        {
            if (SelectedItem is not MajorCashDrawerMasterTreeDTO selected) return;
            await DeleteEntityAsync(selected.Name, selected.Id,
                "canDeleteCashDrawer", "deleteCashDrawer", _cashDrawerService,
                async result => await Context.EventAggregator.PublishOnUIThreadAsync(
                    new TreasuryCashDrawerDeleteMessage { DeletedCashDrawer = result }));
        }

        public ICommand DeleteMinorCashDrawerCommand =>
            field ??= new AsyncCommand(DeleteMinorCashDrawer);

        public async Task DeleteMinorCashDrawer()
        {
            if (SelectedItem is not MinorCashDrawerMasterTreeDTO selected) return;
            await DeleteEntityAsync(selected.Name, selected.Id,
                "canDeleteCashDrawer", "deleteCashDrawer", _cashDrawerService,
                async result => await Context.EventAggregator.PublishOnUIThreadAsync(
                    new TreasuryCashDrawerDeleteMessage { DeletedCashDrawer = result }));
        }

        public ICommand DeleteAuxiliaryCashDrawerCommand =>
            field ??= new AsyncCommand(DeleteAuxiliaryCashDrawerAsync);

        public async Task DeleteAuxiliaryCashDrawerAsync()
        {
            if (SelectedItem is not TreasuryAuxiliaryCashDrawerMasterTreeDTO selected) return;
            await DeleteEntityAsync(selected.Name, selected.Id,
                "canDeleteCashDrawer", "deleteCashDrawer", _cashDrawerService,
                async result => await Context.EventAggregator.PublishOnUIThreadAsync(
                    new TreasuryCashDrawerDeleteMessage { DeletedCashDrawer = result }));
        }

        public ICommand DeleteBankCommand => field ??= new AsyncCommand(DeleteBankAsync);

        public async Task DeleteBankAsync()
        {
            if (SelectedItem is not TreasuryBankMasterTreeDTO selected) return;
            await DeleteEntityAsync(selected.AccountingEntity.SearchName, selected.Id,
                "canDeleteBank", "deleteBank", _bankService,
                async result => await Context.EventAggregator.PublishOnUIThreadAsync(
                    new BankDeleteMessage { DeletedBank = result }));
        }

        public ICommand DeleteBankAccountCommand =>
            field ??= new AsyncCommand(DeleteBankAccountAsync);

        public async Task DeleteBankAccountAsync()
        {
            if (SelectedItem is not TreasuryBankAccountMasterTreeDTO selected) return;
            await DeleteEntityAsync(selected.Description, selected.Id,
                "canDeleteBankAccount", "deleteBankAccount", _bankAccountService,
                async result => await Context.EventAggregator.PublishOnUIThreadAsync(
                    new BankAccountDeleteMessage { DeletedBankAccount = result }));
        }

        public ICommand DeleteFranchiseCommand =>
            field ??= new AsyncCommand(DeleteFranchiseAsync);

        public async Task DeleteFranchiseAsync()
        {
            if (SelectedItem is not TreasuryFranchiseMasterTreeDTO selected) return;
            await DeleteEntityAsync(selected.Name, selected.Id,
                "canDeleteFranchise", "deleteFranchise", _franchiseService,
                async result => await Context.EventAggregator.PublishOnUIThreadAsync(
                    new FranchiseDeleteMessage { DeletedFranchise = result }));
        }

        #endregion

        #region Tree hydration

        /// <summary>
        /// Construye el árbol completo desde los caches ya hidratados.
        /// Siguiendo el patrón de CostCenterMasterViewModel.BuildTree: arma toda la jerarquía
        /// en una ObservableCollection local y la asigna al final como nueva colección raíz
        /// (via NotifyOfPropertyChange). Esto garantiza que el TreeView re-evalúe toda la
        /// estructura — mutar las sub-colecciones in-place no funciona porque DevExpress
        /// TreeViewControl cachea la estructura jerárquica de los nodos bindeados al inicio.
        /// Debe ejecutarse en el hilo de UI.
        /// </summary>
        private void BuildTreeFromCaches()
        {
            
            #pragma warning disable VSTHRD001
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Raíz 1: Caja General (mayor)
                CashDrawerDummyDTO majorDummy = new() { Id = 1, Name = "CAJA GENERAL", Type = CashDrawerType.Major, Context = this };
                foreach (CompanyLocationGraphQLModel loc in _companyLocationCache.Items)
                {
                    CashDrawerCompanyLocationDTO locationDTO = Context.AutoMapper.Map<CashDrawerCompanyLocationDTO>(loc);
                    locationDTO.Context = this;
                    locationDTO.DummyParent = majorDummy;
                    locationDTO.Type = CashDrawerType.Major;

                    foreach (CostCenterGraphQLModel cc in _costCenterCache.Items.Where(c => c.CompanyLocation != null && c.CompanyLocation.Id == loc.Id))
                    {
                        CashDrawerCostCenterDTO costCenterDTO = Context.AutoMapper.Map<CashDrawerCostCenterDTO>(cc);
                        costCenterDTO.Context = this;
                        costCenterDTO.Location = locationDTO;
                        costCenterDTO.Type = CashDrawerType.Major;

                        foreach (CashDrawerGraphQLModel major in _majorCashDrawerCache.Items.Where(m =>
                            m.CostCenter != null && m.CostCenter.Id == cc.Id && !m.IsPettyCash && m.Parent == null))
                        {
                            MajorCashDrawerMasterTreeDTO majorDTO = Context.AutoMapper.Map<MajorCashDrawerMasterTreeDTO>(major);
                            majorDTO.Context = this;

                            foreach (CashDrawerGraphQLModel aux in _auxiliaryCashDrawerCache.Items.Where(a =>
                                a.Parent != null && a.Parent.Id == major.Id))
                            {
                                TreasuryAuxiliaryCashDrawerMasterTreeDTO auxDTO = Context.AutoMapper.Map<TreasuryAuxiliaryCashDrawerMasterTreeDTO>(aux);
                                auxDTO.Context = this;
                                majorDTO.AuxiliaryCashDrawers.Add(auxDTO);
                            }
                            costCenterDTO.CashDrawers.Add(majorDTO);
                        }
                        locationDTO.CostCenters.Add(costCenterDTO);
                    }
                    majorDummy.Locations.Add(locationDTO);
                }
                #pragma warning restore VSTHRD001

                // Raíz 2: Caja Menor
                CashDrawerDummyDTO minorDummy = new() { Id = 2, Name = "CAJA MENOR", Type = CashDrawerType.Minor, Context = this };
                foreach (CompanyLocationGraphQLModel loc in _companyLocationCache.Items)
                {
                    CashDrawerCompanyLocationDTO locationDTO = Context.AutoMapper.Map<CashDrawerCompanyLocationDTO>(loc);
                    locationDTO.Context = this;
                    locationDTO.DummyParent = minorDummy;
                    locationDTO.Type = CashDrawerType.Minor;

                    foreach (CostCenterGraphQLModel cc in _costCenterCache.Items.Where(c => c.CompanyLocation != null && c.CompanyLocation.Id == loc.Id))
                    {
                        CashDrawerCostCenterDTO costCenterDTO = Context.AutoMapper.Map<CashDrawerCostCenterDTO>(cc);
                        costCenterDTO.Context = this;
                        costCenterDTO.Location = locationDTO;
                        costCenterDTO.Type = CashDrawerType.Minor;

                        foreach (CashDrawerGraphQLModel minor in _minorCashDrawerCache.Items.Where(m =>
                            m.CostCenter != null && m.CostCenter.Id == cc.Id && m.IsPettyCash && m.Parent == null))
                        {
                            MinorCashDrawerMasterTreeDTO minorDTO = Context.AutoMapper.Map<MinorCashDrawerMasterTreeDTO>(minor);
                            minorDTO.Context = this;
                            costCenterDTO.CashDrawers.Add(minorDTO);
                        }
                        locationDTO.CostCenters.Add(costCenterDTO);
                    }
                    minorDummy.Locations.Add(locationDTO);
                }

                // Raíz 3: Bancos
                BankDummyDTO bankDummy = new() { Id = 3, Name = "BANCOS", Context = this };
                foreach (BankGraphQLModel bank in _bankCache.Items)
                {
                    TreasuryBankMasterTreeDTO bankDTO = Context.AutoMapper.Map<TreasuryBankMasterTreeDTO>(bank);
                    bankDTO.Context = this;
                    bankDTO.DummyParent = bankDummy;

                    foreach (BankAccountGraphQLModel ba in _bankAccountCache.Items.Where(x => x.Bank != null && x.Bank.Id == bank.Id))
                    {
                        TreasuryBankAccountMasterTreeDTO bankAccountDTO = Context.AutoMapper.Map<TreasuryBankAccountMasterTreeDTO>(ba);
                        bankAccountDTO.Context = this;
                        bankDTO.BankAccounts.Add(bankAccountDTO);
                    }
                    bankDummy.Banks.Add(bankDTO);
                }

                // Raíz 4: Franquicias
                FranchiseDummyDTO franchiseDummy = new() { Id = 4, Name = "FRANQUICIAS", Context = this };
                foreach (FranchiseGraphQLModel franchise in _franchiseCache.Items)
                {
                    TreasuryFranchiseMasterTreeDTO franchiseDTO = Context.AutoMapper.Map<TreasuryFranchiseMasterTreeDTO>(franchise);
                    franchiseDTO.Context = this;
                    franchiseDTO.DummyParent = franchiseDummy;
                    franchiseDummy.Franchises.Add(franchiseDTO);
                }

                // Reasigna la raíz como una nueva ObservableCollection para forzar re-bind del tree.
                DummyItems = [majorDummy, minorDummy, bankDummy, franchiseDummy];
            });
        }

        #endregion

        #region Constructor and lifecycle

        public TreasuryRootMasterViewModel(
            TreasuryRootViewModel context,
            IRepository<CashDrawerGraphQLModel> cashDrawerService,
            IRepository<BankGraphQLModel> bankService,
            IRepository<BankAccountGraphQLModel> bankAccountService,
            IRepository<FranchiseGraphQLModel> franchiseService,
            IDialogService dialogService,
            INotificationService notificationService,
            AuxiliaryAccountingAccountCache auxiliaryAccountingAccountCache,
            CompanyLocationCache companyLocationCache,
            CostCenterCache costCenterCache,
            BankAccountCache bankAccountCache,
            MajorCashDrawerCache majorCashDrawerCache,
            MinorCashDrawerCache minorCashDrawerCache,
            AuxiliaryCashDrawerCache auxiliaryCashDrawerCache,
            BankCache bankCache,
            FranchiseCache franchiseCache,
            IGraphQLClient graphQLClient,
            StringLengthCache stringLengthCache,
            JoinableTaskFactory joinableTaskFactory,
            BankValidator bankValidator,
            BankAccountValidator bankAccountValidator,
            FranchiseValidator franchiseValidator,
            MajorCashDrawerValidator majorCashDrawerValidator,
            MinorCashDrawerValidator minorCashDrawerValidator,
            AuxiliaryCashDrawerValidator auxiliaryCashDrawerValidator)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            _cashDrawerService = cashDrawerService ?? throw new ArgumentNullException(nameof(cashDrawerService));
            _bankService = bankService ?? throw new ArgumentNullException(nameof(bankService));
            _bankAccountService = bankAccountService ?? throw new ArgumentNullException(nameof(bankAccountService));
            _franchiseService = franchiseService ?? throw new ArgumentNullException(nameof(franchiseService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _auxiliaryAccountingAccountCache = auxiliaryAccountingAccountCache ?? throw new ArgumentNullException(nameof(auxiliaryAccountingAccountCache));
            _companyLocationCache = companyLocationCache ?? throw new ArgumentNullException(nameof(companyLocationCache));
            _costCenterCache = costCenterCache ?? throw new ArgumentNullException(nameof(costCenterCache));
            _bankAccountCache = bankAccountCache ?? throw new ArgumentNullException(nameof(bankAccountCache));
            _majorCashDrawerCache = majorCashDrawerCache ?? throw new ArgumentNullException(nameof(majorCashDrawerCache));
            _minorCashDrawerCache = minorCashDrawerCache ?? throw new ArgumentNullException(nameof(minorCashDrawerCache));
            _auxiliaryCashDrawerCache = auxiliaryCashDrawerCache ?? throw new ArgumentNullException(nameof(auxiliaryCashDrawerCache));
            _bankCache = bankCache ?? throw new ArgumentNullException(nameof(bankCache));
            _franchiseCache = franchiseCache ?? throw new ArgumentNullException(nameof(franchiseCache));
            _graphQLClient = graphQLClient ?? throw new ArgumentNullException(nameof(graphQLClient));
            _stringLengthCache = stringLengthCache ?? throw new ArgumentNullException(nameof(stringLengthCache));
            _joinableTaskFactory = joinableTaskFactory ?? throw new ArgumentNullException(nameof(joinableTaskFactory));
            _bankValidator = bankValidator ?? throw new ArgumentNullException(nameof(bankValidator));
            _bankAccountValidator = bankAccountValidator ?? throw new ArgumentNullException(nameof(bankAccountValidator));
            _franchiseValidator = franchiseValidator ?? throw new ArgumentNullException(nameof(franchiseValidator));
            _majorCashDrawerValidator = majorCashDrawerValidator ?? throw new ArgumentNullException(nameof(majorCashDrawerValidator));
            _minorCashDrawerValidator = minorCashDrawerValidator ?? throw new ArgumentNullException(nameof(minorCashDrawerValidator));
            _auxiliaryCashDrawerValidator = auxiliaryCashDrawerValidator ?? throw new ArgumentNullException(nameof(auxiliaryCashDrawerValidator));

            // Siembra los 4 root nodes con colecciones vacías para que el TreeView tenga algo
            // que mostrar aunque OnViewReady falle. BuildTreeFromCaches reasigna DummyItems
            // cuando termina de cargar, lo que dispara NotifyOfPropertyChange y re-bindea la
            // jerarquía completa en el TreeView.
            DummyItems = [
                new CashDrawerDummyDTO() { Id = 1, Name = "CAJA GENERAL", Type = CashDrawerType.Major, Context = this },
                new CashDrawerDummyDTO() { Id = 2, Name = "CAJA MENOR", Type = CashDrawerType.Minor, Context = this },
                new BankDummyDTO() { Id = 3, Name = "BANCOS", Context = this },
                new FranchiseDummyDTO() { Id = 4, Name = "FRANQUICIAS", Context = this }
            ];
            Context.EventAggregator.SubscribeOnUIThread(this);
        }

        protected override async void OnViewReady(object view)
        {
            base.OnViewReady(view);
            try
            {
                IsBusy = true;
                Stopwatch stopwatch = Stopwatch.StartNew();

                await Task.WhenAll(
                    _stringLengthCache.EnsureEntitiesLoadedAsync(StringLengthEntities.Treasury),
                    CacheBatchLoader.LoadAsync(
                        _graphQLClient, default,
                        _auxiliaryAccountingAccountCache,
                        _companyLocationCache,
                        _costCenterCache,
                        _bankAccountCache,
                        _majorCashDrawerCache,
                        _minorCashDrawerCache,
                        _auxiliaryCashDrawerCache,
                        _bankCache,
                        _franchiseCache));

                BuildTreeFromCaches();

                stopwatch.Stop();
                ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"Error al inicializar el módulo.\r\n{GetType().Name}.{nameof(OnViewReady)}: {ex.GetErrorMessage()}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
                await Context.TryCloseAsync();
            }
            finally
            {
                IsBusy = false;
            }
        }

        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            if (close)
            {
                Context.EventAggregator.Unsubscribe(this);
            }
            return base.OnDeactivateAsync(close, cancellationToken);
        }

        #endregion

        #region Event Handlers (Create/Update/Delete)

        public Task HandleAsync(TreasuryCashDrawerCreateMessage message, CancellationToken cancellationToken)
        {
            CashDrawerGraphQLModel createdCashDrawer = message.CreatedCashDrawer.Entity;

            // Caja general (major)
            if (!createdCashDrawer.IsPettyCash && createdCashDrawer.Parent is null)
            {
                MajorCashDrawerMasterTreeDTO majorDTO = Context.AutoMapper.Map<MajorCashDrawerMasterTreeDTO>(createdCashDrawer);
                majorDTO.Context = this;
                ITreasuryTreeMasterSelectedItem? inserted = null;
                
                #pragma warning disable VSTHRD001
                Application.Current.Dispatcher.Invoke(() =>
                {
                    CashDrawerDummyDTO? majorDummy = DummyItems.OfType<CashDrawerDummyDTO>().FirstOrDefault(x => x.Type == CashDrawerType.Major);
                    if (majorDummy is null) return;
                    CashDrawerCompanyLocationDTO? location = majorDummy.Locations.FirstOrDefault(x => x.Id == createdCashDrawer.CostCenter.CompanyLocation.Id);
                    if (location is null) return;
                    CashDrawerCostCenterDTO? costCenter = location.CostCenters.FirstOrDefault(x => x.Id == createdCashDrawer.CostCenter.Id);
                    if (costCenter is null) return;
                    costCenter.CashDrawers.Add(majorDTO);
                    majorDummy.IsExpanded = true;
                    location.IsExpanded = true;
                    costCenter.IsExpanded = true;
                    inserted = majorDTO;
                });
                #pragma warning restore VSTHRD001

                if (inserted != null) SelectedItem = inserted;
                _notificationService.ShowSuccess(message.CreatedCashDrawer.Message);
                return Task.CompletedTask;
            }

            // Caja auxiliar
            if (!createdCashDrawer.IsPettyCash && createdCashDrawer.Parent != null)
            {
                TreasuryAuxiliaryCashDrawerMasterTreeDTO auxDTO = Context.AutoMapper.Map<TreasuryAuxiliaryCashDrawerMasterTreeDTO>(createdCashDrawer);
                auxDTO.Context = this;
                ITreasuryTreeMasterSelectedItem? inserted = null;
                
                #pragma warning disable VSTHRD001
                Application.Current.Dispatcher.Invoke(() =>
                {
                    CashDrawerDummyDTO? majorDummy = DummyItems.OfType<CashDrawerDummyDTO>().FirstOrDefault(x => x.Type == CashDrawerType.Major);
                    if (majorDummy is null) return;
                    CashDrawerCompanyLocationDTO? location = majorDummy.Locations.FirstOrDefault(x => x.Id == createdCashDrawer.Parent.CostCenter.CompanyLocation.Id);
                    if (location is null) return;
                    CashDrawerCostCenterDTO? costCenter = location.CostCenters.FirstOrDefault(x => x.Id == createdCashDrawer.Parent.CostCenter.Id);
                    if (costCenter is null) return;
                    MajorCashDrawerMasterTreeDTO? parent = costCenter.CashDrawers.OfType<MajorCashDrawerMasterTreeDTO>().FirstOrDefault(x => x.Id == createdCashDrawer.Parent.Id);
                    if (parent is null) return;
                    parent.AuxiliaryCashDrawers.Add(auxDTO);
                    majorDummy.IsExpanded = true;
                    location.IsExpanded = true;
                    costCenter.IsExpanded = true;
                    parent.IsExpanded = true;
                    inserted = auxDTO;
                });
                #pragma warning restore VSTHRD001

                if (inserted != null) SelectedItem = inserted;
                _notificationService.ShowSuccess(message.CreatedCashDrawer.Message);
                return Task.CompletedTask;
            }

            // Caja menor
            MinorCashDrawerMasterTreeDTO minorDTO = Context.AutoMapper.Map<MinorCashDrawerMasterTreeDTO>(createdCashDrawer);
            minorDTO.Context = this;
            ITreasuryTreeMasterSelectedItem? insertedMinor = null;
            
            #pragma warning disable VSTHRD001
            Application.Current.Dispatcher.Invoke(() =>
            {
                CashDrawerDummyDTO? minorDummy = DummyItems.OfType<CashDrawerDummyDTO>().FirstOrDefault(x => x.Type == CashDrawerType.Minor);
                if (minorDummy is null) return;
                CashDrawerCompanyLocationDTO? location = minorDummy.Locations.FirstOrDefault(x => x.Id == createdCashDrawer.CostCenter.CompanyLocation.Id);
                if (location is null) return;
                CashDrawerCostCenterDTO? costCenter = location.CostCenters.FirstOrDefault(x => x.Id == createdCashDrawer.CostCenter.Id);
                if (costCenter is null) return;
                costCenter.CashDrawers.Add(minorDTO);
                minorDummy.IsExpanded = true;
                location.IsExpanded = true;
                costCenter.IsExpanded = true;
                insertedMinor = minorDTO;
            });
            #pragma warning restore VSTHRD001

            if (insertedMinor != null) SelectedItem = insertedMinor;
            _notificationService.ShowSuccess(message.CreatedCashDrawer.Message);
            return Task.CompletedTask;
        }

        public Task HandleAsync(TreasuryCashDrawerDeleteMessage message, CancellationToken cancellationToken)
        {
            #pragma warning disable VSTHRD001
            Application.Current.Dispatcher.Invoke(() =>
            {
                int? deletedId = message.DeletedCashDrawer.DeletedId;

                CashDrawerDummyDTO? majorDummy = DummyItems.OfType<CashDrawerDummyDTO>().FirstOrDefault(x => x.Type == CashDrawerType.Major);
                if (majorDummy != null)
                {
                    foreach (CashDrawerCompanyLocationDTO location in majorDummy.Locations)
                    {
                        foreach (CashDrawerCostCenterDTO costCenter in location.CostCenters)
                        {
                            CashDrawerMasterTreeDTO? majorCashDrawer = costCenter.CashDrawers.FirstOrDefault(x => x.Id == deletedId);
                            if (majorCashDrawer != null)
                            {
                                costCenter.CashDrawers.Remove(majorCashDrawer);
                                SelectedItem = null;
                                return;
                            }

                            foreach (MajorCashDrawerMasterTreeDTO major in costCenter.CashDrawers.OfType<MajorCashDrawerMasterTreeDTO>())
                            {
                                TreasuryAuxiliaryCashDrawerMasterTreeDTO? auxiliary = major.AuxiliaryCashDrawers.FirstOrDefault(x => x.Id == deletedId);
                                if (auxiliary != null)
                                {
                                    major.AuxiliaryCashDrawers.Remove(auxiliary);
                                    SelectedItem = null;
                                    return;
                                }
                            }
                        }
                    }
                }
                #pragma warning restore VSTHRD001

                CashDrawerDummyDTO? minorDummy = DummyItems.OfType<CashDrawerDummyDTO>().FirstOrDefault(x => x.Type == CashDrawerType.Minor);
                if (minorDummy != null)
                {
                    foreach (CashDrawerCompanyLocationDTO location in minorDummy.Locations)
                    {
                        foreach (CashDrawerCostCenterDTO costCenter in location.CostCenters)
                        {
                            CashDrawerMasterTreeDTO? minorCashDrawer = costCenter.CashDrawers.FirstOrDefault(x => x.Id == deletedId);
                            if (minorCashDrawer != null)
                            {
                                costCenter.CashDrawers.Remove(minorCashDrawer);
                                SelectedItem = null;
                                return;
                            }
                        }
                    }
                }
            });
            _notificationService.ShowSuccess(message.DeletedCashDrawer.Message);
            return Task.CompletedTask;
        }

        public Task HandleAsync(TreasuryCashDrawerUpdateMessage message, CancellationToken cancellationToken)
        {
            CashDrawerGraphQLModel updatedCashDrawer = message.UpdatedCashDrawer.Entity;

            #pragma warning disable VSTHRD001
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Caja general (major)
                if (!updatedCashDrawer.IsPettyCash && updatedCashDrawer.Parent is null)
                {
                    MajorCashDrawerMasterTreeDTO cashDrawerDTO = Context.AutoMapper.Map<MajorCashDrawerMasterTreeDTO>(updatedCashDrawer);
                    CashDrawerDummyDTO? majorDummy = DummyItems.OfType<CashDrawerDummyDTO>().FirstOrDefault(x => x.Type == CashDrawerType.Major);
                    if (majorDummy is null) return;
                    CashDrawerCompanyLocationDTO? location = majorDummy.Locations.FirstOrDefault(x => x.Id == updatedCashDrawer.CostCenter.CompanyLocation.Id);
                    if (location is null) return;
                    CashDrawerCostCenterDTO? costCenter = location.CostCenters.FirstOrDefault(x => x.Id == updatedCashDrawer.CostCenter.Id);
                    if (costCenter is null) return;
                    MajorCashDrawerMasterTreeDTO? cashDrawerToUpdate = costCenter.CashDrawers.OfType<MajorCashDrawerMasterTreeDTO>().FirstOrDefault(x => x.Id == updatedCashDrawer.Id);
                    if (cashDrawerToUpdate is null) return;
                    cashDrawerToUpdate.Id = cashDrawerDTO.Id;
                    cashDrawerToUpdate.Name = cashDrawerDTO.Name;
                    cashDrawerToUpdate.CashAccountingAccount = cashDrawerDTO.CashAccountingAccount;
                    cashDrawerToUpdate.CheckAccountingAccount = cashDrawerDTO.CheckAccountingAccount;
                    cashDrawerToUpdate.CardAccountingAccount = cashDrawerDTO.CardAccountingAccount;
                    cashDrawerToUpdate.CashReviewRequired = cashDrawerDTO.CashReviewRequired;
                    cashDrawerToUpdate.AutoAdjustBalance = cashDrawerDTO.AutoAdjustBalance;
                    cashDrawerToUpdate.AutoTransfer = cashDrawerDTO.AutoTransfer;
                    cashDrawerToUpdate.AutoTransferCashDrawer = cashDrawerDTO.AutoTransferCashDrawer;
                    return;
                }
                #pragma warning restore VSTHRD001

                // Caja auxiliar
                if (!updatedCashDrawer.IsPettyCash && updatedCashDrawer.Parent != null)
                {
                    TreasuryAuxiliaryCashDrawerMasterTreeDTO auxiliaryCashDrawer = Context.AutoMapper.Map<TreasuryAuxiliaryCashDrawerMasterTreeDTO>(updatedCashDrawer);
                    CashDrawerDummyDTO? majorDummy = DummyItems.OfType<CashDrawerDummyDTO>().FirstOrDefault(x => x.Type == CashDrawerType.Major);
                    if (majorDummy is null) return;
                    CashDrawerCompanyLocationDTO? location = majorDummy.Locations.FirstOrDefault(x => x.Id == updatedCashDrawer.Parent.CostCenter.CompanyLocation.Id);
                    if (location is null) return;
                    CashDrawerCostCenterDTO? costCenter = location.CostCenters.FirstOrDefault(x => x.Id == updatedCashDrawer.Parent.CostCenter.Id);
                    if (costCenter is null) return;
                    MajorCashDrawerMasterTreeDTO? majorCashDrawer = costCenter.CashDrawers.OfType<MajorCashDrawerMasterTreeDTO>().FirstOrDefault(x => x.Id == updatedCashDrawer.Parent.Id);
                    if (majorCashDrawer is null) return;
                    TreasuryAuxiliaryCashDrawerMasterTreeDTO? auxiliaryCashDrawerToUpdate = majorCashDrawer.AuxiliaryCashDrawers.FirstOrDefault(x => x.Id == updatedCashDrawer.Id);
                    if (auxiliaryCashDrawerToUpdate is null) return;
                    auxiliaryCashDrawerToUpdate.Id = auxiliaryCashDrawer.Id;
                    auxiliaryCashDrawerToUpdate.Name = auxiliaryCashDrawer.Name;
                    auxiliaryCashDrawerToUpdate.CashAccountingAccount = auxiliaryCashDrawer.CashAccountingAccount;
                    auxiliaryCashDrawerToUpdate.CheckAccountingAccount = auxiliaryCashDrawer.CheckAccountingAccount;
                    auxiliaryCashDrawerToUpdate.CardAccountingAccount = auxiliaryCashDrawer.CardAccountingAccount;
                    auxiliaryCashDrawerToUpdate.CashReviewRequired = auxiliaryCashDrawer.CashReviewRequired;
                    auxiliaryCashDrawerToUpdate.AutoAdjustBalance = auxiliaryCashDrawer.AutoAdjustBalance;
                    auxiliaryCashDrawerToUpdate.AutoTransfer = auxiliaryCashDrawer.AutoTransfer;
                    auxiliaryCashDrawerToUpdate.AutoTransferCashDrawer = auxiliaryCashDrawer.AutoTransferCashDrawer;
                    auxiliaryCashDrawerToUpdate.ComputerName = auxiliaryCashDrawer.ComputerName;
                    return;
                }

                // Caja menor
                MinorCashDrawerMasterTreeDTO minorCashDrawerMasterTreeDTO = Context.AutoMapper.Map<MinorCashDrawerMasterTreeDTO>(updatedCashDrawer);
                CashDrawerDummyDTO? minorDummy = DummyItems.OfType<CashDrawerDummyDTO>().FirstOrDefault(x => x.Type == CashDrawerType.Minor);
                if (minorDummy is null) return;
                CashDrawerCompanyLocationDTO? minorLocation = minorDummy.Locations.FirstOrDefault(x => x.Id == updatedCashDrawer.CostCenter.CompanyLocation.Id);
                if (minorLocation is null) return;
                CashDrawerCostCenterDTO? minorCostCenter = minorLocation.CostCenters.FirstOrDefault(x => x.Id == updatedCashDrawer.CostCenter.Id);
                if (minorCostCenter is null) return;
                MinorCashDrawerMasterTreeDTO? minorCashDrawerToUpdate = minorCostCenter.CashDrawers.OfType<MinorCashDrawerMasterTreeDTO>().FirstOrDefault(x => x.Id == updatedCashDrawer.Id);
                if (minorCashDrawerToUpdate is null) return;
                minorCashDrawerToUpdate.Id = minorCashDrawerMasterTreeDTO.Id;
                minorCashDrawerToUpdate.Name = minorCashDrawerMasterTreeDTO.Name;
                minorCashDrawerToUpdate.CashAccountingAccount = minorCashDrawerMasterTreeDTO.CashAccountingAccount;
                minorCashDrawerToUpdate.CashReviewRequired = minorCashDrawerMasterTreeDTO.CashReviewRequired;
                minorCashDrawerToUpdate.AutoAdjustBalance = minorCashDrawerMasterTreeDTO.AutoAdjustBalance;
            });
            _notificationService.ShowSuccess(message.UpdatedCashDrawer.Message);
            return Task.CompletedTask;
        }

        public Task HandleAsync(BankCreateMessage message, CancellationToken cancellationToken)
        {
            BankGraphQLModel createdBank = message.CreatedBank.Entity;

            TreasuryBankMasterTreeDTO bankDTO = Context.AutoMapper.Map<TreasuryBankMasterTreeDTO>(createdBank);
            bankDTO.Context = this;
            ITreasuryTreeMasterSelectedItem? inserted = null;
            
            #pragma warning disable VSTHRD001
            Application.Current.Dispatcher.Invoke(() =>
            {
                BankDummyDTO? bankDummy = DummyItems.OfType<BankDummyDTO>().FirstOrDefault();
                if (bankDummy is null) return;
                bankDTO.DummyParent = bankDummy;
                bankDummy.Banks.Add(bankDTO);
                bankDummy.IsExpanded = true;
                inserted = bankDTO;
            });
            #pragma warning restore VSTHRD001
            
            if (inserted != null) SelectedItem = inserted;
            _notificationService.ShowSuccess(message.CreatedBank.Message);
            return Task.CompletedTask;
        }

        public Task HandleAsync(BankUpdateMessage message, CancellationToken cancellationToken)
        {
            BankGraphQLModel updatedBank = message.UpdatedBank.Entity;
            
            #pragma warning disable VSTHRD001
            Application.Current.Dispatcher.Invoke(() =>
            {
                TreasuryBankMasterTreeDTO bankDTO = Context.AutoMapper.Map<TreasuryBankMasterTreeDTO>(updatedBank);
                BankDummyDTO? bankDummy = DummyItems.OfType<BankDummyDTO>().FirstOrDefault();
                if (bankDummy is null) return;
                TreasuryBankMasterTreeDTO? bankToUpdate = bankDummy.Banks.FirstOrDefault(x => x.Id == updatedBank.Id);
                if (bankToUpdate is null) return;
                bankToUpdate.Id = bankDTO.Id;
                bankToUpdate.AccountingEntity = bankDTO.AccountingEntity;
                bankToUpdate.PaymentMethodPrefix = bankDTO.PaymentMethodPrefix;
            });
            #pragma warning restore VSTHRD001

            _notificationService.ShowSuccess(message.UpdatedBank.Message);
            return Task.CompletedTask;
        }

        public Task HandleAsync(BankDeleteMessage message, CancellationToken cancellationToken)
        {
            
            #pragma warning disable VSTHRD001
            Application.Current.Dispatcher.Invoke(() =>
            {
                BankDummyDTO? bankDummy = DummyItems.OfType<BankDummyDTO>().FirstOrDefault();
                if (bankDummy is null) return;
                TreasuryBankMasterTreeDTO? bankToDelete = bankDummy.Banks.FirstOrDefault(x => x.Id == message.DeletedBank.DeletedId);
                if (bankToDelete is null) return;
                bankDummy.Banks.Remove(bankToDelete);
            });
            #pragma warning restore VSTHRD001

            _notificationService.ShowSuccess(message.DeletedBank.Message);
            return Task.CompletedTask;
        }

        public Task HandleAsync(BankAccountCreateMessage message, CancellationToken cancellationToken)
        {
            BankAccountGraphQLModel createdBankAccount = message.CreatedBankAccount.Entity;

            // La API puede crear cuentas contables como side-effect.
            _auxiliaryAccountingAccountCache.Clear();

            TreasuryBankAccountMasterTreeDTO bankAccountDTO = Context.AutoMapper.Map<TreasuryBankAccountMasterTreeDTO>(createdBankAccount);
            bankAccountDTO.Context = this;
            ITreasuryTreeMasterSelectedItem? inserted = null;
            
            #pragma warning disable VSTHRD001
            Application.Current.Dispatcher.Invoke(() =>
            {
                BankDummyDTO? bankDummy = DummyItems.OfType<BankDummyDTO>().FirstOrDefault();
                if (bankDummy is null) return;
                TreasuryBankMasterTreeDTO? bank = bankDummy.Banks.FirstOrDefault(x => x.Id == createdBankAccount.Bank.Id);
                if (bank is null) return;
                bank.BankAccounts.Add(bankAccountDTO);
                bankDummy.IsExpanded = true;
                bank.IsExpanded = true;
                inserted = bankAccountDTO;
            });
            #pragma warning restore VSTHRD001
            
            if (inserted != null) SelectedItem = inserted;
            _notificationService.ShowSuccess(message.CreatedBankAccount.Message);
            return Task.CompletedTask;
        }

        public Task HandleAsync(BankAccountDeleteMessage message, CancellationToken cancellationToken)
        {
            #pragma warning disable VSTHRD001
            Application.Current.Dispatcher.Invoke(() =>
            {
                BankDummyDTO? bankDummy = DummyItems.OfType<BankDummyDTO>().FirstOrDefault();
                if (bankDummy is null) return;

                foreach (TreasuryBankMasterTreeDTO bank in bankDummy.Banks)
                {
                    TreasuryBankAccountMasterTreeDTO? bankAccount = bank.BankAccounts.FirstOrDefault(x => x.Id == message.DeletedBankAccount.DeletedId);
                    if (bankAccount != null)
                    {
                        bank.BankAccounts.Remove(bankAccount);
                        break;
                    }
                }
            });
            #pragma warning restore VSTHRD001
            _notificationService.ShowSuccess(message.DeletedBankAccount.Message);
            return Task.CompletedTask;
        }

        public Task HandleAsync(BankAccountUpdateMessage message, CancellationToken cancellationToken)
        {
            BankAccountGraphQLModel updatedBankAccount = message.UpdatedBankAccount.Entity;
            
            #pragma warning disable VSTHRD001
            Application.Current.Dispatcher.Invoke(() =>
            {
                TreasuryBankAccountMasterTreeDTO bankAccountDTO = Context.AutoMapper.Map<TreasuryBankAccountMasterTreeDTO>(updatedBankAccount);
                BankDummyDTO? bankDummy = DummyItems.OfType<BankDummyDTO>().FirstOrDefault();
                if (bankDummy is null) return;
                TreasuryBankMasterTreeDTO? bank = bankDummy.Banks.FirstOrDefault(x => x.Id == updatedBankAccount.Bank.Id);
                if (bank is null) return;
                TreasuryBankAccountMasterTreeDTO? bankAccountToUpdate = bank.BankAccounts.FirstOrDefault(x => x.Id == updatedBankAccount.Id);
                if (bankAccountToUpdate is null) return;
                bankAccountToUpdate.Id = bankAccountDTO.Id;
                bankAccountToUpdate.Type = bankAccountDTO.Type;
                bankAccountToUpdate.Number = bankAccountDTO.Number;
                bankAccountToUpdate.IsActive = bankAccountDTO.IsActive;
                bankAccountToUpdate.Description = bankAccountDTO.Description;
                bankAccountToUpdate.Reference = bankAccountDTO.Reference;
                bankAccountToUpdate.DisplayOrder = bankAccountDTO.DisplayOrder;
                bankAccountToUpdate.AccountingAccount = bankAccountDTO.AccountingAccount;
                bankAccountToUpdate.Bank = bankAccountDTO.Bank;
                bankAccountToUpdate.Provider = bankAccountDTO.Provider;
                bankAccountToUpdate.PaymentMethod = bankAccountDTO.PaymentMethod;
                bankAccountToUpdate.AllowedCostCenters = bankAccountDTO.AllowedCostCenters;
            });
            #pragma warning restore VSTHRD001

            _notificationService.ShowSuccess(message.UpdatedBankAccount.Message);
            return Task.CompletedTask;
        }

        public Task HandleAsync(FranchiseCreateMessage message, CancellationToken cancellationToken)
        {
            FranchiseGraphQLModel createdFranchise = message.CreatedFranchise.Entity;
            TreasuryFranchiseMasterTreeDTO franchiseDTO = Context.AutoMapper.Map<TreasuryFranchiseMasterTreeDTO>(createdFranchise);
            franchiseDTO.Context = this;
            ITreasuryTreeMasterSelectedItem? inserted = null;
            
            #pragma warning disable VSTHRD001
            Application.Current.Dispatcher.Invoke(() =>
            {
                FranchiseDummyDTO? franchiseDummy = DummyItems.OfType<FranchiseDummyDTO>().FirstOrDefault();
                if (franchiseDummy is null) return;
                franchiseDTO.DummyParent = franchiseDummy;
                franchiseDummy.Franchises.Add(franchiseDTO);
                franchiseDummy.IsExpanded = true;
                inserted = franchiseDTO;
            });
            #pragma warning restore VSTHRD001

            if (inserted != null) SelectedItem = inserted;
            _notificationService.ShowSuccess(message.CreatedFranchise.Message);
            return Task.CompletedTask;
        }

        public Task HandleAsync(FranchiseDeleteMessage message, CancellationToken cancellationToken)
        {
            
            #pragma warning disable VSTHRD001
            Application.Current.Dispatcher?.Invoke(() =>
            {
                FranchiseDummyDTO? franchiseDummy = DummyItems.OfType<FranchiseDummyDTO>().FirstOrDefault();
                if (franchiseDummy is null) return;
                TreasuryFranchiseMasterTreeDTO? franchiseToDelete = franchiseDummy.Franchises.FirstOrDefault(x => x.Id == message.DeletedFranchise.DeletedId);
                if (franchiseToDelete is null) return;
                franchiseDummy.Franchises.Remove(franchiseToDelete);
            });
            #pragma warning restore VSTHRD001

            _notificationService.ShowSuccess(message.DeletedFranchise.Message);
            return Task.CompletedTask;
        }

        public Task HandleAsync(FranchiseUpdateMessage message, CancellationToken cancellationToken)
        {
            FranchiseGraphQLModel updatedFranchise = message.UpdatedFranchise.Entity;

            #pragma warning disable VSTHRD001
            Application.Current.Dispatcher.Invoke(() =>
            {
                TreasuryFranchiseMasterTreeDTO franchiseDTO = Context.AutoMapper.Map<TreasuryFranchiseMasterTreeDTO>(updatedFranchise);
                FranchiseDummyDTO? franchiseDummy = DummyItems.OfType<FranchiseDummyDTO>().FirstOrDefault();
                if (franchiseDummy is null) return;
                TreasuryFranchiseMasterTreeDTO? franchiseToUpdate = franchiseDummy.Franchises.FirstOrDefault(x => x.Id == updatedFranchise.Id);
                if (franchiseToUpdate is null) return;
                franchiseToUpdate.Id = franchiseDTO.Id;
                franchiseToUpdate.Name = franchiseDTO.Name;
                franchiseToUpdate.FormulaCommission = franchiseDTO.FormulaCommission;
                franchiseToUpdate.FormulaReteiva = franchiseDTO.FormulaReteiva;
                franchiseToUpdate.FormulaReteica = franchiseDTO.FormulaReteica;
                franchiseToUpdate.FormulaRetefte = franchiseDTO.FormulaRetefte;
                franchiseToUpdate.CommissionRate = franchiseDTO.CommissionRate;
                franchiseToUpdate.ReteivaRate = franchiseDTO.ReteivaRate;
                franchiseToUpdate.ReteicaRate = franchiseDTO.ReteicaRate;
                franchiseToUpdate.RetefteRate = franchiseDTO.RetefteRate;
                franchiseToUpdate.TaxRate = franchiseDTO.TaxRate;
                franchiseToUpdate.BankAccount = franchiseDTO.BankAccount;
                franchiseToUpdate.CommissionAccountingAccount = franchiseDTO.CommissionAccountingAccount;
                franchiseToUpdate.FranchisesByCostCenter = franchiseDTO.FranchisesByCostCenter;
            });
            #pragma warning restore VSTHRD001

            _notificationService.ShowSuccess(message.UpdatedFranchise.Message);
            return Task.CompletedTask;
        }

        #endregion
    }
}
