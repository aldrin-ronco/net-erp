using Caliburn.Micro;
using Common.Interfaces;
using Microsoft.VisualStudio.Threading;
using Models.Books;
using Models.Global;
using Services.Books.DAL.PostgreSQL;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace NetErp.Books.Reports.AuxiliaryBook.ViewModels
{
    public class AuxiliaryBookViewModel : Conductor<Screen>.Collection.OneActive
    {
         private readonly IRepository<AuxiliaryBookGraphQLModel> _auxiliaryBookService;


      

        public AuxiliaryBookReportViewModel AuxiliaryBookResportViewModel { get; set; }

        public AuxiliaryBookViewModel(IRepository<AuxiliaryBookGraphQLModel> auxiliaryBookService)
        {
            try
            {
                this._auxiliaryBookService = auxiliaryBookService;
                AuxiliaryBookResportViewModel = new AuxiliaryBookReportViewModel(this, this._auxiliaryBookService);
                _ = Task.Run(() => ActivateReportViewAsync());
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task ActivateReportViewAsync()
        {
            await ActivateItemAsync(this.AuxiliaryBookResportViewModel, new System.Threading.CancellationToken());
        }

    }
}
