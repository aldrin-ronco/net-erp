using Caliburn.Micro;
using Common.Extensions;
using Common.Interfaces;
using DevExpress.Xpf.Core;
using Microsoft.VisualStudio.Threading;
using Models.Books;
using Models.Global;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace NetErp.Books.Reports.TestBalance.ViewModels
{
    public class TestBalanceViewModel : Conductor<IScreen>.Collection.OneActive
    {
        public TestBalanceReportViewModel TestBalanceReportViewModel { get; set; }

        private readonly IRepository<TestBalanceGraphQLModel> _testBalanceService;


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

        public TestBalanceViewModel(IRepository<TestBalanceGraphQLModel> testBalanceService)
        {
            this._testBalanceService = testBalanceService;
            try
            {
                this.TestBalanceReportViewModel = new TestBalanceReportViewModel(this, testBalanceService);
                var joinable = new JoinableTaskFactory(new JoinableTaskContext());
                joinable.Run(async () => await Initialize());
                _ = Task.Run(() => ActivateReportView());
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task ActivateReportView()
        {
            await ActivateItemAsync(this.TestBalanceReportViewModel, new System.Threading.CancellationToken());
        }

        private async Task Initialize()
        {
            try
            {
                string query = @"
                query{
                    accountingPresentations{
                    id
                    name
                    },
                    costCenters {
                    id
                    name
                    }
                }";

                var dataContext = await this._testBalanceService.GetDataContextAsync<TestBalanceDataContext>(query, new { });
                if (dataContext != null)
                {
                    this.AccountingPresentations = new ObservableCollection<AccountingPresentationGraphQLModel>(dataContext.AccountingPresentations);
                    this.CostCenters = new ObservableCollection<CostCenterGraphQLModel>(dataContext.CostCenters);

                    // Initial Selected Values
                    if (this.CostCenters != null)
                        this.TestBalanceReportViewModel.SelectedCostCenters = new ObservableCollection<CostCenterGraphQLModel>(this.CostCenters);

                    if (this.AccountingPresentations != null)
                        this.TestBalanceReportViewModel.SelectedAccountingPresentationId = this.AccountingPresentations.FirstOrDefault().Id;

                }
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }
    }
}
