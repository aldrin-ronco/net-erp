using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Extensions.Global;
using Microsoft.VisualStudio.Threading;
using Models.Global;
using NetErp.Global.CostCenters.Validators;
using NetErp.Helpers.Cache;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Global.CostCenters.ViewModels
{
    /// <summary>
    /// Detail dialog ViewModel para CompanyLocation (sede/sucursal).
    /// Soporta Create y Update. Ãšnico campo: Name. Padre: CompanyId.
    /// </summary>
    public class CompanyLocationDetailViewModel : CostCentersDetailViewModelBase
    {
        #region Dependencies

        private readonly IRepository<CompanyLocationGraphQLModel> _companyLocationService;
        private readonly StringLengthCache _stringLengthCache;
        private readonly CompanyLocationValidator _validator;

        #endregion

        #region Constructor

        public CompanyLocationDetailViewModel(
            IRepository<CompanyLocationGraphQLModel> companyLocationService,
            IEventAggregator eventAggregator,
            StringLengthCache stringLengthCache,
            JoinableTaskFactory joinableTaskFactory,
            CompanyLocationValidator validator)
            : base(joinableTaskFactory, eventAggregator)
        {
            _companyLocationService = companyLocationService ?? throw new ArgumentNullException(nameof(companyLocationService));
            _stringLengthCache = stringLengthCache ?? throw new ArgumentNullException(nameof(stringLengthCache));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));

            DialogWidth = 460;
            DialogHeight = 240;
        }

        #endregion

        #region MaxLength

        public int NameMaxLength => _stringLengthCache.GetMaxLength<CompanyLocationGraphQLModel>(nameof(CompanyLocationGraphQLModel.Name));

        #endregion

        #region Form Properties

        [ExpandoPath("name")]
        public string Name
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Name));
                    ValidateProperty(nameof(Name), value);
                    this.TrackChange(nameof(Name), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        [ExpandoPath("companyId")]
        public int CompanyId
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(CompanyId));
                    this.TrackChange(nameof(CompanyId), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        #endregion

        #region CanSave

        public override bool CanSave => _validator.CanSave(new CompanyLocationCanSaveContext
        {
            IsBusy = IsBusy,
            Name = Name,
            HasChanges = this.HasChanges(),
            HasErrors = _errors.Count > 0
        });

        #endregion

        #region Commands

        private ICommand? _saveCommand;
        public ICommand SaveCommand => _saveCommand ??= new AsyncCommand(SaveAsync);

        private ICommand? _cancelCommand;
        public ICommand CancelCommand => _cancelCommand ??= new AsyncCommand(CancelAsync);

        #endregion

        #region SetForNew / SetForEdit

        public void SetForNew(int parentCompanyId)
        {
            Id = 0;
            Name = string.Empty;
            CompanyId = parentCompanyId;
            SeedDefaultValues();
        }

        public void SetForEdit(CompanyLocationGraphQLModel entity)
        {
            Id = entity.Id;
            Name = entity.Name;
            CompanyId = entity.Company?.Id ?? 0;
            SeedCurrentValues();
        }

        private void SeedDefaultValues()
        {
            this.ClearSeeds();
            this.SeedValue(nameof(CompanyId), CompanyId);
            this.AcceptChanges();
        }

        private void SeedCurrentValues()
        {
            this.SeedValue(nameof(Name), Name);
            this.SeedValue(nameof(CompanyId), CompanyId);
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

        #endregion

        #region Save / Cancel

        public async Task SaveAsync()
        {
            try
            {
                IsBusy = true;
                UpsertResponseType<CompanyLocationGraphQLModel> result = await ExecuteSaveAsync();

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
                    IsNewRecord
                        ? new CompanyLocationCreateMessage { CreatedCompanyLocation = result }
                        : new CompanyLocationUpdateMessage { UpdatedCompanyLocation = result },
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

        public async Task<UpsertResponseType<CompanyLocationGraphQLModel>> ExecuteSaveAsync()
        {
            try
            {
                if (IsNewRecord)
                {
                    (GraphQLQueryFragment _, string query) = _createQuery.Value;
                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "createResponseInput");
                    return await _companyLocationService.CreateAsync<UpsertResponseType<CompanyLocationGraphQLModel>>(query, variables);
                }
                else
                {
                    (GraphQLQueryFragment _, string query) = _updateQuery.Value;
                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "updateResponseData");
                    variables.updateResponseId = Id;
                    return await _companyLocationService.UpdateAsync<UpsertResponseType<CompanyLocationGraphQLModel>>(query, variables);
                }
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

        #region Validation

        private void ValidateProperty(string propertyName, string? value)
        {
            CompanyLocationValidationContext context = new() { Name = Name };
            IReadOnlyList<string> errors = _validator.Validate(propertyName, value, context);
            SetPropertyErrors(propertyName, errors);
        }

        private void ValidateProperties()
        {
            CompanyLocationValidationContext context = new() { Name = Name };
            Dictionary<string, IReadOnlyList<string>> allErrors = _validator.ValidateAll(context);
            SetPropertyErrors(nameof(Name), allErrors.TryGetValue(nameof(Name), out IReadOnlyList<string>? nameErrors) ? nameErrors : []);
        }

        #endregion

        #region GraphQL Queries

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _createQuery = new(() =>
        {
            Dictionary<string, object> fields = FieldSpec<UpsertResponseType<CompanyLocationGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "companyLocation", nested: sq => sq
                    .Field(f => f.Id)
                    .Field(f => f.Name)
                    .Select(f => f.Company, c => c.Field(x => x.Id)))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            GraphQLQueryFragment fragment = new("createCompanyLocation",
                [new("input", "CreateCompanyLocationInput!")],
                fields, "CreateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _updateQuery = new(() =>
        {
            Dictionary<string, object> fields = FieldSpec<UpsertResponseType<CompanyLocationGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "companyLocation", nested: sq => sq
                    .Field(f => f.Id)
                    .Field(f => f.Name)
                    .Select(f => f.Company, c => c.Field(x => x.Id)))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            GraphQLQueryFragment fragment = new("updateCompanyLocation",
                [new("data", "UpdateCompanyLocationInput!"), new("id", "ID!")],
                fields, "UpdateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        #endregion
    }
}
