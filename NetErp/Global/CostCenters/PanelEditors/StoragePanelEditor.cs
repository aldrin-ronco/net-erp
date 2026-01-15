using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using Models.Global;
using NetErp.Global.CostCenters.DTO;
using NetErp.Global.CostCenters.ViewModels;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Global.CostCenters.PanelEditors
{
    /// <summary>
    /// Panel Editor para la entidad Storage (Bodega).
    /// Maneja la lógica de edición y persistencia de bodegas.
    /// Storage soporta Create y Update.
    /// </summary>
    public class StoragePanelEditor : CostCentersBasePanelEditor<StorageDTO, StorageGraphQLModel>
    {
        #region Fields

        private readonly IRepository<StorageGraphQLModel> _storageService;

        #endregion

        #region Constructor

        public StoragePanelEditor(
            CostCenterMasterViewModel masterContext,
            IRepository<StorageGraphQLModel> storageService)
            : base(masterContext)
        {
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        }

        #endregion

        #region Properties

        private int _id;
        public int Id
        {
            get => _id;
            set
            {
                if (_id != value)
                {
                    _id = value;
                    NotifyOfPropertyChange(nameof(Id));
                }
            }
        }

        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    NotifyOfPropertyChange(nameof(Name));
                    this.TrackChange(nameof(Name));
                    ValidateName();
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private string _address = string.Empty;
        public string Address
        {
            get => _address;
            set
            {
                if (_address != value)
                {
                    _address = value;
                    NotifyOfPropertyChange(nameof(Address));
                    this.TrackChange(nameof(Address));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        private string _status = "ACTIVE";
        public string Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    NotifyOfPropertyChange(nameof(Status));
                    this.TrackChange(nameof(Status));
                    NotifyOfPropertyChange(nameof(IsStatusActive));
                    NotifyOfPropertyChange(nameof(IsStatusReadOnly));
                    NotifyOfPropertyChange(nameof(IsStatusInactive));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        public bool IsStatusActive
        {
            get => Status == "ACTIVE";
            set
            {
                if (value) Status = "ACTIVE";
            }
        }

        public bool IsStatusReadOnly
        {
            get => Status == "READ_ONLY";
            set
            {
                if (value) Status = "READ_ONLY";
            }
        }

        public bool IsStatusInactive
        {
            get => Status == "INACTIVE";
            set
            {
                if (value) Status = "INACTIVE";
            }
        }

        private int _companyLocationId;
        public int CompanyLocationId
        {
            get => _companyLocationId;
            set
            {
                if (_companyLocationId != value)
                {
                    _companyLocationId = value;
                    NotifyOfPropertyChange(nameof(CompanyLocationId));
                    this.TrackChange(nameof(CompanyLocationId));
                }
            }
        }

        /// <summary>
        /// Id de la sede padre cuando se crea un nuevo storage.
        /// </summary>
        private int _companyLocationIdBeforeNew;
        public int CompanyLocationIdBeforeNew
        {
            get => _companyLocationIdBeforeNew;
            set
            {
                if (_companyLocationIdBeforeNew != value)
                {
                    _companyLocationIdBeforeNew = value;
                    NotifyOfPropertyChange(nameof(CompanyLocationIdBeforeNew));
                }
            }
        }

        #region Country/Department/City Selection

        private CountryGraphQLModel? _selectedCountry;
        public CountryGraphQLModel? SelectedCountry
        {
            get => _selectedCountry;
            set
            {
                if (_selectedCountry != value)
                {
                    _selectedCountry = value;
                    NotifyOfPropertyChange(nameof(SelectedCountry));

                    // Al cambiar país, resetear departamento y ciudad
                    if (value != null && value.Departments?.Any() == true)
                    {
                        SelectedDepartment = value.Departments.First();
                    }
                    else
                    {
                        SelectedDepartment = null;
                    }
                }
            }
        }

        private DepartmentGraphQLModel? _selectedDepartment;
        public DepartmentGraphQLModel? SelectedDepartment
        {
            get => _selectedDepartment;
            set
            {
                if (_selectedDepartment != value)
                {
                    _selectedDepartment = value;
                    NotifyOfPropertyChange(nameof(SelectedDepartment));

                    // Al cambiar departamento, resetear ciudad
                    if (value != null && value.Cities?.Any() == true)
                    {
                        SelectedCity = value.Cities.First();
                    }
                    else
                    {
                        SelectedCity = null;
                    }
                }
            }
        }

        private CityGraphQLModel? _selectedCity;
        public CityGraphQLModel? SelectedCity
        {
            get => _selectedCity;
            set
            {
                if (_selectedCity != value)
                {
                    _selectedCity = value;
                    NotifyOfPropertyChange(nameof(SelectedCity));
                    NotifyOfPropertyChange(nameof(SelectedCityId));
                    this.TrackChange(nameof(SelectedCityId));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        [ExpandoPath("cityId")]
        public int SelectedCityId => SelectedCity?.Id ?? 0;

        #endregion

        #endregion

        #region CanSave

        public override bool CanSave
        {
            get
            {
                if (!IsEditing) return false;
                if (HasErrors) return false;
                if (!this.HasChanges()) return false;
                if (SelectedCity == null) return false;
                return true;
            }
        }

        #endregion

        #region Validation

        private void ValidateName()
        {
            ClearErrors(nameof(Name));
            string trimmedName = Name?.Trim().RemoveExtraSpaces() ?? string.Empty;
            if (string.IsNullOrEmpty(trimmedName))
            {
                AddError(nameof(Name), "El nombre de la bodega es requerido");
            }
        }

        public override void ValidateAll()
        {
            ValidateName();
        }

        #endregion

        #region SetForNew / SetForEdit

        public override void SetForNew(object context)
        {
            if (context is int parentLocationId)
            {
                CompanyLocationIdBeforeNew = parentLocationId;
            }

            OriginalDto = null;
            Id = 0;
            Name = string.Empty;
            Address = string.Empty;
            Status = "ACTIVE";
            CompanyLocationId = 0;

            // Establecer país por defecto (Colombia - código 169)
            SelectedCountry = MasterContext.Countries?.FirstOrDefault(c => c.Code == "169");
            if (SelectedCountry != null)
            {
                SelectedDepartment = SelectedCountry.Departments?.FirstOrDefault();
                if (SelectedDepartment != null)
                {
                    SelectedCity = SelectedDepartment.Cities?.FirstOrDefault();
                }
            }

            SeedDefaultValues();
            ClearAllErrors();
            ValidateAll();

            IsEditing = true;
        }

        public override void SetForEdit(object dto)
        {
            if (dto is not StorageDTO storageDTO) return;

            OriginalDto = storageDTO;
            Id = storageDTO.Id;
            Name = storageDTO.Name;
            Address = storageDTO.Address;
            Status = storageDTO.Status;
            CompanyLocationId = storageDTO.CompanyLocation?.Id ?? 0;

            // Establecer país/departamento/ciudad desde el DTO
            SelectedCountry = MasterContext.Countries?.FirstOrDefault(c => c.Id == storageDTO.City?.Department?.Country?.Id);
            if (SelectedCountry != null)
            {
                SelectedDepartment = SelectedCountry.Departments?.FirstOrDefault(d => d.Id == storageDTO.City?.Department?.Id);
                if (SelectedDepartment != null)
                {
                    SelectedCity = SelectedDepartment.Cities?.FirstOrDefault(c => c.Id == storageDTO.City?.Id);
                }
            }

            SeedCurrentValues();
            ClearAllErrors();
            ValidateAll();

            IsEditing = false;
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

        private void SeedDefaultValues()
        {
            this.SeedValue(nameof(Status), Status);
            this.SeedValue(nameof(Name), string.Empty);
            this.SeedValue(nameof(Address), string.Empty);
            this.SeedValue(nameof(CompanyLocationId), CompanyLocationIdBeforeNew);
            this.SeedValue(nameof(SelectedCityId), SelectedCityId);
            this.AcceptChanges();
        }

        #endregion

        #region Abstract Methods Implementation

        protected override int GetId() => Id;

        protected override string GetCreateQuery()
        {
            var fields = FieldSpec<UpsertResponseType<StorageGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "storage", nested: entity => entity
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Field(e => e.Address)
                    .Field(e => e.Status)
                    .Select(e => e.City, city => city
                        .Field(c => c.Id)
                        .Field(c => c.Code)
                        .Field(c => c.Name)
                        .Select(c => c.Department, dept => dept
                            .Field(d => d.Id)
                            .Select(d => d.Country, country => country
                                .Field(co => co.Id))))
                    .Select(e => e.CompanyLocation, loc => loc
                        .Field(l => l.Id)
                        .Select(l => l.Company, company => company
                            .Field(c => c.Id))))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, errors => errors
                    .Field(e => e.Fields)
                    .Field(e => e.Message))
                .Build();

            var parameter = new GraphQLQueryParameter("input", "CreateStorageInput!");
            var fragment = new GraphQLQueryFragment("createStorage", [parameter], fields, "CreateResponse");
            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }

        protected override string GetUpdateQuery()
        {
            var fields = FieldSpec<UpsertResponseType<StorageGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "storage", nested: entity => entity
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Field(e => e.Address)
                    .Field(e => e.Status)
                    .Select(e => e.City, city => city
                        .Field(c => c.Id)
                        .Field(c => c.Code)
                        .Field(c => c.Name)
                        .Select(c => c.Department, dept => dept
                            .Field(d => d.Id)
                            .Select(d => d.Country, country => country
                                .Field(co => co.Id))))
                    .Select(e => e.CompanyLocation, loc => loc
                        .Field(l => l.Id)
                        .Select(l => l.Company, company => company
                            .Field(c => c.Id))))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, errors => errors
                    .Field(e => e.Fields)
                    .Field(e => e.Message))
                .Build();

            var parameters = new List<GraphQLQueryParameter>
            {
                new("data", "UpdateStorageInput!"),
                new("id", "ID!")
            };
            var fragment = new GraphQLQueryFragment("updateStorage", parameters, fields, "UpdateResponse");
            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }

        protected override async Task<UpsertResponseType<StorageGraphQLModel>> ExecuteSaveAsync()
        {
            string query;
            dynamic variables;

            if (IsNewRecord)
            {
                // Asignar el CompanyLocationId del padre antes de recolectar cambios
                CompanyLocationId = CompanyLocationIdBeforeNew;
                query = GetCreateQuery();
                variables = ChangeCollector.CollectChanges(this, prefix: "createResponseInput");
            }
            else
            {
                query = GetUpdateQuery();
                variables = ChangeCollector.CollectChanges(this, prefix: "updateResponseData");
                variables.updateResponseId = Id;
            }

            return IsNewRecord
                ? await _storageService.CreateAsync<UpsertResponseType<StorageGraphQLModel>>(query, variables)
                : await _storageService.UpdateAsync<UpsertResponseType<StorageGraphQLModel>>(query, variables);
        }

        protected override async Task PublishMessageAsync(UpsertResponseType<StorageGraphQLModel> result)
        {
            if (IsNewRecord)
            {
                await MasterContext.Context.EventAggregator.PublishOnUIThreadAsync(
                    new StorageCreateMessage { CreatedStorage = result });
            }
            else
            {
                await MasterContext.Context.EventAggregator.PublishOnUIThreadAsync(
                    new StorageUpdateMessage { UpdatedStorage = result });
            }
        }

        #endregion
    }
}
