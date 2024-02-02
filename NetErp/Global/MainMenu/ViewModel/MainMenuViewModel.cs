using System.Collections.ObjectModel;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Xpf.WindowsUI.Navigation;
using Models.Books;
using NetErp.Books.AccountingAccounts.ViewModel;
using NetErp.IoC;
using Ninject;
using Services.Books.DAL.PostgreSQL;


namespace NetErp.Global.MainMenu.ViewModel
{
    public class MainMenuViewModel: ViewModelBase
    {

        //public INavigationService NavigationService { get;}

        private ObservableCollection<TabContainer> _Tabs = [];
        public ObservableCollection<TabContainer> Tabs
        {
            get { return _Tabs; }
            set { SetValue(ref _Tabs, value); }
        }

        public MainMenuViewModel()
        {
            //NavigationService = navigationService;
        }


        //=> NavigationService.Navigate("AccountPlanMasterView", null, this);
        [Command]
        public void OpenPlanMasterView()
        {
            TabContainer option = new() { AllowHide = true, Header = "Plan Unico de Cuentas", Content = NinjectKernel.Kernel.Get<AccountPlanMasterViewModel>() };
            Tabs.Add(option);
        }

        public bool CanOpenPlanMasterView() => true;
    }
}
