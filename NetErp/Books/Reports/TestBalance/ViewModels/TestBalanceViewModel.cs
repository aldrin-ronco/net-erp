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



        public TestBalanceViewModel(IRepository<TestBalanceGraphQLModel> testBalanceService)
        {
            this._testBalanceService = testBalanceService;
            try
            {
                this.TestBalanceReportViewModel = new TestBalanceReportViewModel(this, testBalanceService);
                var joinable = new JoinableTaskFactory(new JoinableTaskContext());
              
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

        
    }
}
