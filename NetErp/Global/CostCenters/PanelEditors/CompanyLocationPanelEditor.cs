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
using System.Threading.Tasks;

namespace NetErp.Global.CostCenters.PanelEditors
{
    /// <summary>
    /// Panel Editor para la entidad CompanyLocation.
    /// Maneja la lógica de edición y persistencia de sedes/ubicaciones.
    /// CompanyLocation soporta Create y Update.
    /// </summary>
    public class CompanyLocationPanelEditor : CostCentersBasePanelEditor<CompanyLocationDTO, CompanyLocationGraphQLModel>
    {
        #region Fields

        private readonly IRepository<CompanyLocationGraphQLModel> _companyLocationService;

        #endregion

        #region Constructor

        public CompanyLocationPanelEditor(
            CostCenterMasterViewModel masterContext,
            IRepository<CompanyLocationGraphQLModel> companyLocationService)
            : base(masterContext)
        {
            _companyLocationService = companyLocationService ?? throw new ArgumentNullException(nameof(companyLocationService));
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

        private int _companyId;
        [ExpandoPath("Data.companyId")]
        public int CompanyId
        {
            get => _companyId;
            set
            {
                if (_companyId != value)
                {
                    _companyId = value;
                    NotifyOfPropertyChange(nameof(CompanyId));
                    this.TrackChange(nameof(CompanyId));
                }
            }
        }

        /// <summary>
        /// Id de la compañía padre cuando se crea una nueva sede.
        /// Se guarda al seleccionar el nodo padre antes de crear.
        /// </summary>
        private int _companyIdBeforeNew;
        public int CompanyIdBeforeNew
        {
            get => _companyIdBeforeNew;
            set
            {
                if (_companyIdBeforeNew != value)
                {
                    _companyIdBeforeNew = value;
                    NotifyOfPropertyChange(nameof(CompanyIdBeforeNew));
                }
            }
        }

        #endregion

        #region CanSave

        public override bool CanSave
        {
            get
            {
                if (!IsEditing) return false;
                if (HasErrors) return false;
                if (!this.HasChanges()) return false;
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
                AddError(nameof(Name), "El nombre de la sede es requerido");
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
            if (context is int parentCompanyId)
            {
                CompanyIdBeforeNew = parentCompanyId;
            }

            OriginalDto = null;
            Id = 0;
            Name = string.Empty;
            CompanyId = 0;

            this.SeedValue(nameof(Name), string.Empty);
            this.SeedValue(nameof(CompanyId), CompanyIdBeforeNew);
            this.AcceptChanges();
            ClearAllErrors();

            IsEditing = true;
        }

        public override void SetForEdit(object dto)
        {
            if (dto is not CompanyLocationDTO companyLocationDTO) return;

            OriginalDto = companyLocationDTO;
            Id = companyLocationDTO.Id;
            Name = companyLocationDTO.Name;
            CompanyId = companyLocationDTO.Company?.Id ?? 0;

            this.AcceptChanges();
            ClearAllErrors();

            IsEditing = false;
        }

        #endregion

        #region Abstract Methods Implementation

        protected override int GetId() => Id;

        protected override string GetCreateQuery()
        {
            var fields = FieldSpec<CompanyLocationGraphQLModel>
                .Create()
                .Field(f => f.Id)
                .Field(f => f.Name)
                .Select(f => f.Company, nested => nested
                    .Field(n => n.Id))
                .Build();

            var parameter = new GraphQLQueryParameter("data", "CreateCompanyLocationInput!");
            var fragment = new GraphQLQueryFragment("createCompanyLocation", [parameter], fields, "CreateResponse");
            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }

        protected override string GetUpdateQuery()
        {
            var fields = FieldSpec<CompanyLocationGraphQLModel>
                .Create()
                .Field(f => f.Id)
                .Field(f => f.Name)
                .Select(f => f.Company, nested => nested
                    .Field(n => n.Id))
                .Build();

            var parameters = new List<GraphQLQueryParameter>
            {
                new GraphQLQueryParameter("data", "UpdateCompanyLocationInput!"),
                new GraphQLQueryParameter("id", "Int!")
            };
            var fragment = new GraphQLQueryFragment("updateCompanyLocation", parameters, fields, "UpdateResponse");
            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }

        protected override async Task<CompanyLocationGraphQLModel> ExecuteSaveAsync()
        {
            string query;
            dynamic variables;

            if (IsNewRecord)
            {
                // Asignar el CompanyId del padre antes de recolectar cambios
                CompanyId = CompanyIdBeforeNew;
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
                ? await _companyLocationService.CreateAsync(query, variables)
                : await _companyLocationService.UpdateAsync(query, variables);
        }

        protected override async Task PublishMessageAsync(CompanyLocationGraphQLModel result)
        {
            if (IsNewRecord)
            {
                await MasterContext.Context.EventAggregator.PublishOnUIThreadAsync(
                    new CompanyLocationCreateMessage { CreatedCompanyLocation = result });
            }
            else
            {
                await MasterContext.Context.EventAggregator.PublishOnUIThreadAsync(
                    new CompanyLocationUpdateMessage { UpdatedCompanyLocation = result });
            }
        }

        #endregion
    }
}
