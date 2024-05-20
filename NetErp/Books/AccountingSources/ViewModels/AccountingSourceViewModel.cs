using AutoMapper;
using Caliburn.Micro;
using Models.Books;
using Models.Global;
using NetErp.Books.AccountingEntities.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetErp.Books.AccountingSources.ViewModels
{
    public class AccountingSourceViewModel : Conductor<object>.Collection.OneActive
    {
        private AccountingSourceMasterViewModel _accountingSourceMasterViewModel;
        public AccountingSourceMasterViewModel AccountingSourceMasterViewModel 
        {
            get
            {
                
                    if (_accountingSourceMasterViewModel is null) _accountingSourceMasterViewModel = new(this);
                    return _accountingSourceMasterViewModel;
                }
        }

        public IMapper AutoMapper { get; set; }
        public IEventAggregator EventAggregator { get; set; }

        private ObservableCollection<ProcessTypeGraphQLModel> _processTypes;
        public ObservableCollection<ProcessTypeGraphQLModel> ProcessTypes
        {
            get => _processTypes;
            set
            {
                if (_processTypes != value)
                {
                    _processTypes = value;
                    NotifyOfPropertyChange(nameof(ProcessTypes));
                }
            }
        }

        private bool _enableOnViewReady = true;

        public bool EnableOnViewReady
        {
            get { return _enableOnViewReady; }
            set
            {
                _enableOnViewReady = value;
            }
        }
        public AccountingSourceViewModel(IMapper mapper,
                                         IEventAggregator eventAggregator)
        {
            this.EventAggregator = eventAggregator;
            this.AutoMapper = mapper;
            _ = Task.Run(ActivateMasterView);
        }

        public async Task ActivateMasterView()
        {
            try
            {
                await ActivateItemAsync(this.AccountingSourceMasterViewModel, new System.Threading.CancellationToken());
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task ActivateDetailViewForNew()
        {
            AccountingSourceDetailViewModel instance = new(this);
            //instance.Id = 0; // Necesario para saber que es un nuevo registro
            instance.CleanUpControls();
            await ActivateItemAsync(instance, new System.Threading.CancellationToken());
        }

        public async Task ActivateDetailViewForEdit(AccountingSourceDTO selectedItem)
        {
            AccountingSourceDetailViewModel instance = new(this);

            // Esta tecnica se una cuando se trabaja con SelectedItem
            // this.AccountingSourceDetailViewModel.SelectedProcessType = this.AccountingSourceDetailViewModel.ProcessTypes.Where(x => x.Id == selectedItem.ProcessType.Id).FirstOrDefault();

            instance.Id = selectedItem.Id; // Necesario para que el Update se ejecute correctaente
            instance.SelectedProcessTypeId = selectedItem.ProcessType.Id;
            instance.Code = selectedItem.Code;
            instance.Name = selectedItem.Name;
            instance.SelectedKardexFlow = selectedItem.KardexFlow;
            instance.SelectedAnnulmentType = selectedItem.AnnulmentCharacter;
            instance.IsKardexTransaction = selectedItem.IsKardexTransaction;

            if (selectedItem.AccountingAccount != null)
            {
                instance.SelectedAccountingAccountId = selectedItem.AccountingAccount.Id;
            }
            else
            {
                instance.SelectedAccountingAccountId = 0;
            }

            await ActivateItemAsync(instance, new System.Threading.CancellationToken());
        }
    }
}
