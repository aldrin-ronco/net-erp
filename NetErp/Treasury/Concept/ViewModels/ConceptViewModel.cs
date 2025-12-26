using AutoMapper;
using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using Models.Books;
using Models.Global;
using Models.Treasury;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;

namespace NetErp.Treasury.Concept.ViewModels
{
    public class ConceptViewModel : Conductor<object>.Collection.OneActive
    {
        public IMapper AutoMapper { get; private set; }
        public IEventAggregator EventAggregator { get; set; }
        private readonly Helpers.Services.INotificationService _notificationService;

        private readonly IRepository<TreasuryConceptGraphQLModel> _conceptService;
        private readonly IRepository<AccountingAccountGraphQLModel> _accountingAccountService;
        private ConceptMasterViewModel _conceptMasterViewModel;
        public ConceptMasterViewModel ConceptMasterViewModel
        {
            get
            {
                if (_conceptMasterViewModel is null) _conceptMasterViewModel = new ConceptMasterViewModel(this, _notificationService, _conceptService);
                return _conceptMasterViewModel;
            }
        }
        public ConceptViewModel(
            IMapper mapper, 
            IEventAggregator eventAggregator,
            IRepository<TreasuryConceptGraphQLModel> conceptService,
            Helpers.Services.INotificationService notificationService,
            IRepository<AccountingAccountGraphQLModel> accountingAccountService)
        {
            EventAggregator = eventAggregator;
            AutoMapper = mapper;
            _conceptService = conceptService;
            _notificationService = notificationService;
            _accountingAccountService = accountingAccountService;
            _ = ActivateMasterViewAsync();
        }
        
        public async Task ActivateMasterViewAsync()
        {
            try
            {
                await ActivateItemAsync(ConceptMasterViewModel, new System.Threading.CancellationToken());
            }
            catch (Exception)
            {
                throw;
            }

        }
        public async Task ActivateDetailViewForEditAsync(TreasuryConceptGraphQLModel concept)
        {
            try
            {
                ConceptDetailViewModel instance = new(this, _conceptService, _accountingAccountService);

                instance.ConceptId = concept.Id;
                instance.Name = concept.Name;
                instance.Type = concept.Type;
                instance.AccountingAccountId = concept.AccountingAccount.Id;
                instance.AllowMargin = concept.AllowMargin;
                instance.PercentageValue = concept.Margin;
                instance.IsBase100 = concept.MarginBasis == 100;
                instance.IsBase1000 = concept.MarginBasis == 1000;
                instance.AcceptChanges();
                await ActivateItemAsync(instance, new System.Threading.CancellationToken());

            }
            catch(Exception)
            {
                throw;
            }
        }
        public async Task ActivateDetailViewForNewAsync()
        {
            try
            {
                ConceptDetailViewModel instance = new(this, _conceptService, _accountingAccountService);
                instance.CleanUpControls();                
                await ActivateItemAsync(instance, new System.Threading.CancellationToken());
                instance.Type = "D";
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
