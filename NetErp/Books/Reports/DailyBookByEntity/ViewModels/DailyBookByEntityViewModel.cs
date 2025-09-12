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

       

        public DailyBookByEntityViewModel(IRepository<DailyBookByEntityGraphQLModel> dailyBookService, IRepository<AccountingEntityGraphQLModel> accountingEntityService)
        {
            this._dailyBookService = dailyBookService;
            this._accountingEntityService = accountingEntityService;

            DailyBookReportViewModel = new DailyBookByEntityReportViewModel(this, this._dailyBookService, this._accountingEntityService);
            var joinable = new JoinableTaskFactory(new JoinableTaskContext());
          
            _= ActivateReportViewAsync();
        }

        public async Task ActivateReportViewAsync()
        {
            await ActivateItemAsync(DailyBookReportViewModel, new System.Threading.CancellationToken());
        }

        
    }
}
