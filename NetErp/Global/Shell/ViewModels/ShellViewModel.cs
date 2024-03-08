using Caliburn.Micro;
using NetErp.Global.MainMenu.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetErp.Global.Shell.ViewModels
{
    public class ShellViewModel : Conductor<IScreen>.Collection.OneActive
    {

        private MainMenuViewModel? _mainMenuViewModel;

        public MainMenuViewModel MainMenuViewModel
        {
            get 
            {
                if (_mainMenuViewModel == null) this._mainMenuViewModel = new();
                return _mainMenuViewModel; 
            }
        }


        public ShellViewModel()
        {
            Task.Run(() => ActivateMainMenuView());
        }

        public async Task ActivateMainMenuView()
        {
            await ActivateItemAsync(MainMenuViewModel, new CancellationToken());
        }
    }
}
