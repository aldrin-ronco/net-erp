using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Xpf.Controls.Internal;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.WindowsUI.Navigation;
using GraphQL.Client.Http;
using Models.Books;
using NetErp.Helpers.Messages;
using NetErp.Login.ViewModels;
using NetErp.Billing.CreditLimit.ViewModels;
using NetErp.Billing.Customers.ViewModels;
using NetErp.Billing.PriceList.ViewModels;
using NetErp.Billing.Sellers.ViewModels;
using NetErp.Billing.Zones.ViewModels;
using NetErp.Books.AccountingAccountGroups.ViewModels;
using NetErp.Books.AccountingAccounts.ViewModels;
using NetErp.Books.AccountingBooks.ViewModels;
using NetErp.Books.AccountingEntities.ViewModels;
using NetErp.Books.AccountingEntries.ViewModels;
using NetErp.Books.AccountingPresentations.ViewModels;
using NetErp.Books.AccountingSources.ViewModels;
using NetErp.Books.IdentificationTypes.ViewModels;
using NetErp.Books.Reports.AnnualIncomeStatement.ViewModels;
using NetErp.Books.Reports.AuxiliaryBook.ViewModels;
using NetErp.Books.Reports.DailyBook.ViewModels;
using NetErp.Books.Reports.EntityVsAccount.ViewModels;
using NetErp.Books.Reports.TestBalance.ViewModels;
using NetErp.Books.Reports.TestBalanceByEntity.ViewModels;
using NetErp.Books.WithholdingCertificateConfig.ViewModels;
using NetErp.Global.AuthorizationSequence.ViewModels;
using NetErp.Global.CostCenters.ViewModels;
using NetErp.Global.Email.ViewModels;
using NetErp.Global.Email.Views;
using NetErp.Global.Smtp.ViewModels;
using NetErp.Inventory.CatalogItems.ViewModels;
using NetErp.Inventory.ItemSizes.ViewModels;
using NetErp.Inventory.MeasurementUnits.ViewModels;
using NetErp.Suppliers.Suppliers.ViewModels;
using NetErp.Treasury.Concept.ViewModels;
using NetErp.Treasury.Masters.ViewModels;
using Ninject;
using Services.Books.DAL.PostgreSQL;
using NetErp.Global.AuthorizationSequence.ViewModels;
using NetErp.Books.Tax.ViewModels;
using NetErp.Global.Parameter.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using NetErp.Books.TaxCategory.ViewModels;


namespace NetErp.Global.MainMenu.ViewModels
{
    public class MainMenuViewModel : Conductor<IScreen>.Collection.OneActive
    {
        private readonly IEventAggregator _eventAggregator;
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
            _eventAggregator = IoC.Get<IEventAggregator>();
        }


        public async Task OpenAccountingAccounts()
        {
            try
            {
                AccountPlanViewModel instance = IoC.Get<AccountPlanViewModel>();
                 instance.DisplayName = "Plan único de cuentas";
                await ActivateItemAsync(instance, new CancellationToken());
                int MyNewIndex = Items.IndexOf(instance);
                if (MyNewIndex >= 0) SelectedIndex = MyNewIndex;
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "OpenOption1" : currentMethod.Name.Between("<", ">"))} \r\n{exGraphQL.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                ThemedMessageBox.Show(text: ex.Message, title: "Atencion!", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Information);
            }
        }
        public async Task OpenAccountingEntities()
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
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(text: $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Message}", title: "Atención !", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                _ = ThemedMessageBox.Show(text: ex.Message, title: "Atencion !", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Information);
            }
        }

        public async Task OpenCustomer()
        {
            try
            {
                CustomerViewModel instance = IoC.Get<CustomerViewModel>();
                instance.DisplayName = "Administración de clientes";
                await ActivateItemAsync(instance, new CancellationToken()).ConfigureAwait(false);
                int MyNewIndex = Items.IndexOf(instance);
                if (MyNewIndex >= 0) SelectedIndex = MyNewIndex;
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                _ = ThemedMessageBox.Show("Atencion !", ex.Message, MessageBoxButton.OK, MessageBoxImage.Information);
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
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                _ = ThemedMessageBox.Show("Atencion !", ex.Message, MessageBoxButton.OK, MessageBoxImage.Information);
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
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                _ = ThemedMessageBox.Show("Atencion !", ex.Message, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        public async Task OpenZone()
        {
            try
            {
                ZoneViewModel instance = IoC.Get<ZoneViewModel>();
                instance.DisplayName = "Administración de zonas de ventas";
                await ActivateItemAsync(instance, new CancellationToken());
                int MyNewIndex = Items.IndexOf(instance);
                if (MyNewIndex >= 0) SelectedIndex = MyNewIndex;
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                _ = ThemedMessageBox.Show("Atencion !", ex.Message, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        public async Task OpenAccountingPresentationAsync()
        {
            try
            {
                AccountingPresentationViewModel instance = IoC.Get<AccountingPresentationViewModel>();
                instance.DisplayName = "Presentaciones contables";
                await ActivateItemAsync(instance, new CancellationToken());
                int MyNewIndex = Items.IndexOf(instance);
                if (MyNewIndex >= 0) SelectedIndex = MyNewIndex;
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                _ = ThemedMessageBox.Show("Atencion !", ex.Message, MessageBoxButton.OK, MessageBoxImage.Information);
            }

        }
        public async Task OpenAccountingSource()
        {
            try
            {
                AccountingSourceViewModel instance = IoC.Get<AccountingSourceViewModel>();
                instance.DisplayName = "Fuentes contables";
                await ActivateItemAsync(instance, new CancellationToken());
                int MyNewIndex = Items.IndexOf(instance);
                if (MyNewIndex >= 0) SelectedIndex = MyNewIndex;
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                _ = ThemedMessageBox.Show("Atencion !", ex.Message, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public async Task OpenAuxiliaryBook()
        {
            try
            {
                AuxiliaryBookViewModel instance = IoC.Get<AuxiliaryBookViewModel>();
                instance.DisplayName = "Libro auxiliar";
                await ActivateItemAsync(instance, new CancellationToken());
                int MyNewIndex = Items.IndexOf(instance);
                if (MyNewIndex >= 0) SelectedIndex = MyNewIndex;
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                _ = ThemedMessageBox.Show("Atencion !", ex.Message, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        public async Task OpenTestBalance()
        {
            try
            {
                TestBalanceViewModel instance = IoC.Get<TestBalanceViewModel>();
                instance.DisplayName = "Balance de prueba";
                await ActivateItemAsync(instance, new CancellationToken());
                int MyNewIndex = Items.IndexOf(instance);
                if (MyNewIndex >= 0) SelectedIndex = MyNewIndex;
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show($"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Message}", "Atención !", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                _ = ThemedMessageBox.Show(ex.Message, "Atencion !", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        public async Task OpenDailyBookByEntity()
        {
            try
            {
                DailyBookByEntityViewModel instance = IoC.Get<DailyBookByEntityViewModel>();
                instance.DisplayName = "Libro diario por tercero";
                await ActivateItemAsync(instance, new CancellationToken());
                int MyNewIndex = Items.IndexOf(instance);
                if (MyNewIndex >= 0) SelectedIndex = MyNewIndex;
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                _ = ThemedMessageBox.Show("Atencion !", ex.Message, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        public async Task OpenAnnualIncomeStatement()
        {
            try
            {
                AnnualIncomeStatementViewModel instance = IoC.Get<AnnualIncomeStatementViewModel>();
                instance.DisplayName = "Estado de resultados anual";
                await ActivateItemAsync(instance, new CancellationToken());
                int MyNewIndex = Items.IndexOf(instance);
                if (MyNewIndex >= 0) SelectedIndex = MyNewIndex;
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Extensions.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                _ = ThemedMessageBox.Show("Atencion !", ex.Message, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public async Task OpenEntityVsAccount()
        {
            try
            {
                EntityVsAccountViewModel instance = IoC.Get<EntityVsAccountViewModel>();
                instance.DisplayName = "Movimiento por tercero & cuenta";
                await ActivateItemAsync(instance, new CancellationToken());
                int MyNewIndex = Items.IndexOf(instance);
                if (MyNewIndex >= 0) SelectedIndex = MyNewIndex;
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                _ = ThemedMessageBox.Show("Atencion !", ex.Message, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        public async Task OpenTestBalanceByEntity()
        {
            try
            {
                TestBalanceByEntityViewModel instance = IoC.Get<TestBalanceByEntityViewModel>();
                instance.DisplayName = "Balance de prueba por tercero";
                await ActivateItemAsync(instance, new CancellationToken());
                int MyNewIndex = Items.IndexOf(instance);
                if (MyNewIndex >= 0) SelectedIndex = MyNewIndex;
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Extensions.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                ThemedMessageBox.Show("Atencion !", ex.Message, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        public async Task OpenAccountingEntries()
        {
            try
            {
                AccountingEntriesViewModel instance = IoC.Get<AccountingEntriesViewModel>();
                instance.DisplayName = "Comprobantes contables";
                await ActivateItemAsync(instance, new CancellationToken());
                int MyNewIndex = Items.IndexOf(instance);
                if (MyNewIndex >= 0) SelectedIndex = MyNewIndex;
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                _ = ThemedMessageBox.Show("Atencion !", ex.Message, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public async Task OpenMeasurementUnits()
        {
            try
            {
                MeasurementUnitViewModel instance = IoC.Get<MeasurementUnitViewModel>();
                instance.DisplayName = "Unidades de medida";
                await ActivateItemAsync(instance, new CancellationToken());
                int MyNewIndex = Items.IndexOf(instance);
                if (MyNewIndex >= 0) SelectedIndex = MyNewIndex;
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                _ = ThemedMessageBox.Show("Atencion !", ex.Message, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        public async Task OpenItemSizes()
        {
            try
            {
                ItemSizeViewModel instance = IoC.Get<ItemSizeViewModel>();
                instance.DisplayName = "Grupos de tallaje";
                await ActivateItemAsync(instance, new CancellationToken());
                int MyNewIndex = Items.IndexOf(instance);
                if (MyNewIndex >= 0) SelectedIndex = MyNewIndex;
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                _ = ThemedMessageBox.Show("Atencion !", ex.Message, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        public async Task OpenIdentificationType()
        {
            try
            {
                IdentificationTypeViewModel instance = IoC.Get<IdentificationTypeViewModel>();
                instance.DisplayName = "Tipos de documentos de identidad";
                await ActivateItemAsync(instance, new CancellationToken());
                int MyNewIndex = Items.IndexOf(instance);
                if (MyNewIndex >= 0) SelectedIndex = MyNewIndex;
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                _ = ThemedMessageBox.Show("Atencion !", ex.Message, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        public async Task OpenCatalogItems()
        {
            try
            {
                CatalogViewModel instance = IoC.Get<CatalogViewModel>();
                instance.DisplayName = "Catalogo de productos";
                await ActivateItemAsync(instance, new CancellationToken());
                int MyNewIndex = Items.IndexOf(instance);
                if (MyNewIndex >= 0) SelectedIndex = MyNewIndex;
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                _ = ThemedMessageBox.Show("Atencion !", ex.Message, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public async Task OpenCostCenters()
        {
            try
            {
                CostCenterViewModel instance = IoC.Get<CostCenterViewModel>();
                instance.DisplayName = "Administración de centros de costos";
                await ActivateItemAsync(instance, new CancellationToken());
                int MyNewIndex = Items.IndexOf(instance);
                if (MyNewIndex >= 0) SelectedIndex = MyNewIndex;
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                _ = ThemedMessageBox.Show("Atencion !", ex.Message, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public async Task OpenTreasuryRootMaster()
        {
            try
            {
                TreasuryRootViewModel instance = IoC.Get<TreasuryRootViewModel>();
                instance.DisplayName = "Administración de cajas, bancos y franquicias";
                await ActivateItemAsync(instance, new CancellationToken());
                int MyNewIndex = Items.IndexOf(instance);
                if (MyNewIndex >= 0) SelectedIndex = MyNewIndex;
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                _ = ThemedMessageBox.Show("Atencion !", ex.Message, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public async Task OpenSmtp()
        {
            try
            {
                SmtpViewModel instance = IoC.Get<SmtpViewModel>();
                instance.DisplayName = "Administración de smtp";
                await ActivateItemAsync(instance, new CancellationToken());
                int MyNewIndex = Items.IndexOf(instance);
                if (MyNewIndex >= 0) SelectedIndex = MyNewIndex;
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                _ = ThemedMessageBox.Show("Atencion !", ex.Message, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public async Task OpenCreditLimit()
        {
            try
            {
                CreditLimitViewModel instance = IoC.Get<CreditLimitViewModel>();
                instance.DisplayName = "Administración de cupos de crédito";
                await ActivateItemAsync(instance, new CancellationToken());
                int MyNewIndex = Items.IndexOf(instance);
                if (MyNewIndex >= 0) SelectedIndex = MyNewIndex;
            }
            catch (Exception ex)
            {
                _ = ThemedMessageBox.Show("Atencion !", ex.Message, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public async Task OpenEmail()
        {
            try
            {
                EmailViewModel instance = IoC.Get<EmailViewModel>();
                instance.DisplayName = "Administración de email";
                await ActivateItemAsync(instance, new CancellationToken());
                int MyNewIndex = Items.IndexOf(instance);
                if (MyNewIndex >= 0) SelectedIndex = MyNewIndex;
            }
            catch (Exception ex)
            {
                _ = ThemedMessageBox.Show("Atencion !", ex.Message, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        public async Task OpenAccountingBooks()
        {
            try
            {
                AccountingBookViewModel instance = IoC.Get<AccountingBookViewModel>();
                instance.DisplayName = "Administración de libros contables";
                await ActivateItemAsync(instance, new CancellationToken());
                int MyNewIndex = Items.IndexOf(instance);
                if (MyNewIndex >= 0) SelectedIndex = MyNewIndex;
            }
            catch (Exception ex)
            {
                _ = ThemedMessageBox.Show("Atencion !", ex.Message, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        public async Task OpenTreasuryConcept()
        {
            try
            {
                ConceptViewModel instance = IoC.Get<ConceptViewModel>();
                instance.DisplayName = "Administración de conceptos de ingresos, egresos y descuentos";
                await ActivateItemAsync(instance, new CancellationToken());
                int MyNewIndex = Items.IndexOf(instance);
                if (MyNewIndex >= 0) SelectedIndex = MyNewIndex;
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                _ = ThemedMessageBox.Show("Atencion !", ex.Message, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public async Task OpenAccountingAccountGroups()
        {
            try
            {
                AccountingAccountGroupViewModel instance = IoC.Get<AccountingAccountGroupViewModel>();
                instance.DisplayName = "Administración de agrupación de cuentas contables";
                await ActivateItemAsync(instance, new CancellationToken());
                int MyNewIndex = Items.IndexOf(instance);
                if (MyNewIndex >= 0) SelectedIndex = MyNewIndex;
            }
            catch (Exception ex)
            {
                _ = ThemedMessageBox.Show("Atencion !", ex.Message, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public async Task OpenWithholdingCertificateConfig()
        {
            try
            {
                WithholdingCertificateConfigViewModel instance = IoC.Get<WithholdingCertificateConfigViewModel>();
                instance.DisplayName = "Configuración del certificado de retención";
                await ActivateItemAsync(instance, new CancellationToken());
                int MyNewIndex = Items.IndexOf(instance);
                if (MyNewIndex >= 0) SelectedIndex = MyNewIndex;
            }
            catch (Exception ex)
            {
                _ = ThemedMessageBox.Show("Atencion !", ex.Message, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        public async Task OpenAuthorizationSequence()
        {
            try
            {
                AuthorizationSequenceViewModel instance = IoC.Get<AuthorizationSequenceViewModel>();
                instance.DisplayName = "Secuencia de Autorizacion";
                await ActivateItemAsync(instance, new CancellationToken());
                int MyNewIndex = Items.IndexOf(instance);
                if (MyNewIndex >= 0) SelectedIndex = MyNewIndex;
            }
            catch (Exception ex)
            {
                _ = ThemedMessageBox.Show("Atencion !", ex.Message, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        public async Task OpenParameter()
        {
            try
            {
                ParameterViewModel instance = IoC.Get<ParameterViewModel>();
                instance.DisplayName = "Configuración";
                await ActivateItemAsync(instance, new CancellationToken());
                int MyNewIndex = Items.IndexOf(instance);
                if (MyNewIndex >= 0) SelectedIndex = MyNewIndex;
            }
            catch (Exception ex)
            {
                _ = ThemedMessageBox.Show("Atencion !", ex.Message, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        
        public async Task OpenPriceList()
        {
            try
            {
                PriceListViewModel instance = IoC.Get<PriceListViewModel>();
                instance.DisplayName = "Administración de listas de precios";
                await ActivateItemAsync(instance, new CancellationToken());
                int MyNewIndex = Items.IndexOf(instance);
                if (MyNewIndex >= 0) SelectedIndex = MyNewIndex;
            }
            catch (Exception ex)
            {
                _ = ThemedMessageBox.Show("Atencion !", ex.Message, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        public async Task OpenTax()
        {
            try
            {
                TaxViewModel instance = IoC.Get<TaxViewModel>();
                instance.DisplayName = "Administración de Impuestos";
                await ActivateItemAsync(instance, new CancellationToken());
                int MyNewIndex = Items.IndexOf(instance);
                if (MyNewIndex >= 0) SelectedIndex = MyNewIndex;
            }
            catch (Exception ex)
            {
                _ = ThemedMessageBox.Show("Atencion !", ex.Message, MessageBoxButton.OK, MessageBoxImage.Information);
            }

        }
        public async Task OpenTaxCategory()
        {
            try
            {
                TaxCategoryViewModel instance = IoC.Get<TaxCategoryViewModel>();
                instance.DisplayName = "Administración de Tipos de Impuesto";
                await ActivateItemAsync(instance, new CancellationToken());
                int MyNewIndex = Items.IndexOf(instance);
                if (MyNewIndex >= 0) SelectedIndex = MyNewIndex;
            }
            catch (Exception ex)
            {
                _ = ThemedMessageBox.Show("Atencion !", ex.Message, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public void ReturnToCompanySelection()
        {
            try
            {
                _eventAggregator.PublishOnUIThreadAsync(new ReturnToCompanySelectionMessage());
            }
            catch (Exception ex)
            {
                _ = ThemedMessageBox.Show("Atención !", ex.Message, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
