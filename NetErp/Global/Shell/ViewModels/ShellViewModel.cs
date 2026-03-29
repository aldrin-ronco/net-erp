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
using NetErp.Helpers.Cache;
using NetErp.Helpers.GraphQLQueryBuilder;

namespace NetErp.Global.Shell.ViewModels
{
    public class ShellViewModel : Conductor<IScreen>.Collection.OneActive, IHandle<NetworkStatusChangedMessage>, IHandle<LoginSuccessMessage>, IHandle<CompanySelectedMessage>, IHandle<LogoutMessage>, IHandle<ReturnToCompanySelectionMessage>
    {
        private readonly INotificationService _notificationService;
        private readonly IBackgroundQueueService _backgroundService;
        private readonly IEventAggregator _eventAggregator;
        private readonly ILoginService _loginService;
        private readonly ICompanySeedService _companySeedService;
        private readonly ISQLiteEmailStorageService _emailStorageService;
        private readonly IRepository<CompanyGraphQLModel> _companyService;
        private readonly IRepository<CountryGraphQLModel> _countryService;
        private readonly IRepository<GlobalConfigGraphQLModel> _globalConfigService;
        private readonly IEnumerable<IEntityCache> _entityCaches;
        private readonly IAuthApiClient _authApiClient;
        private readonly IAdminRecentCompanyService _recentCompanyService;

        private MainMenuViewModel? _mainMenuViewModel;
        private SystemAccountGraphQLModel? _currentAccount;
        private List<LoginCompanyGraphQLModel>? _availableCompanies;
        private LoginTicketGraphQLModel? _accessTicket;

        public MainMenuViewModel MainMenuViewModel
        {
            get
            {
                if (_mainMenuViewModel == null) this._mainMenuViewModel = IoC.Get<MainMenuViewModel>();
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
            ICompanySeedService companySeedService,
            ISQLiteEmailStorageService emailStorageService,
            IRepository<CompanyGraphQLModel> companyService,
            IRepository<CountryGraphQLModel> countryService,
            IRepository<GlobalConfigGraphQLModel> globalConfigService,
            IEnumerable<IEntityCache> entityCaches,
            IAuthApiClient authApiClient,
            IAdminRecentCompanyService recentCompanyService)
        {
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _backgroundService = backgroundService ?? throw new ArgumentNullException(nameof(backgroundService));
            _loginService = loginService ?? throw new ArgumentNullException(nameof(loginService));
            _companySeedService = companySeedService ?? throw new ArgumentNullException(nameof(companySeedService));
            _emailStorageService = emailStorageService ?? throw new ArgumentNullException(nameof(emailStorageService));
            _companyService = companyService ?? throw new ArgumentNullException(nameof(companyService));
            _countryService = countryService ?? throw new ArgumentNullException(nameof(countryService));
            _globalConfigService = globalConfigService ?? throw new ArgumentNullException(nameof(globalConfigService));
            _entityCaches = entityCaches ?? throw new ArgumentNullException(nameof(entityCaches));
            _authApiClient = authApiClient ?? throw new ArgumentNullException(nameof(authApiClient));
            _recentCompanyService = recentCompanyService ?? throw new ArgumentNullException(nameof(recentCompanyService));

            _eventAggregator.SubscribeOnPublishedThread(this);
            _ = Task.Run(ActivateLoginViewAsync);
        }

        public async Task ActivateLoginViewAsync()
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
            var companySelectionViewModel = new NetErp.Login.ViewModels.CompanySelectionViewModel(_notificationService, _eventAggregator, _loginService, _companySeedService, _companyService, _countryService, _authApiClient, _recentCompanyService);
            companySelectionViewModel.Initialize(message.Account, message.Companies, message.AccessTicket);
            await ActivateItemAsync(companySelectionViewModel, cancellationToken);
        }

        public async Task HandleAsync(CompanySelectedMessage message, CancellationToken cancellationToken)
        {
            // Empresa seleccionada - cargar configuración global AWS y navegar al MainMenu
            await LoadGlobalAwsConfigAsync();
            // Los caches individuales se cargan bajo demanda (lazy loading) via EnsureLoadedAsync()
            await ActivateItemAsync(MainMenuViewModel, cancellationToken);
        }

        public async Task HandleAsync(LogoutMessage message, CancellationToken cancellationToken)
        {
            // Logout - limpiar todos los caches y sesión, volver al login
            ClearAllCaches();
            _mainMenuViewModel = null;
            SessionInfo.CurrentCompany = null;
            SessionInfo.DefaultAwsS3Config = null;
            SessionInfo.DatabaseId = string.Empty;
            SessionInfo.LoginCompanyId = 0;
            SessionInfo.IsSystemAdmin = false;
            SessionInfo.SessionId = string.Empty;
            var loginViewModel = new LoginViewModel(_loginService, _notificationService, _eventAggregator, _emailStorageService);
            await ActivateItemAsync(loginViewModel, cancellationToken);
        }

        public async Task HandleAsync(ReturnToCompanySelectionMessage message, CancellationToken cancellationToken)
        {
            // Limpiar todos los caches y la empresa actual al salir
            ClearAllCaches();
            _mainMenuViewModel = null;
            SessionInfo.CurrentCompany = null;
            SessionInfo.DefaultAwsS3Config = null;
            SessionInfo.DatabaseId = string.Empty;
            SessionInfo.LoginCompanyId = 0;

            // Volver a la selección de empresa si tenemos los datos almacenados
            if (_currentAccount != null && _availableCompanies != null)
            {
                var companySelectionViewModel = new NetErp.Login.ViewModels.CompanySelectionViewModel(_notificationService, _eventAggregator, _loginService, _companySeedService, _companyService, _countryService, _authApiClient, _recentCompanyService);
                companySelectionViewModel.Initialize(_currentAccount, _availableCompanies, _accessTicket);
                await ActivateItemAsync(companySelectionViewModel, cancellationToken);
            }
        }

        private async Task LoadGlobalAwsConfigAsync()
        {
            var fields = FieldSpec<GlobalConfigGraphQLModel>
                .Create()
                .Field(f => f.Id)
                .Select(f => f.DefaultAwsS3Config, nested: aws => aws
                    .Field(a => a.Id)
                    .Field(a => a.AccessKey)
                    .Field(a => a.SecretKey)
                    .Field(a => a.Region)
                    .Field(a => a.Description))
                .Build();

            var fragment = new GraphQLQueryFragment("globalConfig", [], fields, "SingleItemResponse");
            var query = new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.QUERY);
            var config = await _globalConfigService.GetSingleItemAsync(query, new { });
            if (config is not null) SessionInfo.DefaultAwsS3Config = config.DefaultAwsS3Config;
        }

        private void ClearAllCaches()
        {
            foreach (var cache in _entityCaches)
            {
                cache.Clear();
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
