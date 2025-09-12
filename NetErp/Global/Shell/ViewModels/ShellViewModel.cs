using Caliburn.Micro;
using Common.Interfaces;
using NetErp.Global.MainMenu.ViewModels;
using NetErp.Helpers.Messages;
using NetErp.Helpers.Services;
using NetErp.Login.ViewModels;
using static NetErp.Login.ViewModels.CompanySelectionViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Models.Login;

namespace NetErp.Global.Shell.ViewModels
{
    public class ShellViewModel : Conductor<IScreen>.Collection.OneActive, IHandle<NetworkStatusChangedMessage>, IHandle<LoginSuccessMessage>, IHandle<CompanySelectedMessage>, IHandle<LogoutMessage>, IHandle<ReturnToCompanySelectionMessage>
    {
        private readonly INotificationService _notificationService;
        private readonly IBackgroundQueueService _backgroundService;
        private readonly IEventAggregator _eventAggregator;
        private readonly ILoginService _loginService;
        private readonly ISQLiteEmailStorageService _emailStorageService;

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
            ISQLiteEmailStorageService emailStorageService)
        {
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _backgroundService = backgroundService ?? throw new ArgumentNullException(nameof(backgroundService));
            _loginService = loginService ?? throw new ArgumentNullException(nameof(loginService));
            _emailStorageService = emailStorageService ?? throw new ArgumentNullException(nameof(emailStorageService));
            
            _eventAggregator.SubscribeOnPublishedThread(this);
            Task.Run(() => ActivateMainMenuView());
        }

        public async Task ActivateMainMenuView()
        {
            //Lógica momentanea para cargar el Login o el menú principal y no hacer log a cada rato
            //if (Debugger.IsAttached)
            //{
            //    await ActivateItemAsync(MainMenuViewModel, new CancellationToken());
            //    return;
            //}

            // Crear LoginViewModel con Constructor Injection
            LoginViewModel loginViewModel = new LoginViewModel(_loginService, _notificationService, _eventAggregator, _emailStorageService);
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
            var companySelectionViewModel = new NetErp.Login.ViewModels.CompanySelectionViewModel(_notificationService, _eventAggregator, _loginService);
            companySelectionViewModel.Initialize(message.Account, message.Companies, message.AccessTicket);
            await ActivateItemAsync(companySelectionViewModel, cancellationToken);
        }

        public async Task HandleAsync(CompanySelectedMessage message, CancellationToken cancellationToken)
        {
            // Empresa seleccionada - navegar al MainMenu
            await ActivateItemAsync(MainMenuViewModel, cancellationToken);
        }

        public async Task HandleAsync(LogoutMessage message, CancellationToken cancellationToken)
        {
            // Logout - volver al login
            var loginViewModel = new LoginViewModel(_loginService, _notificationService, _eventAggregator, _emailStorageService);
            await ActivateItemAsync(loginViewModel, cancellationToken);
        }

        public async Task HandleAsync(ReturnToCompanySelectionMessage message, CancellationToken cancellationToken)
        {
            // Volver a la selección de empresa si tenemos los datos almacenados
            if (_currentAccount != null && _availableCompanies != null)
            {
                var companySelectionViewModel = new NetErp.Login.ViewModels.CompanySelectionViewModel(_notificationService, _eventAggregator, _loginService);
                companySelectionViewModel.Initialize(_currentAccount, _availableCompanies, _accessTicket);
                await ActivateItemAsync(companySelectionViewModel, cancellationToken);
            }
        }
    }
}
