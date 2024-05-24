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
using NetErp.Billing.Customers.ViewModels;
using NetErp.Billing.Sellers.ViewModels;
using NetErp.Books.AccountingAccounts.ViewModels;
using NetErp.Books.AccountingEntities.ViewModels;
using NetErp.Books.AccountingSources.ViewModels;
using NetErp.Books.Reports.AuxiliaryBook.ViewModels;
using NetErp.Suppliers.Suppliers.ViewModels;
using Ninject;
using Services.Books.DAL.PostgreSQL;


namespace NetErp.Global.MainMenu.ViewModels
{
    public class MainMenuViewModel: Conductor<IScreen>.Collection.OneActive
    {


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

        }


        public async Task OpenAccountingAccounts()
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
        public async void OpenAccountingEntities()
        {
            try
            {
                AccountingEntityViewModel instance = IoC.Get<AccountingEntityViewModel>();
                instance.DisplayName = "Administración de terceros";
                await ActivateItemAsync(instance, new CancellationToken());
                int MyNewIndex = Items.IndexOf(instance);
                if (MyNewIndex >= 0) SelectedIndex = MyNewIndex;
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                _ = Application.Current.Dispatcher.Invoke(() => DXMessageBox.Show($"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Message}", "Atención !", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                _ = DXMessageBox.Show(ex.Message, "Atencion !", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public async Task OpenCustomer()
        {
            try
            {
                CustomerViewModel instance = IoC.Get<CustomerViewModel>();
                instance.DisplayName = "Administración de clientes";
                await ActivateItemAsync(instance, new CancellationToken());
                int MyNewIndex = Items.IndexOf(instance);
                if (MyNewIndex >= 0) SelectedIndex = MyNewIndex;
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                _ = Application.Current.Dispatcher.Invoke(() => DXMessageBox.Show($"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Message}", "Atención !", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                _ = DXMessageBox.Show(ex.Message, "Atencion !", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public async Task OpenSupplier()
        {
            try
            {
                SupplierViewModel instance = IoC.Get<SupplierViewModel>();
                instance.DisplayName = "Administración de proveedores";
                await ActivateItemAsync(instance, new CancellationToken());
                int MyNewIndex = Items.IndexOf(instance);
                if (MyNewIndex >= 0) SelectedIndex = MyNewIndex;
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                _ = Application.Current.Dispatcher.Invoke(() => DXMessageBox.Show($"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Message}", "Atención !", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                _ = DXMessageBox.Show(ex.Message, "Atencion !", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        public async Task OpenSeller()
        {
            try
            {
                SellerViewModel instance = IoC.Get<SellerViewModel>();
                instance.DisplayName = "Administración de vendedores";
                await ActivateItemAsync(instance, new CancellationToken());
                int MyNewIndex = Items.IndexOf(instance);
                if (MyNewIndex >= 0) SelectedIndex = MyNewIndex;
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                _ = Application.Current.Dispatcher.Invoke(() => DXMessageBox.Show($"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Message}", "Atención !", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                _ = DXMessageBox.Show(ex.Message, "Atencion !", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        public async void OpenAccountingSource()
        {
            try
            {
                AccountingSourceViewModel instance = IoC.Get<AccountingSourceViewModel>();
                instance.DisplayName = "Fuentes Contables";
                await ActivateItemAsync(instance, new CancellationToken());
                int MyNewIndex = Items.IndexOf(instance);
                if (MyNewIndex >= 0) SelectedIndex = MyNewIndex;
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                _ = Application.Current.Dispatcher.Invoke(() => DXMessageBox.Show($"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Message}", "Atención !", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                _ = DXMessageBox.Show(ex.Message, "Atencion !", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public async Task OpenAuxiliaryBook()
        {
            try
            {
                AuxiliaryBookViewModel instance = IoC.Get<AuxiliaryBookViewModel>();
                instance.DisplayName = "Libro Auxiliar";
                await ActivateItemAsync(instance, new CancellationToken());
                int MyNewIndex = Items.IndexOf(instance);
                if (MyNewIndex >= 0) SelectedIndex = MyNewIndex;
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                _ = Application.Current.Dispatcher.Invoke(() => DXMessageBox.Show($"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Message}", "Atención !", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                _ = DXMessageBox.Show(ex.Message, "Atencion !", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
