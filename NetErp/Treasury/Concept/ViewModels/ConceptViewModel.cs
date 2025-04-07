using AutoMapper;
using Caliburn.Micro;
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
        private ConceptMasterViewModel _conceptMasterViewModel;
        public ConceptMasterViewModel ConceptMasterViewModel
        {
            get
            {
                if (_conceptMasterViewModel is null) _conceptMasterViewModel = new ConceptMasterViewModel(this);
                return _conceptMasterViewModel;
            }
        }
        public ConceptViewModel(IMapper mapper, IEventAggregator eventAggregator)
        {
            EventAggregator = eventAggregator;
            AutoMapper = mapper;
            _ = Task.Run(ActivateMasterView);
        }
        
        public async Task ActivateMasterView()
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
        public async Task ActivateDetailViewForEdit(ConceptGraphQLModel concept)
        {
            try
            {
                ConceptDetailViewModel instance = new(this);
                instance.ConceptId = concept.Id;
                instance.NameConcept = concept.Name;
                instance.SelectedType = concept.Type;
                instance.SelectedAccoutingAccount = instance.AccoutingAccount.FirstOrDefault(account => account.Id == concept.AccountingAccountId) ?? throw new Exception(); //TODO

                await ActivateItemAsync(instance, new System.Threading.CancellationToken());
            }
            catch(Exception)
            {
                throw;
            }
        }
        public async Task ActivateDetailViewForNew()
        {
            try
            {
                ConceptDetailViewModel instance = new(this);
                instance.CleanUpControls();
                
                await ActivateItemAsync(instance, new System.Threading.CancellationToken());
                instance.SelectedType = "D";
            }
            catch (Exception)
            {
                throw;
            }
        }


    }
}
