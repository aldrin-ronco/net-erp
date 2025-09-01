using Caliburn.Micro;
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

        private LoginAccountGraphQLModel _currentAccount = new();
        private ObservableCollection<LoginOrganizationDTO> _organizationGroups = [];
        private LoginCompanyInfoDTO? _selectedCompany;

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
            IEventAggregator eventAggregator)
        {
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            
            DisplayName = "Selección de Empresa";
        }

        public void Initialize(LoginAccountGraphQLModel account, List<LoginCompanyGraphQLModel> companies)
        {
            CurrentAccount = account;
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
        }


        public async Task ContinueAsync()
        {
            if (!CanContinue) return;

            try
            {
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