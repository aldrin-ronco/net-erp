using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
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
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using static Dictionaries.BooksDictionaries;
using static Models.Global.GraphQLResponseTypes;
using INotificationService = NetErp.Helpers.Services.INotificationService;
using System.Windows;

namespace NetErp.Login.ViewModels
{
    public class CompanySelectionViewModel : Screen
    {
        private readonly INotificationService _notificationService;
        private readonly IEventAggregator _eventAggregator;
        private readonly ILoginService _loginService;
        private readonly ICompanySeedService _companySeedService;
        private readonly IRepository<CompanyGraphQLModel> _companyService;
        private readonly IRepository<CountryGraphQLModel> _countryService;
        private readonly IAuthApiClient _authApiClient;
        private readonly IAdminRecentCompanyService _recentCompanyService;
        private readonly NetErp.Helpers.DebouncedAction _adminSearchDebounce = new();
        private bool _showingRecents;

        private LoginTicketGraphQLModel _accessTicket = new();

        public SystemAccountGraphQLModel CurrentAccount
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(CurrentAccount));
                    NotifyOfPropertyChange(nameof(WelcomeMessage));
                }
            }
        } = new();

        public ObservableCollection<LoginOrganizationDTO> OrganizationGroups
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(OrganizationGroups));
                }
            }
        } = [];

        public ObservableCollection<LoginOrganizationDTO> FilteredOrganizationGroups
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(FilteredOrganizationGroups));
                    NotifyOfPropertyChange(nameof(ShowAdminEmptyState));
                    NotifyOfPropertyChange(nameof(ShowRecentLabel));
                }
            }
        } = [];

        public string SearchText
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SearchText));
                    ApplyFilter();
                }
            }
        } = string.Empty;

        public LoginCompanyInfoDTO? SelectedCompany
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedCompany));
                    NotifyOfPropertyChange(nameof(CanContinue));
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
                    NotifyOfPropertyChange(nameof(CanContinue));
                }
            }
        }

        public string BusyContent
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(BusyContent));
                }
            }
        } = string.Empty;



        public bool CanContinue => SelectedCompany != null && !IsBusy;

        public string WelcomeMessage => $"Bienvenido {CurrentAccount.FirstName} {CurrentAccount.FirstLastName}";

        public bool IsAdminMode
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IsAdminMode));
                    NotifyOfPropertyChange(nameof(IsRegularMode));
                    NotifyOfPropertyChange(nameof(SubtitleMessage));
                    NotifyOfPropertyChange(nameof(ShowAdminEmptyState));
                }
            }
        }

        public bool ShowAdminEmptyState => IsAdminMode && FilteredOrganizationGroups.Count == 0;
        public bool ShowRecentLabel => IsAdminMode && _showingRecents && FilteredOrganizationGroups.Count > 0;

        public bool IsRegularMode => !IsAdminMode;

        public string SubtitleMessage => IsAdminMode
            ? "Busca la empresa a la que deseas acceder"
            : "Selecciona la empresa con la que deseas trabajar";

        public string AdminSearchText
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(AdminSearchText));
                    if (string.IsNullOrEmpty(value) || value.Length >= 3)
                    {
                        _ = _adminSearchDebounce.RunAsync(SearchCompaniesAsync);
                    }
                }
            }
        } = string.Empty;

        public CompanySelectionViewModel(
            INotificationService notificationService,
            IEventAggregator eventAggregator,
            ILoginService loginService,
            ICompanySeedService companySeedService,
            IRepository<CompanyGraphQLModel> companyService,
            IRepository<CountryGraphQLModel> countryService,
            IAuthApiClient authApiClient,
            IAdminRecentCompanyService recentCompanyService)
        {
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _loginService = loginService ?? throw new ArgumentNullException(nameof(loginService));
            _companySeedService = companySeedService ?? throw new ArgumentNullException(nameof(companySeedService));
            _companyService = companyService ?? throw new ArgumentNullException(nameof(companyService));
            _countryService = countryService ?? throw new ArgumentNullException(nameof(countryService));
            _authApiClient = authApiClient ?? throw new ArgumentNullException(nameof(authApiClient));
            _recentCompanyService = recentCompanyService ?? throw new ArgumentNullException(nameof(recentCompanyService));

            DisplayName = "Selección de Empresa";
        }

        public void Initialize(SystemAccountGraphQLModel account, List<LoginCompanyGraphQLModel> companies, LoginTicketGraphQLModel accessTicket)
        {
            CurrentAccount = account;
            _accessTicket = accessTicket;
            IsAdminMode = account.IsSystemAdmin;

            if (!IsAdminMode)
            {
                GroupCompaniesByOrganization(companies);
                _ = LoadAccessDatesAsync();
            }
            else
            {
                _ = LoadRecentCompaniesAsync();
            }
        }

        private void GroupCompaniesByOrganization(List<LoginCompanyGraphQLModel> companies)
        {
            var groupedCompanies = companies
                .GroupBy(c => new { 
                    c.Company.Organization.Id, 
                    c.Company.Organization.Name 
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
                                OriginalData = company,
                                OnSelectionChanged = (selected) => SelectedCompany = selected
                            };
                            return dto;
                        })
                    )
                })
                .OrderBy(g => g.OrganizationName)
                .ToList();

            OrganizationGroups = new ObservableCollection<LoginOrganizationDTO>(groupedCompanies);
            FilteredOrganizationGroups = new ObservableCollection<LoginOrganizationDTO>(groupedCompanies);

            //if (Debugger.IsAttached)
            //{
            //    FilteredOrganizationGroups.First(x => x.OrganizationId == 36).Companies.First(f => f.CompanyId == 15).IsSelected = true;
            //    _ = ContinueAsync();
            //}
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
            SelectedCompany = null;
        }

        private ICommand? _continueCommand;

        public ICommand ContinueCommand
        {
            get
            {
                _continueCommand ??= new AsyncCommand(ContinueAsync);
                return _continueCommand;
            }
        }

        public async Task ContinueAsync()
        {
            if (!CanContinue) return;

            try
            {
                IsBusy = true;
                CompanyGraphQLModel currentCompany;

                // Establecer database-id de la organización para los requests a la API principal
                SessionInfo.DatabaseId = SelectedCompany!.OriginalData.Company.Organization.DatabaseId;

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

                LoginCompanyInfoGraphQLModel loginCompany = SelectedCompany!.OriginalData.Company;
                bool needsSeeds;

                if (loginCompany.TenantCompanyId.HasValue)
                {
                    // Empresa ya existe en la API principal — usar el ID directamente
                    currentCompany = new CompanyGraphQLModel
                    {
                        Id = loginCompany.TenantCompanyId.Value,
                        CompanyEntity = new() { SearchName = loginCompany.SearchName }
                    };
                    needsSeeds = string.Equals(loginCompany.SeedStatus, "PENDING", StringComparison.OrdinalIgnoreCase);
                }
                else
                {
                    // Empresa nueva — crear en la API principal
                    BusyContent = "Dejando todo listo...";
                    UpsertResponseType<CompanyGraphQLModel> createdCompany = await CreateCompanyAsync(loginCompany);
                    if (!createdCompany.Success)
                    {
                        ThemedMessageBox.Show(text: $"No fue posible acceder a la compañía seleccionada. \n\n {createdCompany.Errors.ToUserMessage()} \n\n Intenta nuevamente o comunícate con soporte técnico.", title: "Error de acceso", messageBoxButtons: MessageBoxButton.OK, icon: MessageBoxImage.Error);
                        return;
                    }
                    currentCompany = createdCompany.Entity;
                    needsSeeds = string.Equals(loginCompany.SeedStatus, "PENDING", StringComparison.OrdinalIgnoreCase);
                }

                SessionInfo.CurrentCompany = currentCompany;
                SessionInfo.LoginCompanyId = SelectedCompany.OriginalData.Company.Id;
                SessionInfo.LoginAccountId = CurrentAccount.Id;
                SessionInfo.IsSystemAdmin = IsAdminMode;

                string companyJson = Newtonsoft.Json.JsonConvert.SerializeObject(loginCompany);
                await _recentCompanyService.SaveRecentCompanyAsync(
                    CurrentAccount.Id,
                    loginCompany.Id,
                    companyJson,
                    loginCompany.FullName,
                    loginCompany.Organization?.Name ?? "");

                // Actualizar snapshot del login para que ReturnToCompanySelection tenga datos correctos
                loginCompany.TenantCompanyId = currentCompany.Id;

                if (needsSeeds)
                {
                    BusyContent = "Configurando la empresa...";
                    var seedProgress = new Progress<string>(message => BusyContent = message);
                    CompanySeedResultModel seedResult = await _companySeedService.RunSeedsAsync(
                        SessionInfo.LoginCompanyId, seedProgress);

                    if (!seedResult.Success)
                    {
                        string errorDetail = seedResult.Errors.Count > 0
                            ? string.Join("\n", seedResult.Errors)
                            : seedResult.Message;
                        ThemedMessageBox.Show(
                            text: $"La configuración de la empresa presentó inconvenientes.\n\n{errorDetail}\n\nComunícate con soporte técnico.",
                            title: "Error de configuración",
                            messageBoxButtons: MessageBoxButton.OK,
                            icon: MessageBoxImage.Error);
                        return;
                    }
                    loginCompany.SeedStatus = "APPLIED";
                }
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
                ThemedMessageBox.Show(text: $"Ha ocurrido un error. \n\n {ex.GetErrorMessage()} \n\n Comunicate con soporte técnico.", title: "Error inexperado", messageBoxButtons: MessageBoxButton.OK, icon: MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _createCompanyQuery = new(() =>
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
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var fragment = new GraphQLQueryFragment("createCompany",
                [new("input", "CreateCompanyInput!")],
                fields, "CreateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        public async Task<UpsertResponseType<CompanyGraphQLModel>> CreateCompanyAsync(LoginCompanyInfoGraphQLModel company)
        {
            try
            {
                CompanyContextData response = await GetCompanyContextDataAsync(cityCode: company.City.Code, departmentCode: company.Department.Code, countryCode: company.Country.Code, identificationTypeCode: company.IdentificationType.Code, currencyCode: company.DefaultCurrency.Code);

                if(response.Country is null) throw new Exception($"No country found with code {company.Country.Code}");
                if(response.Department is null) throw new Exception($"No department found with code {company.Country.Code} - {company.Department.Code}");
                if(response.City is null) throw new Exception($"No city found with code {company.Country.Code} - {company.Department.Code} - {company.City.Code}");
                if(response.IdentificationType is null) throw new Exception($"No identification type found with code {company.IdentificationType.Code}");

                dynamic variables = new ExpandoObject();
                variables.createResponseInput = new ExpandoObject();
                variables.createResponseInput.reference = company.Reference;
                variables.createResponseInput.status = company.Status;
                variables.createResponseInput.defaultCurrencyId = response.Currency.Id;

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

                (GraphQLQueryFragment _, string query) = _createCompanyQuery.Value;

                UpsertResponseType<CompanyGraphQLModel> result = await _companyService.CreateAsync<UpsertResponseType<CompanyGraphQLModel>>(query, variables);

                return result;
            }
            catch (Exception)
            {

                throw;
            }
        }

        private static readonly Lazy<string> _companyContextDataQuery = new(() =>
        {
            var cityFields = FieldSpec<CityGraphQLModel>.Create()
                .Field(e => e.Id).Field(e => e.Code).Build();
            var departmentFields = FieldSpec<DepartmentGraphQLModel>.Create()
                .Field(e => e.Id).Field(e => e.Code).Build();
            var countryFields = FieldSpec<CountryGraphQLModel>.Create()
                .Field(e => e.Id).Field(e => e.Code).Build();
            var identificationTypeFields = FieldSpec<IdentificationTypeGraphQLModel>.Create()
                .Field(e => e.Id).Field(e => e.Code).Build();
            var currencyFields = FieldSpec<CurrencyGraphQLModel>.Create()
                .Field(e => e.Id).Field(e => e.Code).Build();

            var cityFragment = new GraphQLQueryFragment("cityByCodes",
                [new("cityCode", "String!"), new("departmentCode", "String!"), new("countryCode", "String!")],
                cityFields, "City");
            var departmentFragment = new GraphQLQueryFragment("departmentByCodes",
                [new("departmentCode", "String!"), new("countryCode", "String!")],
                departmentFields, "Department");
            var countryFragment = new GraphQLQueryFragment("countryByCode",
                [new("code", "String!")], countryFields, "Country");
            var identificationTypeFragment = new GraphQLQueryFragment("identificationTypeByCode",
                [new("code", "String!")], identificationTypeFields, "IdentificationType");
            var currencyFragment = new GraphQLQueryFragment("currencyByCode",
                [new("code", "String!")], currencyFields, "Currency");

            return new GraphQLQueryBuilder([cityFragment, departmentFragment, countryFragment, identificationTypeFragment, currencyFragment]).GetQuery();
        });

        public async Task<CompanyContextData> GetCompanyContextDataAsync(string cityCode, string departmentCode, string countryCode, string identificationTypeCode, string currencyCode)
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
                variables.currencyCode = currencyCode;
                string query = _companyContextDataQuery.Value;

                CompanyContextData response = await _countryService.GetDataContextAsync<CompanyContextData>(query, variables);

                return response;
            }
            catch (Exception)
            {

                throw;
            }
        }

        private async Task LoadAccessDatesAsync()
        {
            try
            {
                List<AdminRecentCompanyEntry> entries = await _recentCompanyService.GetRecentCompaniesAsync(CurrentAccount.Id);
                foreach (AdminRecentCompanyEntry entry in entries)
                {
                    LoginCompanyInfoDTO? dto = FilteredOrganizationGroups
                        .SelectMany(g => g.Companies)
                        .FirstOrDefault(c => c.CompanyId == entry.CompanyId);
                    if (dto != null)
                        dto.LastAccessedAt = entry.LastAccessedAt;
                }
            }
            catch { /* Non-critical — dates are informational only */ }
        }

        private async Task LoadRecentCompaniesAsync()
        {
            try
            {
                List<AdminRecentCompanyEntry> entries = await _recentCompanyService.GetRecentCompaniesAsync(CurrentAccount.Id);

                if (entries.Count == 0)
                {
                    FilteredOrganizationGroups = [];
                    _showingRecents = false;
                    return;
                }

                List<LoginCompanyGraphQLModel> recentCompanies = [];
                foreach (AdminRecentCompanyEntry entry in entries)
                {
                    try
                    {
                        LoginCompanyInfoGraphQLModel companyInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<LoginCompanyInfoGraphQLModel>(entry.CompanyData)!;
                        recentCompanies.Add(new LoginCompanyGraphQLModel
                        {
                            Company = companyInfo,
                            Role = "SYSTEM_ADMIN"
                        });
                    }
                    catch { /* Skip entries that fail to deserialize */ }
                }

                if (recentCompanies.Count > 0)
                {
                    _showingRecents = true;
                    GroupCompaniesByOrganization(recentCompanies);

                    foreach (AdminRecentCompanyEntry entry in entries)
                    {
                        LoginCompanyInfoDTO? dto = FilteredOrganizationGroups
                            .SelectMany(g => g.Companies)
                            .FirstOrDefault(c => c.CompanyId == entry.CompanyId);
                        if (dto != null)
                            dto.LastAccessedAt = entry.LastAccessedAt;
                    }
                }
                else
                {
                    _showingRecents = false;
                    FilteredOrganizationGroups = [];
                }
            }
            catch
            {
                _showingRecents = false;
                FilteredOrganizationGroups = [];
            }
        }

        public async Task SearchCompaniesAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(AdminSearchText))
                {
                    await LoadRecentCompaniesAsync();
                    return;
                }
                _showingRecents = false;

                IsBusy = true;
                BusyContent = "Buscando empresas...";

                GraphQL.GraphQLResponse<AdminCompanySearchResponse> result = await _authApiClient.SendQueryAsync<AdminCompanySearchResponse>(
                    new GraphQL.GraphQLRequest
                    {
                        Query = _adminSearchQuery.Value,
                        Variables = new
                        {
                            filters = new { matching = AdminSearchText.Trim() },
                            pagination = new { pageSize = 50 }
                        }
                    });

                if (result.Errors != null && result.Errors.Length > 0)
                {
                    ThemedMessageBox.Show(
                        text: $"Error al buscar empresas.\n\n{result.Errors[0].Message}",
                        title: "Error de búsqueda",
                        messageBoxButtons: MessageBoxButton.OK,
                        icon: MessageBoxImage.Error);
                    FilteredOrganizationGroups = [];
                    return;
                }

                List<LoginCompanyGraphQLModel> wrappedResults = [.. result.Data.CompaniesPage.Entries.Select(companyInfo =>
                    new LoginCompanyGraphQLModel
                    {
                        Company = companyInfo,
                        Role = "SYSTEM_ADMIN"
                    })];

                GroupCompaniesByOrganization(wrappedResults);
            }
            catch (Exception ex)
            {
                ThemedMessageBox.Show(
                    text: $"Error al buscar empresas.\n\n{ex.GetErrorMessage()}",
                    title: "Error",
                    messageBoxButtons: MessageBoxButton.OK,
                    icon: MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private static readonly Lazy<string> _adminSearchQuery = new(() => @"
            query ($filters: CompanyFilters, $pagination: Pagination) {
                companiesPage(filters: $filters, pagination: $pagination) {
                    entries {
                        id
                        reference
                        status
                        address
                        businessName
                        captureType
                        tenantCompanyId
                        seedStatus
                        defaultCurrency { code }
                        country { code }
                        department { code }
                        city { code }
                        firstLastName
                        firstName
                        fullName
                        identificationNumber
                        identificationType { code }
                        middleLastName
                        middleName
                        primaryCellPhone
                        secondaryCellPhone
                        primaryPhone
                        secondaryPhone
                        regime
                        searchName
                        tradeName
                        verificationDigit
                        telephonicInformation
                        updatedAt
                        insertedAt
                        organization {
                            id
                            name
                            databaseId
                        }
                    }
                    totalEntries
                }
            }");

        private class AdminCompanySearchResponse
        {
            public AdminCompanyPageGraphQLModel CompaniesPage { get; set; } = new();
        }

        private class AdminCompanyPageGraphQLModel
        {
            public List<LoginCompanyInfoGraphQLModel> Entries { get; set; } = [];
            public int TotalEntries { get; set; }
        }

        public void SelectCompany(LoginCompanyInfoDTO company)
        {
            company.IsSelected = true;
            SelectedCompany = company;
        }

        private ICommand? _onCompanyDoubleClickCommand;

        public ICommand OnCompanyDoubleClickCommand
        {
            get
            {
                _onCompanyDoubleClickCommand ??= new DelegateCommand<LoginCompanyInfoDTO>(OnCompanyDoubleClick);
                return _onCompanyDoubleClickCommand;
            }
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
        public SystemAccountGraphQLModel Account { get; set; } = new();
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
        public CurrencyGraphQLModel Currency { get; set; } = new();
    }
}