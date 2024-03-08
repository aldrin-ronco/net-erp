using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.WindowsUI.Navigation;
using GraphQL.Client.Http;
using Models.Books;
using NetErp.Books.AccountingAccounts.ViewModels;
using Ninject;
using Services.Books.DAL.PostgreSQL;


namespace NetErp.Global.MainMenu.ViewModels
{
    public class MainMenuViewModel: Conductor<IScreen>.Collection.OneActive
    {

        //public IGenericDataAccess<AccountingAccountGraphQLModel> AccountingAccountService = null!;

        private int selectedIndex;

        public int SelectedIndex
        {
            get => selectedIndex;
            set
            {
                if (selectedIndex != value)
                {
                    selectedIndex = value;
                    NotifyOfPropertyChange(nameof(SelectedIndex));
                }
            }
        }
        public MainMenuViewModel()
        {
            //AccountingAccountService = accountingAccountService;
        }

        //[Command]
        //public void OpenPlanMasterView()
        //{
        //    TabContainer option = new() { AllowHide = true, Header = "Plan Unico de Cuentas", Content = NinjectKernel.Kernel.Get<AccountPlanMasterViewModel>() };
        //    Tabs.Add(option);
        //}

        //public bool CanOpenPlanMasterView() => true;

        public async Task OpenOption1()
        {
            try
            {
                AccountPlanViewModel instance = new()
                {
                    DisplayName = "Plan Unico de Cuentas"
                };
                await ActivateItemAsync(instance, new CancellationToken());
                int MyNewIndex = Items.IndexOf(instance);
                if (MyNewIndex >= 0) SelectedIndex = MyNewIndex;
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                _ = Application.Current.Dispatcher.Invoke(() => DXMessageBox.Show(caption: "Atención!", messageBoxText: $"{this.GetType().Name}.{(currentMethod is null ? "OpenOption1" : currentMethod.Name.Between("<", ">"))} \r\n{exGraphQL.Message}", button: MessageBoxButton.OK, icon: MessageBoxImage.Error));
            }
            catch(Exception ex)
            {
                DXMessageBox.Show(ex.Message,"Atencion!", button: MessageBoxButton.OK, icon: MessageBoxImage.Information);
            }
        }
    }
}
