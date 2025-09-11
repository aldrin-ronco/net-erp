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

namespace NetErp.Books.Reports.EntityVsAccount.ViewModels
{
    public class EntityVsAccountViewModel : Conductor<IScreen>.Collection.OneActive
    {
        //

        private readonly IRepository<AccountingEntityGraphQLModel> _accountingEntityService;
        private readonly IRepository<EntityVsAccountGraphQLModel> _entityVsAccountService;

        
        public EntityVsAccountReportViewModel EntityVsAccountReportViewModel { get; set; }

        public EntityVsAccountViewModel(IRepository<AccountingEntityGraphQLModel> accountingEntityService, IRepository<EntityVsAccountGraphQLModel> entityVsAccountService)
        {
            this._accountingEntityService = accountingEntityService;
            this._entityVsAccountService = entityVsAccountService;
            try
            {
                this.EntityVsAccountReportViewModel = new EntityVsAccountReportViewModel(this, this._accountingEntityService, this._entityVsAccountService);
               

               _= ActivateReportView();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task ActivateReportView()
        {
            await ActivateItemAsync(this.EntityVsAccountReportViewModel, new System.Threading.CancellationToken());
        }

        

    }
}
