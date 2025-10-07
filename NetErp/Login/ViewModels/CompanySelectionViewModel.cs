using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Xpf.Core;
using Dictionaries;
using Extensions.Global;
using Models.Books;
using Models.Global;
using Models.Login;
using NetErp.Helpers.GraphQLQueryBuilder;
using NetErp.Helpers.Services;
using NetErp.Login.DTO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using static Dictionaries.BooksDictionaries;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Login.ViewModels
{
    public class CompanySelectionViewModel : Screen
    {
        private readonly INotificationService _notificationService;
        private readonly IEventAggregator _eventAggregator;
        private readonly ILoginService _loginService;
        private readonly IRepository<CompanyGraphQLModel> _companyService;
        private readonly IRepository<CountryGraphQLModel> _countryService;

        private LoginAccountGraphQLModel _currentAccount = new();
        private ObservableCollection<LoginOrganizationDTO> _organizationGroups = [];
        private ObservableCollection<LoginOrganizationDTO> _filteredOrganizationGroups = [];
        private LoginCompanyInfoDTO? _selectedCompany;
        private LoginTicketGraphQLModel _accessTicket = new();
        private string _searchText = string.Empty;
        public LoginAccountGraphQLModel CurrentAccount
        {
            get { return _currentAccount; }
            set
            {
                if (_currentAccount != value)
                {
                    _currentAccount = value;
                    NotifyOfPropertyChange(nameof(CurrentAccount));
                    NotifyOfPropertyChange(nameof(WelcomeMessage));
                }
            }
        }

        public ObservableCollection<LoginOrganizationDTO> OrganizationGroups
        {
            get { return _organizationGroups; }
            set
            {
                if (_organizationGroups != value)
                {
                    _organizationGroups = value;
                    NotifyOfPropertyChange(nameof(OrganizationGroups));
                }
            }
        }

        public ObservableCollection<LoginOrganizationDTO> FilteredOrganizationGroups
        {
            get { return _filteredOrganizationGroups; }
            set
            {
                if (_filteredOrganizationGroups != value)
                {
                    _filteredOrganizationGroups = value;
                    NotifyOfPropertyChange(nameof(FilteredOrganizationGroups));
                }
            }
        }

        public string SearchText
        {
            get { return _searchText; }
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    NotifyOfPropertyChange(nameof(SearchText));
                    ApplyFilter();
                }
            }
        }

        public LoginCompanyInfoDTO? SelectedCompany
        {
            get { return _selectedCompany; }
            set
            {
                if (_selectedCompany != value)
                {
                    _selectedCompany = value;
                    NotifyOfPropertyChange(nameof(SelectedCompany));
                    NotifyOfPropertyChange(nameof(CanContinue));
                }
            }
        }

        private bool _isBusy;

        public bool IsBusy
        {
            get { return _isBusy; }
            set 
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    NotifyOfPropertyChange(nameof(IsBusy));
                    NotifyOfPropertyChange(nameof(CanContinue));
                }
            }
        }

        private string _busyContent;

        public string BusyContent
        {
            get { return _busyContent; }
            set 
            {
                if(_busyContent != value)
                {
                    _busyContent = value;
                    NotifyOfPropertyChange(nameof(BusyContent));
                }
            }
        }



        public bool CanContinue => SelectedCompany != null && !IsBusy;

        public string WelcomeMessage => $"Bienvenido {CurrentAccount.FirstName} {CurrentAccount.FirstLastName}";

        public CompanySelectionViewModel(
            INotificationService notificationService,
            IEventAggregator eventAggregator,
            ILoginService loginService,
            IRepository<CompanyGraphQLModel> companyService,
            IRepository<CountryGraphQLModel> countryService)
        {
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _loginService = loginService ?? throw new ArgumentNullException(nameof(loginService));
            _companyService = companyService ?? throw new ArgumentNullException(nameof(companyService));
            _countryService = countryService ?? throw new ArgumentNullException(nameof(countryService));

            DisplayName = "Selección de Empresa";
        }

        public void Initialize(LoginAccountGraphQLModel account, List<LoginCompanyGraphQLModel> companies, LoginTicketGraphQLModel accessTicket)
        {
            CurrentAccount = account;
            _accessTicket = accessTicket;
            GroupCompaniesByOrganization(companies);
        }

        private void GroupCompaniesByOrganization(List<LoginCompanyGraphQLModel> companies)
        {
            var groupedCompanies = companies
                .GroupBy(c => new { 
                    c.Company.License.Organization.Id, 
                    c.Company.License.Organization.Name 
                })
                .Select(g => new LoginOrganizationDTO
                {
                    OrganizationId = g.Key.Id,
                    OrganizationName = g.Key.Name,
                    Companies = new ObservableCollection<LoginCompanyInfoDTO>(
                        g.Select(company => 
                        {
                            var dto = new LoginCompanyInfoDTO
                            {
                                CompanyId = company.Company.Id,
                                CompanyName = company.Company.FullName,
                                Role = company.Role,
                                OrganizationName = g.Key.Name,
                                OriginalData = company
                            };
                            dto.OnSelectionChanged = (selected) => SelectedCompany = selected;
                            return dto;
                        })
                    )
                })
                .OrderBy(g => g.OrganizationName)
                .ToList();

            OrganizationGroups = new ObservableCollection<LoginOrganizationDTO>(groupedCompanies);
            FilteredOrganizationGroups = new ObservableCollection<LoginOrganizationDTO>(groupedCompanies);
        }

        private void ApplyFilter()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                FilteredOrganizationGroups = new ObservableCollection<LoginOrganizationDTO>(OrganizationGroups);
                return;
            }

            var filteredGroups = new List<LoginOrganizationDTO>();

            foreach (var org in OrganizationGroups)
            {
                var filteredCompanies = org.Companies.Where(company =>
                    org.OrganizationName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    company.CompanyName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                ).ToList();

                if (filteredCompanies.Count != 0)
                {
                    var filteredOrg = new LoginOrganizationDTO
                    {
                        OrganizationId = org.OrganizationId,
                        OrganizationName = org.OrganizationName,
                        Companies = new ObservableCollection<LoginCompanyInfoDTO>(filteredCompanies)
                    };
                    filteredGroups.Add(filteredOrg);
                }
            }

            FilteredOrganizationGroups = new ObservableCollection<LoginOrganizationDTO>(filteredGroups);
        }


        public async Task ContinueAsync()
        {
            if (!CanContinue) return;

            try
            {
                IsBusy = true;
                CompanyGraphQLModel currentCompany;

                // Enviar temporalmente el database-id basado en la referencia seleccionada
                SessionInfo.PendingCompanyReference = SelectedCompany!.OriginalData.Company.Reference;

                if (string.IsNullOrEmpty(SessionInfo.SessionId))
                {
                    BusyContent = "Redimiendo ticket...";
                    LoginValidateTicketGraphQLModel result = await _loginService.RedeemTicketAsync(_accessTicket.Ticket);
                    if (!result.Success) 
                    { 
                        ThemedMessageBox.Show(text: $"No se pudo validar el ticket de acceso. \n\n {result.Message} \n\n Comunicate con soporte técnico.", title: "Error de acceso", messageBoxButtons: MessageBoxButton.OK, icon: MessageBoxImage.Error);
                        return;
                    }
                    SessionInfo.SessionId = result.SessionId;
                }

                BusyContent = "Verificando información...";
                //Para este punto sí o sí debe haber una empresa seleccionada
                CompanyGraphQLModel? resolvedCompany = await GetCompanyByReferenceAsync(SelectedCompany!.OriginalData.Company);

                if(resolvedCompany is null)
                {
                    BusyContent = "Dejando todo listo...";
                    UpsertResponseType<CompanyGraphQLModel> createdCompany = await CreateCompanyAsync(SelectedCompany!.OriginalData.Company);
                    if (!createdCompany.Success)
                    {
                        ThemedMessageBox.Show(text: $"No fue posible acceder a la compañía seleccionada. \n\n {createdCompany.Errors.ToUserMessage()} \n\n Intenta nuevamente o comunícate con soporte técnico.", title: "Error de acceso", messageBoxButtons: MessageBoxButton.OK, icon: MessageBoxImage.Error);
                        return;
                    }
                    currentCompany = createdCompany.Entity;
                }
                else
                {
                    currentCompany = resolvedCompany;
                }
                SessionInfo.CurrentCompany = currentCompany;
                SessionInfo.PendingCompanyReference = null;

                // Publicar mensaje de empresa seleccionada para navegación
                await _eventAggregator.PublishOnUIThreadAsync(new CompanySelectedMessage
                {
                    Account = CurrentAccount,
                    SelectedCompany = SelectedCompany!.OriginalData
                });

                _notificationService.ShowSuccess($"Accediendo a {SelectedCompany!.CompanyName}");
            }
            catch (Exception ex)
            {
                ThemedMessageBox.Show(text: $"Ha ocurrido un error. \n\n {ex.Message} \n\n Comunicate con soporte técnico.", title: "Error inexperado", messageBoxButtons: MessageBoxButton.OK, icon: MessageBoxImage.Error);
            }
            finally
            {
                // Limpia el pending; desde este punto el flujo superior establecerá CurrentCompany.
                SessionInfo.PendingCompanyReference = null;
                IsBusy = false;
            }
        }

        public string GetCompanyByReferenceQuery()
        {
            var fields = FieldSpec<PageType<CompanyGraphQLModel>>
            .Create()
            .SelectList(it => it.Entries, entries => entries
                .Field(e => e.Id)
                .Field(e => e.InsertedAt)
                .Field(e => e.UpdatedAt)
                .Field(e => e.Reference)
                .Field(e => e.Status)
                .Select(e => e.CompanyEntity, entity => entity
                    .Field(e => e.Id)
                    .Field(e => e.BusinessName)
                    .Field(e => e.IdentificationNumber)))
            .Build();

            var parameters = new GraphQLQueryParameter("filters", "CompanyFilters");

            var fragment = new GraphQLQueryFragment("companiesPage", [parameters], fields, "PageResponse");

            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery();
        }

        public async Task<CompanyGraphQLModel?> GetCompanyByReferenceAsync(LoginCompanyInfoGraphQLModel company)
        {
            try
            {
                if (string.IsNullOrEmpty(company.Reference)) throw new Exception($"Company reference is null or empty");

                dynamic variables = new ExpandoObject();
                variables.filters = new ExpandoObject();
                variables.pageResponseFilters = new ExpandoObject();
                variables.pageResponseFilters.reference = company.Reference;

                string query = GetCompanyByReferenceQuery();

                PageType<CompanyGraphQLModel> response = await _companyService.GetPageAsync(query, variables);

                if (response.Entries.Count > 1) throw new Exception("Company reference must be unique");

                return response.Entries.FirstOrDefault();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public string GetCreateCompanyQuery()
        {
            var fields = FieldSpec<UpsertResponseType<CompanyGraphQLModel>>
            .Create()
            .Select(selector: f => f.Entity, overrideName: "company", alias: "entity", nested: entity => entity
                .Field(e => e.Id)
                .Field(e => e.InsertedAt)
                .Field(e => e.UpdatedAt)
                .Field(e => e.Reference)
                .Field(e => e.Status)
                .Select(f => f.CompanyEntity, sq => sq
                    .Field(e => e.Id)
                    .Field(e => e.BusinessName)
                    .Field(e => e.IdentificationNumber)))
            .Field(e => e.Success)
            .Field(e => e.Message)
            .SelectList(f => f.Errors, sq => sq
                .Field(f => f.Field)
                .Field(f => f.Message))
            .Build();

            var parameters = new GraphQLQueryParameter("input", "CreateCompanyInput!");

            var fragment = new GraphQLQueryFragment("createCompany", [parameters], fields, "CreateResponse");

            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }

        public async Task<UpsertResponseType<CompanyGraphQLModel>> CreateCompanyAsync(LoginCompanyInfoGraphQLModel company)
        {
            try
            {
                CompanyContextData response = await GetCompanyContextDataAsync(cityCode: company.City.Code, departmentCode: company.Department.Code, countryCode: company.Country.Code, identificationTypeCode: company.IdentificationType.Code);

                //bloque de validación
                if (response.Country is null) throw new Exception($"No country found with code {company.Country.Code}");
                if(response.Department is null) throw new Exception($"No department found with code {company.Country.Code} - {company.Department.Code}");
                if(response.City is null) throw new Exception($"No city found with code {company.Country.Code} - {company.Department.Code} - {company.City.Code}");
                if(response.IdentificationType is null) throw new Exception($"No identification type found with code {company.IdentificationType.Code}");

                dynamic variables = new ExpandoObject();
                variables.createResponseInput = new ExpandoObject();
                variables.createResponseInput.reference = company.Reference;
                variables.createResponseInput.status = company.Status;

                variables.createResponseInput.accountingEntity = new ExpandoObject();
                variables.createResponseInput.accountingEntity.address = company.Address;
                variables.createResponseInput.accountingEntity.businessName = company.BusinessName;
                variables.createResponseInput.accountingEntity.captureType = company.CaptureType;
                variables.createResponseInput.accountingEntity.firstLastName = company.FirstLastName;
                variables.createResponseInput.accountingEntity.firstName = company.FirstName;
                variables.createResponseInput.accountingEntity.identificationNumber = company.IdentificationNumber;
                variables.createResponseInput.accountingEntity.middleLastName = company.MiddleLastName;
                variables.createResponseInput.accountingEntity.middleName = company.MiddleName;
                variables.createResponseInput.accountingEntity.primaryCellPhone = company.PrimaryCellPhone;
                variables.createResponseInput.accountingEntity.secondaryCellPhone = company.SecondaryCellPhone;
                variables.createResponseInput.accountingEntity.primaryPhone = company.PrimaryPhone;
                variables.createResponseInput.accountingEntity.secondaryPhone = company.SecondaryPhone;
                variables.createResponseInput.accountingEntity.regime = company.Regime;
                variables.createResponseInput.accountingEntity.tradeName = company.TradeName;
                variables.createResponseInput.accountingEntity.verificationDigit = company.VerificationDigit;
                variables.createResponseInput.accountingEntity.countryId = response.Country.Id;
                variables.createResponseInput.accountingEntity.departmentId = response.Department.Id;
                variables.createResponseInput.accountingEntity.cityId = response.City.Id; 
                variables.createResponseInput.accountingEntity.identificationTypeId = response.IdentificationType.Id;


                string query = GetCreateCompanyQuery();

                UpsertResponseType<CompanyGraphQLModel> result = await _companyService.CreateAsync<UpsertResponseType<CompanyGraphQLModel>>(query, variables);

                return result;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public string GetCompanyContextDataQuery()
        {
            var cityFields = FieldSpec<CityGraphQLModel>
            .Create()
            .Field(e => e.Id)
            .Field(e => e.Code).Build();

            var departmentFields = FieldSpec<DepartmentGraphQLModel>
                .Create()
                .Field(e => e.Id)
                .Field(e => e.Code).Build();

            var countryField = FieldSpec<CountryGraphQLModel>
                .Create()
                .Field(e => e.Id)
                .Field(e => e.Code).Build();

            var identificationTypeFields = FieldSpec<IdentificationTypeGraphQLModel>
                .Create()
                .Field(e => e.Id)
                .Field(e => e.Code).Build();

            var cityParameters = new List<GraphQLQueryParameter> { new("cityCode", "String!"), new("departmentCode", "String!"), new("countryCode", "String!") };
            var departmentParameters = new List<GraphQLQueryParameter> { new("departmentCode", "String!"), new("countryCode", "String!") };
            var countryParameters = new GraphQLQueryParameter("code", "String!");
            var identificationTypeParameters = new GraphQLQueryParameter("code", "String!");

            var cityFragment = new GraphQLQueryFragment("cityByCodes", cityParameters, cityFields, "City");
            var departmentFragment = new GraphQLQueryFragment("departmentByCodes", departmentParameters, departmentFields, "Department");
            var countryFragment = new GraphQLQueryFragment("countryByCode", [countryParameters], countryField, "Country");
            var identificationTypeFragment = new GraphQLQueryFragment("identificationTypeByCode", [identificationTypeParameters], identificationTypeFields, "IdentificationType");

            var builder = new GraphQLQueryBuilder([cityFragment, departmentFragment, countryFragment, identificationTypeFragment]);

            return builder.GetQuery();
        }

        public async Task<CompanyContextData> GetCompanyContextDataAsync(string cityCode, string departmentCode, string countryCode, string identificationTypeCode)
        {
            try
            {
                dynamic variables = new ExpandoObject();
                variables.cityCityCode = cityCode;
                variables.cityDepartmentCode = departmentCode;
                variables.cityCountryCode = countryCode;
                variables.departmentDepartmentCode = departmentCode;
                variables.departmentCountryCode = countryCode;
                variables.countryCode = countryCode;
                variables.identificationTypeCode = identificationTypeCode;
                string query = GetCompanyContextDataQuery();

                CompanyContextData response = await _countryService.GetDataContextAsync<CompanyContextData>(query, variables);

                return response;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public void SelectCompany(LoginCompanyInfoDTO company)
        {
            company.IsSelected = true;
            SelectedCompany = company;
        }

        public void OnCompanyDoubleClick(LoginCompanyInfoDTO company)
        {
            SelectCompany(company);
            _ = ContinueAsync();
        }

        public async Task LogOutAsync()
        {
            // Volver al login
            await _eventAggregator.PublishOnUIThreadAsync(new LogoutMessage());
        }
    }

    // Mensajes para comunicación
    public class CompanySelectedMessage
    {
        public LoginAccountGraphQLModel Account { get; set; } = new();
        public LoginCompanyGraphQLModel SelectedCompany { get; set; } = new();
    }

    public class LogoutMessage
    {
    }

    public class ReturnToCompanySelectionMessage
    {
    }

    public class CompanyContextData
    {
        public CityGraphQLModel City { get; set; } = new();
        public DepartmentGraphQLModel Department { get; set; } = new();
        public CountryGraphQLModel Country { get; set; } = new();
        public IdentificationTypeGraphQLModel IdentificationType { get; set; } = new();
    }
}