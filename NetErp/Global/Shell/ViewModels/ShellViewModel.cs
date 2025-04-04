using Caliburn.Micro;
using NetErp.Global.MainMenu.ViewModels;
using NetErp.Helpers.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetErp.Global.Shell.ViewModels
{
    public class ShellViewModel : Conductor<IScreen>.Collection.OneActive, IHandle<NetworkStatusChangedMessage>
    {
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly IEventAggregator _eventAggregator;

        private MainMenuViewModel? _mainMenuViewModel;

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


        public ShellViewModel(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
            _eventAggregator.SubscribeOnPublishedThread(this);
            _notificationService = IoC.Get<Helpers.Services.INotificationService>();

            Task.Run(() => ActivateMainMenuView());
        }

        public async Task ActivateMainMenuView()
        {
            await ActivateItemAsync(MainMenuViewModel, new CancellationToken());
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
    }
}
