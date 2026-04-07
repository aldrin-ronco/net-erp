using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Extensions.Global;
using Microsoft.VisualStudio.Threading;
using Models.Global;
using NetErp.Global.CostCenters.Shared;
using NetErp.Global.CostCenters.Validators;
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
    /// Detail dialog ViewModel para Storage (bodega).
    /// Soporta Create y Update. Cascada Country â†’ Department â†’ City.
    /// Estado tri-valor (ACTIVE / READ_ONLY / INACTIVE).
    /// </summary>
    public class StorageDetailViewModel : CostCentersDetailViewModelBase
    {
        #region Constants

        private const string DefaultCountryCode = "169"; // Colombia

        #endregion

        #region Dependencies

        private readonly IRepository<StorageGraphQLModel> _storageService;
        private readonly StringLengthCache _stringLengthCache;
        private readonly StorageValidator _validator;

        #endregion

        #region Constructor

        public StorageDetailViewModel(
            IRepository<StorageGraphQLModel> storageService,
            IEventAggregator eventAggregator,
            StringLengthCache stringLengthCache,
            JoinableTaskFactory joinableTaskFactory,
            StorageValidator validator)
            : base(joinableTaskFactory, eventAggregator)
        {
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _stringLengthCache = stringLengthCache ?? throw new ArgumentNullException(nameof(stringLengthCache));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));

            DialogWidth = 540;
            DialogHeight = 552;
        }

        #endregion

        #region MaxLength

        public int NameMaxLength => _stringLengthCache.GetMaxLength<StorageGraphQLModel>(nameof(StorageGraphQLModel.Name));
        public int AddressMaxLength => _stringLengthCache.GetMaxLength<StorageGraphQLModel>(nameof(StorageGraphQLModel.Address));

        #endregion

        #region Sources

        public ObservableCollection<CountryGraphQLModel> Countries { get; set; } = [];

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

        [ExpandoPath("address")]
        public string Address
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Address));
                    this.TrackChange(nameof(Address), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        [ExpandoPath("status")]
        public string Status
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Status));
                    NotifyOfPropertyChange(nameof(IsStatusActive));
                    NotifyOfPropertyChange(nameof(IsStatusReadOnly));
                    NotifyOfPropertyChange(nameof(IsStatusInactive));
                    this.TrackChange(nameof(Status), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = CostCentersStatus.Active;

        public bool IsStatusActive
        {
            get => Status == CostCentersStatus.Active;
            set { if (value) Status = CostCentersStatus.Active; }
        }

        public bool IsStatusReadOnly
        {
            get => Status == CostCentersStatus.ReadOnly;
            set { if (value) Status = CostCentersStatus.ReadOnly; }
        }

        public bool IsStatusInactive
        {
            get => Status == CostCentersStatus.Inactive;
            set { if (value) Status = CostCentersStatus.Inactive; }
        }

        [ExpandoPath("companyLocationId")]
        public int CompanyLocationId
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(CompanyLocationId));
                    this.TrackChange(nameof(CompanyLocationId), value);
                }
            }
        }

        public CountryGraphQLModel? SelectedCountry
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedCountry));
                    SelectedDepartment = GeographicCascadeHelper.FirstDepartment(value);
                }
            }
        }

        public DepartmentGraphQLModel? SelectedDepartment
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedDepartment));
                    SelectedCity = value?.Cities?.FirstOrDefault();
                }
            }
        }

        public CityGraphQLModel? SelectedCity
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedCity));
                    NotifyOfPropertyChange(nameof(SelectedCityId));
                    this.TrackChange(nameof(SelectedCityId), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        [ExpandoPath("cityId")]
        public int SelectedCityId => SelectedCity?.Id ?? 0;

        #endregion

        #region CanSave

        public override bool CanSave => _validator.CanSave(new StorageCanSaveContext
        {
            IsBusy = IsBusy,
            Name = Name,
            SelectedCityId = SelectedCityId,
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

        public void SetForNew(int parentCompanyLocationId, IEnumerable<CountryGraphQLModel> countries)
        {
            Countries = [.. countries];
            NotifyOfPropertyChange(nameof(Countries));

            Id = 0;
            Name = string.Empty;
            Address = string.Empty;
            Status = CostCentersStatus.Active;
            CompanyLocationId = parentCompanyLocationId;

            (CountryGraphQLModel? country, DepartmentGraphQLModel? department, int cityId) =
                GeographicCascadeHelper.FindDefaults(Countries, FindCountryIdByCode(DefaultCountryCode));
            SelectedCountry = country;
            SelectedDepartment = department;
            SelectedCity = department?.Cities?.FirstOrDefault(c => c.Id == cityId) ?? department?.Cities?.FirstOrDefault();

            SeedDefaultValues();
        }

        public void SetForEdit(StorageGraphQLModel entity, IEnumerable<CountryGraphQLModel> countries)
        {
            Countries = [.. countries];
            NotifyOfPropertyChange(nameof(Countries));

            Id = entity.Id;
            Name = entity.Name;
            Address = entity.Address;
            Status = entity.Status;
            CompanyLocationId = entity.CompanyLocation?.Id ?? 0;

            int countryId = entity.City?.Department?.Country?.Id ?? 0;
            int departmentId = entity.City?.Department?.Id ?? 0;
            int cityId = entity.City?.Id ?? 0;

            SelectedCountry = Countries.FirstOrDefault(c => c.Id == countryId);
            SelectedDepartment = SelectedCountry?.Departments?.FirstOrDefault(d => d.Id == departmentId);
            SelectedCity = SelectedDepartment?.Cities?.FirstOrDefault(c => c.Id == cityId);

            SeedCurrentValues();
        }

        private int FindCountryIdByCode(string code)
        {
            return Countries.FirstOrDefault(c => c.Code == code)?.Id ?? 0;
        }

        private void SeedDefaultValues()
        {
            this.ClearSeeds();
            this.SeedValue(nameof(Status), Status);
            this.SeedValue(nameof(CompanyLocationId), CompanyLocationId);
            this.SeedValue(nameof(SelectedCityId), SelectedCityId);
            this.AcceptChanges();
        }

        private void SeedCurrentValues()
        {
            this.SeedValue(nameof(Name), Name);
            this.SeedValue(nameof(Address), Address);
            this.SeedValue(nameof(Status), Status);
            this.SeedValue(nameof(CompanyLocationId), CompanyLocationId);
            this.SeedValue(nameof(SelectedCityId), SelectedCityId);
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
                UpsertResponseType<StorageGraphQLModel> result = await ExecuteSaveAsync();

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
                        ? new StorageCreateMessage { CreatedStorage = result }
                        : new StorageUpdateMessage { UpdatedStorage = result },
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

        public async Task<UpsertResponseType<StorageGraphQLModel>> ExecuteSaveAsync()
        {
            try
            {
                if (IsNewRecord)
                {
                    (GraphQLQueryFragment _, string query) = _createQuery.Value;
                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "createResponseInput");
                    return await _storageService.CreateAsync<UpsertResponseType<StorageGraphQLModel>>(query, variables);
                }
                else
                {
                    (GraphQLQueryFragment _, string query) = _updateQuery.Value;
                    dynamic variables = ChangeCollector.CollectChanges(this, prefix: "updateResponseData");
                    variables.updateResponseId = Id;
                    return await _storageService.UpdateAsync<UpsertResponseType<StorageGraphQLModel>>(query, variables);
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
            StorageValidationContext context = new() { Name = Name };
            IReadOnlyList<string> errors = _validator.Validate(propertyName, value, context);
            SetPropertyErrors(propertyName, errors);
        }

        private void ValidateProperties()
        {
            StorageValidationContext context = new() { Name = Name };
            Dictionary<string, IReadOnlyList<string>> allErrors = _validator.ValidateAll(context);
            SetPropertyErrors(nameof(Name), allErrors.TryGetValue(nameof(Name), out IReadOnlyList<string>? nameErrors) ? nameErrors : []);
        }

        #endregion

        #region GraphQL Queries

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _createQuery = new(() =>
        {
            Dictionary<string, object> fields = BuildEntityFields();
            GraphQLQueryFragment fragment = new("createStorage",
                [new("input", "CreateStorageInput!")],
                fields, "CreateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _updateQuery = new(() =>
        {
            Dictionary<string, object> fields = BuildEntityFields();
            GraphQLQueryFragment fragment = new("updateStorage",
                [new("data", "UpdateStorageInput!"), new("id", "ID!")],
                fields, "UpdateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static Dictionary<string, object> BuildEntityFields()
        {
            return FieldSpec<UpsertResponseType<StorageGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "storage", nested: sq => sq
                    .Field(f => f.Id)
                    .Field(f => f.Name)
                    .Field(f => f.Address)
                    .Field(f => f.Status)
                    .Select(f => f.City, city => city
                        .Field(c => c.Id)
                        .Field(c => c.Code)
                        .Field(c => c.Name)
                        .Select(c => c.Department, dept => dept
                            .Field(d => d.Id)
                            .Select(d => d.Country, country => country
                                .Field(co => co.Id))))
                    .Select(f => f.CompanyLocation, loc => loc
                        .Field(l => l.Id)
                        .Select(l => l.Company, company => company
                            .Field(c => c.Id))))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();
        }

        #endregion
    }
}
