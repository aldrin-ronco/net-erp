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


        public TestBalanceByEntityViewModel(IRepository<TestBalanceByEntityGraphQLModel> testBalanceByEntityService, IRepository<AccountingEntityGraphQLModel> accountingEntityService)
        {
            this._accountingEntityService = accountingEntityService;
            this._testBalanceByEntityService = testBalanceByEntityService;
            try
            {
                this.TestBalanceByEntityReportViewModel = new TestBalanceByEntityReportViewModel(this, _testBalanceByEntityService, _accountingEntityService);
                var joinable = new JoinableTaskFactory(new JoinableTaskContext());
               
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

      
    }
}
