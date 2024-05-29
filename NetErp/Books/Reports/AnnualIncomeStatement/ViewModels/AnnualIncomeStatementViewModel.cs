using Caliburn.Micro;
using Common.Extensions;
using Common.Interfaces;
using DevExpress.Xpf.Core;
using Microsoft.VisualStudio.Threading;
using Models.Books;
using Models.Global;
using NetErp;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using static Models.Books.AnnualIncomeStatementGraphQLModel;

namespace NetErp.Books.Reports.AnnualIncomeStatement.ViewModels
{
    public class AnnualIncomeStatementViewModel : Conductor<IScreen>.Collection.OneActive
    {

        public AnnualIncomeStatementReportViewModel AnnualIncomeStatementReportViewModel { get; set; }
        public readonly IGenericDataAccess<AnnualIncomeStatementGraphQLModel> AnnualIncomeStatementService = IoC.Get<IGenericDataAccess<AnnualIncomeStatementGraphQLModel>>();

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

        public AnnualIncomeStatementViewModel()
        {

            this.AnnualIncomeStatementReportViewModel = new AnnualIncomeStatementReportViewModel(this);
            var joinable = new JoinableTaskFactory(new JoinableTaskContext());
            joinable.Run(async () => await Initialize());
            Task.Run(() => ActivateReportView());
        }

        public async Task ActivateReportView()
        {
            await ActivateItemAsync(this.AnnualIncomeStatementReportViewModel, new System.Threading.CancellationToken());
        }

        public async Task Initialize()
        {
            try
            {
                string query = @"
                query{
                    accountingPresentations{
                    id
                    name
                    },
                    costCenters{
                    id
                    name
                    }
                }";

                var dataContext = await this.AnnualIncomeStatementService.GetDataContext<AnnualIncomeStatementDataContext>(query, new { });
                if (dataContext != null)
                {
                    this.AccountingPresentations = new ObservableCollection<AccountingPresentationGraphQLModel>(dataContext.AccountingPresentations);
                    this.CostCenters = new ObservableCollection<CostCenterGraphQLModel>(dataContext.CostCenters);

                    // Initial Selected Values
                    if (this.CostCenters != null)
                        this.AnnualIncomeStatementReportViewModel.SelectedCostCenters = new ObservableCollection<CostCenterGraphQLModel>(this.CostCenters);

                    if (this.AccountingPresentations != null)
                        this.AnnualIncomeStatementReportViewModel.SelectedAccountingPresentationId = this.AccountingPresentations.FirstOrDefault().Id;

                }
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }
    }
}
