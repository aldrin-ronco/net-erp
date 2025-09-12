using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using Models.Login;
using NetErp.Helpers.Services;
using NetErp.Login.DTO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace NetErp.Login.ViewModels
{
    public class CompanySelectionViewModel : Screen
    {
        private readonly INotificationService _notificationService;
        private readonly IEventAggregator _eventAggregator;
        private readonly ILoginService _loginService;

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
                    NotifyOfPropertyChange();
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
                    NotifyOfPropertyChange();
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
                    NotifyOfPropertyChange();
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
                    NotifyOfPropertyChange();
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
                    NotifyOfPropertyChange();
                    NotifyOfPropertyChange(nameof(CanContinue));
                }
            }
        }

        public bool CanContinue => SelectedCompany != null;

        public string WelcomeMessage => $"Bienvenido {CurrentAccount.FirstName} {CurrentAccount.FirstLastName}";

        public CompanySelectionViewModel(
            INotificationService notificationService,
            IEventAggregator eventAggregator,
            ILoginService loginService)
        {
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _loginService = loginService ?? throw new ArgumentNullException(nameof(loginService));
            
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
                                CompanyName = company.Company.Name,
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
                if (string.IsNullOrEmpty(SessionInfo.SessionId))
                {
                    LoginValidateTicketGraphQLModel result = await _loginService.RedeemTicketAsync(_accessTicket.Ticket);
                    if (!result.Success) 
                    { 
                        _notificationService.ShowError($"No se pudo validar el ticket de acceso. \n\n {result.Message} \n\n comunicate con soporte técnico.", "Error de acceso");
                        return;
                    }
                    SessionInfo.SessionId = result.SessionId;
                    //Para este punto sí o sí debe haber una empresa seleccionada
                    SessionInfo.CurrentCompany = SelectedCompany!.OriginalData.Company;
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
                _notificationService.ShowError($"Error: {ex.Message}", "Error de acceso");
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
}