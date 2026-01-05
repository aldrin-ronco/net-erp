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
        [ExpandoPath("Data.name")]
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
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _address = string.Empty;
        [ExpandoPath("Data.address")]
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
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private string _state = "A";
        [ExpandoPath("Data.state")]
        public string State
        {
            get => _state;
            set
            {
                if (_state != value)
                {
                    _state = value;
                    NotifyOfPropertyChange(nameof(State));
                    this.TrackChange(nameof(State));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private int _cityId;
        [ExpandoPath("Data.cityId")]
        public int CityId
        {
            get => _cityId;
            set
            {
                if (_cityId != value)
                {
                    _cityId = value;
                    NotifyOfPropertyChange(nameof(CityId));
                    this.TrackChange(nameof(CityId));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private int _companyLocationId;
        [ExpandoPath("Data.companyLocationId")]
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
                    this.TrackChange(nameof(SelectedCountry));

                    // Al cambiar país, resetear departamento y ciudad
                    if (value != null && value.Departments?.Any() == true)
                    {
                        SelectedDepartment = value.Departments.First();
                    }
                    else
                    {
                        SelectedDepartment = null;
                    }
                    NotifyOfPropertyChange(nameof(CanSave));
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
                    this.TrackChange(nameof(SelectedDepartment));

                    // Al cambiar departamento, resetear ciudad
                    if (value != null && value.Cities?.Any() == true)
                    {
                        SelectedCity = value.Cities.First();
                    }
                    else
                    {
                        SelectedCity = null;
                    }
                    NotifyOfPropertyChange(nameof(CanSave));
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
                    this.TrackChange(nameof(SelectedCity));

                    // Actualizar CityId cuando cambia la ciudad
                    CityId = value?.Id ?? 0;
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

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
            State = "A";
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
            State = storageDTO.State;
            CompanyLocationId = storageDTO.Location?.Id ?? 0;

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

            this.AcceptChanges();
            ClearAllErrors();
            ValidateAll();

            IsEditing = false;
        }

        private void SeedDefaultValues()
        {
            this.SeedValue(nameof(State), State);
            this.SeedValue(nameof(Name), string.Empty);
            this.SeedValue(nameof(Address), string.Empty);
            this.SeedValue(nameof(CompanyLocationId), CompanyLocationIdBeforeNew);
            this.AcceptChanges();
        }

        #endregion

        #region Abstract Methods Implementation

        protected override int GetId() => Id;

        protected override string GetCreateQuery()
        {
            var fields = FieldSpec<StorageGraphQLModel>
                .Create()
                .Field(f => f.Id)
                .Field(f => f.Name)
                .Field(f => f.Address)
                .Field(f => f.State)
                .Select(f => f.City, nested => nested
                    .Field(n => n.Id)
                    .Field(n => n.Code)
                    .Field(n => n.Name)
                    .Select(n => n.Department, deptNested => deptNested
                        .Field(d => d.Id)
                        .Select(d => d.Country, countryNested => countryNested
                            .Field(c => c.Id))))
                .Select(f => f.Location, nested => nested
                    .Field(n => n.Id)
                    .Select(n => n.Company, companyNested => companyNested
                        .Field(c => c.Id)))
                .Build();

            var parameter = new GraphQLQueryParameter("data", "CreateStorageInput!");
            var fragment = new GraphQLQueryFragment("createStorage", [parameter], fields, "CreateResponse");
            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }

        protected override string GetUpdateQuery()
        {
            var fields = FieldSpec<StorageGraphQLModel>
                .Create()
                .Field(f => f.Id)
                .Field(f => f.Name)
                .Field(f => f.Address)
                .Field(f => f.State)
                .Select(f => f.City, nested => nested
                    .Field(n => n.Id)
                    .Field(n => n.Code)
                    .Field(n => n.Name)
                    .Select(n => n.Department, deptNested => deptNested
                        .Field(d => d.Id)
                        .Select(d => d.Country, countryNested => countryNested
                            .Field(c => c.Id))))
                .Select(f => f.Location, nested => nested
                    .Field(n => n.Id)
                    .Select(n => n.Company, companyNested => companyNested
                        .Field(c => c.Id)))
                .Build();

            var parameters = new List<GraphQLQueryParameter>
            {
                new GraphQLQueryParameter("data", "UpdateStorageInput!"),
                new GraphQLQueryParameter("id", "Int!")
            };
            var fragment = new GraphQLQueryFragment("updateStorage", parameters, fields, "UpdateResponse");
            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }

        protected override async Task<StorageGraphQLModel> ExecuteSaveAsync()
        {
            string query;
            dynamic variables;

            if (IsNewRecord)
            {
                // Asignar el CompanyLocationId del padre antes de recolectar cambios
                CompanyLocationId = CompanyLocationIdBeforeNew;
                query = GetCreateQuery();
                variables = ChangeCollector.CollectChanges(this, prefix: "data");
            }
            else
            {
                query = GetUpdateQuery();
                variables = ChangeCollector.CollectChanges(this, prefix: "data");
                variables.id = Id;
            }

            return IsNewRecord
                ? await _storageService.CreateAsync(query, variables)
                : await _storageService.UpdateAsync(query, variables);
        }

        protected override async Task PublishMessageAsync(StorageGraphQLModel result)
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
