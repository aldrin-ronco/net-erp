using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Extensions.Global;
using Microsoft.VisualStudio.Threading;
using Models.Books;
using Models.Global;
using NetErp.Global.CostCenters.Validators;
using NetErp.Global.Modals.ViewModels;
using NetErp.Helpers;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Global.CostCenters.ViewModels
{
    /// <summary>
    /// Detail dialog ViewModel para Company.
    /// Solo soporta Update â€” cambia la AccountingEntity asociada via mutation
    /// 'changeCurrentCompanyEntity'. Invoca un sub-modal de bÃºsqueda de AccountingEntity
    /// (twocolumns grid) y recibe el resultado vÃ­a Messenger.Default.
    /// </summary>
    public class CompanyDetailViewModel : CostCentersDetailViewModelBase
    {
        #region Dependencies

        private readonly IRepository<CompanyGraphQLModel> _companyService;
        private readonly NetErp.Helpers.IDialogService _dialogService;
        private readonly CompanyValidator _validator;

        #endregion

        #region Constructor

        public CompanyDetailViewModel(
            IRepository<CompanyGraphQLModel> companyService,
            IEventAggregator eventAggregator,
            NetErp.Helpers.IDialogService dialogService,
            JoinableTaskFactory joinableTaskFactory,
            CompanyValidator validator)
            : base(joinableTaskFactory, eventAggregator)
        {
            _companyService = companyService ?? throw new ArgumentNullException(nameof(companyService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));

            DialogWidth = 540;
            DialogHeight = 280;

            Messenger.Default.Register<ReturnedDataFromModalWithTwoColumnsGridViewMessage<AccountingEntityGraphQLModel>>(
                this,
                SearchWithTwoColumnsGridMessageToken.CompanyAccountingEntity,
                false,
                OnFindCompanyAccountingEntityMessage);
        }

        #endregion

        #region Form Properties

        [ExpandoPath("accountingEntityId")]
        public int AccountingEntityCompanyId
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(AccountingEntityCompanyId));
                    ValidateAccountingEntity();
                    this.TrackChange(nameof(AccountingEntityCompanyId), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        public string AccountingEntityCompanySearchName
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(AccountingEntityCompanySearchName));
                }
            }
        } = string.Empty;

        #endregion

        #region CanSave

        public override bool CanSave => _validator.CanSave(new CompanyCanSaveContext
        {
            IsBusy = IsBusy,
            AccountingEntityCompanyId = AccountingEntityCompanyId,
            HasChanges = this.HasChanges(),
            HasErrors = _errors.Count > 0
        });

        #endregion

        #region Commands

        private ICommand? _saveCommand;
        public ICommand SaveCommand => _saveCommand ??= new AsyncCommand(SaveAsync);

        private ICommand? _cancelCommand;
        public ICommand CancelCommand => _cancelCommand ??= new AsyncCommand(CancelAsync);

        private ICommand? _searchAccountingEntityCommand;
        public ICommand SearchAccountingEntityCommand => _searchAccountingEntityCommand ??= new AsyncCommand(SearchAccountingEntityAsync);

        #endregion

        #region SetForEdit (no SetForNew â€” Company es Update only)

        public void SetForEdit(CompanyGraphQLModel entity)
        {
            Id = entity.Id;
            AccountingEntityCompanyId = entity.CompanyEntity?.Id ?? 0;
            AccountingEntityCompanySearchName = entity.CompanyEntity?.SearchName ?? string.Empty;
            SeedCurrentValues();
        }

        private void SeedCurrentValues()
        {
            this.SeedValue(nameof(AccountingEntityCompanyId), AccountingEntityCompanyId);
            this.AcceptChanges();
        }

        #endregion

        #region Lifecycle

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            ValidateProperties();
            this.AcceptChanges();
            NotifyOfPropertyChange(nameof(CanSave));
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

        #region Save / Cancel

        public async Task SaveAsync()
        {
            try
            {
                IsBusy = true;
                UpsertResponseType<CompanyGraphQLModel> result = await ExecuteSaveAsync();

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

                await _eventAggregator.PublishOnCurrentThreadAsync(
                    new CompanyUpdateMessage { UpdatedCompany = result },
                    CancellationToken.None);

                await TryCloseAsync(true);
            }
            catch (AsyncException ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("AtenciÃ³n!",
                    $"Error al realizar operaciÃ³n.\r\n{ex.GetErrorMessage()}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("AtenciÃ³n!",
                    $"{GetType().Name}.{nameof(SaveAsync)}: {ex.GetErrorMessage()}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task<UpsertResponseType<CompanyGraphQLModel>> ExecuteSaveAsync()
        {
            try
            {
                (GraphQLQueryFragment _, string query) = _updateQuery.Value;
                dynamic variables = new ExpandoObject();
                variables.updateResponseAccountingEntityId = AccountingEntityCompanyId;
                return await _companyService.UpdateAsync<UpsertResponseType<CompanyGraphQLModel>>(query, variables);
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }

        public async Task CancelAsync()
        {
            await TryCloseAsync(false);
        }

        #endregion

        #region AccountingEntity Search Sub-Modal

        public async Task SearchAccountingEntityAsync()
        {
            try
            {
                (GraphQLQueryFragment _, string searchQuery) = _searchAccountingEntityQuery.Value;

                SearchWithTwoColumnsGridViewModel<AccountingEntityGraphQLModel> searchVm = new(
                    searchQuery,
                    "IdentificaciÃ³n",
                    "RazÃ³n social",
                    "IdentificationNumberWithVerificationDigit",
                    "SearchName",
                    null,
                    SearchWithTwoColumnsGridMessageToken.CompanyAccountingEntity,
                    _dialogService);

                await _dialogService.ShowDialogAsync(searchVm, "Buscar entidad contable");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("AtenciÃ³n!",
                    $"{GetType().Name}.{nameof(SearchAccountingEntityAsync)}: {ex.GetErrorMessage()}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnFindCompanyAccountingEntityMessage(ReturnedDataFromModalWithTwoColumnsGridViewMessage<AccountingEntityGraphQLModel> message)
        {
            if (message.ReturnedData is null) return;
            AccountingEntityCompanyId = message.ReturnedData.Id;
            AccountingEntityCompanySearchName = message.ReturnedData.SearchName;
        }

        #endregion

        #region Validation

        // Nota: el validador opera sobre AccountingEntityCompanyId (clave del FK),
        // pero los errores se reportan bajo AccountingEntityCompanySearchName porque
        // el binding del control visual (ButtonEdit readonly) usa ese campo display.
        private void ValidateAccountingEntity()
        {
            CompanyValidationContext context = new() { AccountingEntityCompanyId = AccountingEntityCompanyId };
            IReadOnlyList<string> errors = _validator.Validate(nameof(AccountingEntityCompanyId), AccountingEntityCompanyId, context);
            SetPropertyErrors(nameof(AccountingEntityCompanySearchName), errors);
        }

        private void ValidateProperties()
        {
            CompanyValidationContext context = new() { AccountingEntityCompanyId = AccountingEntityCompanyId };
            Dictionary<string, IReadOnlyList<string>> allErrors = _validator.ValidateAll(context);
            SetPropertyErrors(nameof(AccountingEntityCompanySearchName),
                allErrors.TryGetValue(nameof(AccountingEntityCompanyId), out IReadOnlyList<string>? errors) ? errors : []);
        }

        #endregion

        #region GraphQL Queries

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _updateQuery = new(() =>
        {
            Dictionary<string, object> fields = FieldSpec<UpsertResponseType<CompanyGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "company", nested: sq => sq
                    .Field(f => f.Id)
                    .Select(f => f.CompanyEntity, ce => ce
                        .Field(c => c.Id)
                        .Field(c => c.SearchName)))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            GraphQLQueryFragment fragment = new("changeCurrentCompanyEntity",
                [new("accountingEntityId", "ID!")],
                fields, "UpdateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _searchAccountingEntityQuery = new(() =>
        {
            Dictionary<string, object> fields = FieldSpec<PageType<AccountingEntityGraphQLModel>>
                .Create()
                .Field(f => f.PageNumber)
                .Field(f => f.PageSize)
                .Field(f => f.TotalEntries)
                .SelectList(f => f.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.IdentificationNumber)
                    .Field(e => e.VerificationDigit)
                    .Field(e => e.SearchName))
                .Build();

            GraphQLQueryFragment fragment = new("accountingEntitiesPage",
                [new("filters", "AccountingEntityFilters"), new("pagination", "Pagination")],
                fields, "PageResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        #endregion
    }
}
