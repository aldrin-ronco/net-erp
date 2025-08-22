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
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace NetErp.Books.Reports.DailyBook.ViewModels
{
    public class DailyBookByEntityViewModel : Conductor<IScreen>.Collection.OneActive
    {
        public DailyBookByEntityReportViewModel DailyBookReportViewModel { get; set; }

        private readonly IRepository<DailyBookByEntityGraphQLModel> _dailyBookService;
        private readonly IRepository<AccountingEntityGraphQLModel> _accountingEntityService;

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

        // Fuentes Contables
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


        public DailyBookByEntityViewModel(IRepository<DailyBookByEntityGraphQLModel> dailyBookService, IRepository<AccountingEntityGraphQLModel> accountingEntityService)
        {
            this._dailyBookService = dailyBookService;
            this._accountingEntityService = accountingEntityService;

            DailyBookReportViewModel = new DailyBookByEntityReportViewModel(this, this._dailyBookService, this._accountingEntityService);
            var joinable = new JoinableTaskFactory(new JoinableTaskContext());
            _= InitializeAsync();
            _= ActivateReportViewAsync();
        }

        public async Task ActivateReportViewAsync()
        {
            await ActivateItemAsync(DailyBookReportViewModel, new System.Threading.CancellationToken());
        }

        public async Task InitializeAsync()
        {
            try
            {
                string query = @"
                query ($accountingSourceFilter: AccountingSourceFilterInput) {
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
                    name
                   }
                }";

                dynamic variables = new ExpandoObject();
                variables.AccountingSourceFilter = new ExpandoObject();
                variables.AccountingSourceFilter.Annulment = new ExpandoObject();
                variables.AccountingSourceFilter.Annulment.@operator = "=";
                variables.AccountingSourceFilter.Annulment.value = false;
                var dataContext = await this._dailyBookService.GetDataContextAsync<DailyBookDataContext>(query, variables);
                if (dataContext != null)
                {
                    this.AccountingPresentations = new ObservableCollection<AccountingPresentationGraphQLModel>(dataContext.AccountingPresentations);
                    this.CostCenters = new ObservableCollection<CostCenterGraphQLModel>(dataContext.CostCenters);
                    this.AccountingSources = new ObservableCollection<AccountingSourceGraphQLModel>(dataContext.AccountingSources);

                    // Initial Selected Values
                    if (this.CostCenters != null)
                        this.DailyBookReportViewModel.SelectedCostCenters = new ObservableCollection<CostCenterGraphQLModel>(this.CostCenters);

                    if (this.AccountingPresentations != null)
                        this.DailyBookReportViewModel.SelectedAccountingPresentationId = this.AccountingPresentations.FirstOrDefault().Id;

                    if (this.AccountingSources != null)
                        this.DailyBookReportViewModel.SelectedAccountingSources = new ObservableCollection<AccountingSourceGraphQLModel>(dataContext.AccountingSources);
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
