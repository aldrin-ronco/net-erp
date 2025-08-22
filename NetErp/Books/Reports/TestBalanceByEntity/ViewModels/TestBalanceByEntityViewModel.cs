using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
using Microsoft.VisualStudio.Threading;
using Models.Books;
using Models.Global;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace NetErp.Books.Reports.TestBalanceByEntity.ViewModels
{
    public class TestBalanceByEntityViewModel : Conductor<IScreen>.Collection.OneActive
    {
        public TestBalanceByEntityReportViewModel TestBalanceByEntityReportViewModel { get; set; }

        private readonly IRepository<AccountingEntityGraphQLModel> _accountingEntityService;

        private readonly IRepository<TestBalanceByEntityGraphQLModel> _testBalanceByEntityService;


        // Presentaciones
        private ObservableCollection<AccountingPresentationGraphQLModel> _accountingPresentations;
        public ObservableCollection<AccountingPresentationGraphQLModel> AccountingPresentations
        {
            get { return _accountingPresentations; }
            set
            {
                if (_accountingPresentations != value)
                {
                    _accountingPresentations = value;
                    NotifyOfPropertyChange(nameof(AccountingPresentations));
                }
            }
        }

        // Centros de costos
        private ObservableCollection<CostCenterGraphQLModel> _costCenters;
        public ObservableCollection<CostCenterGraphQLModel> CostCenters
        {
            get { return _costCenters; }
            set
            {
                if (_costCenters != value)
                {
                    _costCenters = value;
                    NotifyOfPropertyChange(nameof(CostCenters));
                }
            }
        }

        // Fuentes contables
        private ObservableCollection<AccountingSourceGraphQLModel> _accountingSources;
        public ObservableCollection<AccountingSourceGraphQLModel> AccountingSources
        {
            get { return _accountingSources; }
            set
            {
                if (_accountingSources != value)
                {
                    _accountingSources = value;
                    NotifyOfPropertyChange(nameof(AccountingSources));
                }
            }
        }

        // Cuentas Contables
        private ObservableCollection<AccountingAccountGraphQLModel> _accountingAccounts;
        public ObservableCollection<AccountingAccountGraphQLModel> AccountingAccounts
        {
            get { return _accountingAccounts; }
            set
            {
                if (_accountingAccounts != value)
                {
                    _accountingAccounts = value;
                    NotifyOfPropertyChange(nameof(AccountingAccounts));
                }
            }
        }

        // Cuentas Contables para filtro de cuenta final, por alguna razon no me permite usar una sola fuente al usar el combo de autocompletado
        private ObservableCollection<AccountingAccountGraphQLModel> _accountingAccountsEnd;
        public ObservableCollection<AccountingAccountGraphQLModel> AccountingAccountsEnd
        {
            get { return _accountingAccountsEnd; }
            set
            {
                if (_accountingAccountsEnd != value)
                {
                    _accountingAccountsEnd = value;
                    NotifyOfPropertyChange(nameof(AccountingAccounts));
                }
            }
        }

        public TestBalanceByEntityViewModel(IRepository<TestBalanceByEntityGraphQLModel> testBalanceByEntityService, IRepository<AccountingEntityGraphQLModel> accountingEntityService)
        {
            this._accountingEntityService = accountingEntityService;
            this._testBalanceByEntityService = testBalanceByEntityService;
            try
            {
                this.TestBalanceByEntityReportViewModel = new TestBalanceByEntityReportViewModel(this, _testBalanceByEntityService, _accountingEntityService);
                var joinable = new JoinableTaskFactory(new JoinableTaskContext());
                joinable.Run(async () => await Initialize());
                _= ActivateReportView();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task ActivateReportView()
        {
            await ActivateItemAsync(this.TestBalanceByEntityReportViewModel, new System.Threading.CancellationToken());
        }

        public async Task Initialize()
        {
            try
            {
                string query = @"
                query ($accountingAccountFilter: AccountingAccountFilterInput, $accountingSourceFilter:AccountingSourceFilterInput ) {
                  accountingPresentations{
                    id
                    name
                  },
                  costCenters{
                    id
                    name
                  },
                  accountingSources(filter: $accountingSourceFilter) {
                    id
                    reverseId
                    name
                  },
                  accountingAccounts(filter: $accountingAccountFilter) {
                    id
                    code
                    name
                  }
                }";

                dynamic variables = new ExpandoObject();
                variables.AccountingSourceFilter = new ExpandoObject();
                variables.AccountingSourceFilter.Annulment = new ExpandoObject();
                variables.AccountingSourceFilter.Annulment.@operator = "=";
                variables.AccountingSourceFilter.Annulment.value = false;
                variables.AccountingAccountFilter = new ExpandoObject();
                variables.AccountingAccountFilter.Code = new ExpandoObject();
                variables.AccountingAccountFilter.Code.@operator = new List<string> { "length", ">=" };
                variables.AccountingAccountFilter.Code.value = 8;
                var dataContext = await this._testBalanceByEntityService.GetDataContextAsync<TestBalanceByEntityDataContext>(query, variables);
                if (dataContext != null)
                {
                    this.AccountingPresentations = new ObservableCollection<AccountingPresentationGraphQLModel>(dataContext.AccountingPresentations);
                    this.CostCenters = new ObservableCollection<CostCenterGraphQLModel>(dataContext.CostCenters);
                    this.AccountingSources = new ObservableCollection<AccountingSourceGraphQLModel>(dataContext.AccountingSources);
                    this.AccountingAccounts = new ObservableCollection<AccountingAccountGraphQLModel>(dataContext.AccountingAccounts);
                    this.AccountingAccountsEnd = new ObservableCollection<AccountingAccountGraphQLModel>(dataContext.AccountingAccounts);

                    // Initial Selected Values
                    if (this.CostCenters != null)
                        this.TestBalanceByEntityReportViewModel.SelectedCostCenters = new ObservableCollection<CostCenterGraphQLModel>(this.CostCenters);

                    if (this.AccountingPresentations != null)
                        this.TestBalanceByEntityReportViewModel.SelectedAccountingPresentationId = this.AccountingPresentations.FirstOrDefault().Id;

                    if (this.AccountingSources != null)
                        this.TestBalanceByEntityReportViewModel.SelectedAccountingSources = new ObservableCollection<AccountingSourceGraphQLModel>(this.AccountingSources);
                }
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Extensions.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }
    }
}
