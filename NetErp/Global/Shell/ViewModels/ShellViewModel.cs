using Caliburn.Micro;
using Common.Interfaces;
using NetErp.Global.MainMenu.ViewModels;
using NetErp.Helpers.Messages;
using NetErp.Helpers.Services;
using NetErp.Login.ViewModels;
using static NetErp.Login.ViewModels.CompanySelectionViewModel;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Models.Login;
using Models.Global;
using Common.Helpers;

namespace NetErp.Global.Shell.ViewModels
{
    public class ShellViewModel : Conductor<IScreen>.Collection.OneActive, IHandle<NetworkStatusChangedMessage>, IHandle<LoginSuccessMessage>, IHandle<CompanySelectedMessage>, IHandle<LogoutMessage>, IHandle<ReturnToCompanySelectionMessage>
    {
        private readonly INotificationService _notificationService;
        private readonly IBackgroundQueueService _backgroundService;
        private readonly IEventAggregator _eventAggregator;
        private readonly ILoginService _loginService;
        private readonly ISQLiteEmailStorageService _emailStorageService;
        private readonly IRepository<CompanyGraphQLModel> _companyService;
        private readonly IRepository<CountryGraphQLModel> _countryService;

        private MainMenuViewModel? _mainMenuViewModel;
        private LoginAccountGraphQLModel? _currentAccount;
        private List<LoginCompanyGraphQLModel>? _availableCompanies;
        private LoginTicketGraphQLModel? _accessTicket;

        public MainMenuViewModel MainMenuViewModel
        {
            get 
            {
                if (_mainMenuViewModel == null) this._mainMenuViewModel = new();
                return _mainMenuViewModel; 
            }
        }

        private bool _hasInternetConnection = true;
        public bool HasInternetConnection
        {
            get { return _hasInternetConnection; }
            set
            {
                if (_hasInternetConnection != value)
                {
                    _hasInternetConnection = value;
                    NotifyOfPropertyChange();
                    NotifyOfPropertyChange(nameof(InternetStatusText));
                    NotifyOfPropertyChange(nameof(ShowInternetWarning));
                }
            }
        }

        public string InternetStatusText => HasInternetConnection ? "Conectado" : "Sin conexión a Internet";
        public bool ShowInternetWarning => !HasInternetConnection;


        public ShellViewModel(
            IEventAggregator eventAggregator,
            INotificationService notificationService,
            IBackgroundQueueService backgroundService,
            ILoginService loginService,
            ISQLiteEmailStorageService emailStorageService,
            IRepository<CompanyGraphQLModel> companyService,
            IRepository<CountryGraphQLModel> countryService)
        {
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _backgroundService = backgroundService ?? throw new ArgumentNullException(nameof(backgroundService));
            _loginService = loginService ?? throw new ArgumentNullException(nameof(loginService));
            _emailStorageService = emailStorageService ?? throw new ArgumentNullException(nameof(emailStorageService));
            _companyService = companyService ?? throw new ArgumentNullException(nameof(companyService));
            _countryService = countryService ?? throw new ArgumentNullException(nameof(countryService));

            _eventAggregator.SubscribeOnPublishedThread(this);
            Task.Run(() => ActivateLoginView());
        }

        public async Task ActivateLoginView()
        {

            // Crear LoginViewModel con Constructor Injection
            LoginViewModel loginViewModel = new(_loginService, _notificationService, _eventAggregator, _emailStorageService);
            await ActivateItemAsync(loginViewModel, new CancellationToken());
        }

        public Task HandleAsync(NetworkStatusChangedMessage message, CancellationToken cancellationToken)
        {
            HasInternetConnection = message.IsConnected;

            if (!message.IsConnected)
            {
                _notificationService.ShowWarning(
                    "Las actualizaciones serán procesadas cuando se restablezca la conexión",
                    "Sin conexión a Internet");
            }
            else
            {
                _notificationService.ShowInfo("Conexión a Internet restablecida");
            }

            return Task.CompletedTask;
        }

        public async Task HandleAsync(LoginSuccessMessage message, CancellationToken cancellationToken)
        {
            // Almacenar los datos para poder volver a CompanySelection más tarde
            _currentAccount = message.Account;
            _availableCompanies = message.Companies;

            // Login exitoso - navegar a CompanySelection
            var companySelectionViewModel = new NetErp.Login.ViewModels.CompanySelectionViewModel(_notificationService, _eventAggregator, _loginService, _companyService, _countryService);
            companySelectionViewModel.Initialize(message.Account, message.Companies, message.AccessTicket);
            await ActivateItemAsync(companySelectionViewModel, cancellationToken);
        }

        public async Task HandleAsync(CompanySelectedMessage message, CancellationToken cancellationToken)
        {
            // Empresa seleccionada - navegar al MainMenu
            // Los caches individuales se cargarán de forma lazy cuando se necesiten
            await ActivateItemAsync(MainMenuViewModel, cancellationToken);
        }

        public async Task HandleAsync(LogoutMessage message, CancellationToken cancellationToken)
        {
            // Logout - volver al login
            // Los caches individuales se limpian cuando se cierra la aplicación o cambia de compañía
            SessionInfo.SessionId = string.Empty;
            var loginViewModel = new LoginViewModel(_loginService, _notificationService, _eventAggregator, _emailStorageService);
            await ActivateItemAsync(loginViewModel, cancellationToken);
        }

        public async Task HandleAsync(ReturnToCompanySelectionMessage message, CancellationToken cancellationToken)
        {
            // Volver a la selección de empresa si tenemos los datos almacenados
            if (_currentAccount != null && _availableCompanies != null)
            {
                var companySelectionViewModel = new NetErp.Login.ViewModels.CompanySelectionViewModel(_notificationService, _eventAggregator, _loginService, _companyService, _countryService);
                companySelectionViewModel.Initialize(_currentAccount, _availableCompanies, _accessTicket);
                await ActivateItemAsync(companySelectionViewModel, cancellationToken);
            }
        }

        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            // Desuscribirse del EventAggregator para evitar memory leaks
            _eventAggregator.Unsubscribe(this);
            return base.OnDeactivateAsync(close, cancellationToken);
        }
    }
}
