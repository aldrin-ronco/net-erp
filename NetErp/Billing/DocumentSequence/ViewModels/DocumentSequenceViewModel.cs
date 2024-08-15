using AutoMapper;
using Caliburn.Micro;
using Common.Interfaces;
using Models.Billing;
using Models.Global;
using NetErp.Billing.Customers.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace NetErp.Billing.DocumentSequence.ViewModels
{
    public class DocumentSequenceViewModel : Conductor<Screen>.Collection.OneActive
    {
        public IEventAggregator EventAggregator { get; private set; }
        public IMapper AutoMapper { get; private set; }

        private ObservableCollection<CostCenterGraphQLModel> _costCenters;
        public ObservableCollection<CostCenterGraphQLModel> CostCenters
        {
            get => _costCenters;
            set
            {
                if (_costCenters != value)
                {
                    _costCenters = value;
                    NotifyOfPropertyChange(nameof(CostCenters));
                }
            }
        }

        private DocumentSequenceMasterViewModel _documentSequenceMasterViewModel;
        public DocumentSequenceMasterViewModel DocumentSequenceMasterViewModel
        {
            get
            {
                if (_documentSequenceMasterViewModel is null) _documentSequenceMasterViewModel = new DocumentSequenceMasterViewModel(this);
                return _documentSequenceMasterViewModel;
            }
        }
        public DocumentSequenceViewModel(IEventAggregator eventAggregator,
                                         IMapper mapper)
        {
            EventAggregator = eventAggregator;
            AutoMapper = mapper;
            Task.Run(() => ActivateMasterView());
        }

        public async Task ActivateMasterView()
        {
            try
            {
                await ActivateItemAsync(DocumentSequenceMasterViewModel, new System.Threading.CancellationToken());
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task ActivateDetailViewForEdit(DocumentSequenceMasterGraphQLModel model)
        {
            DocumentSequenceDetailViewModel instance = new(this);
            instance.Id = model.Id;
            instance.SelectedCostCenterId = model.CostCenter.Id;
            instance.Number = model.Number;
            instance.InitialDate = model.InitialDate;
            instance.FinalDate = model.FinalDate;
            instance.Prefix = model.Prefix;
            instance.InitialNumber = model.InitialNumber;
            instance.FinalNumber = model.FinalNumber;
            instance.ActualNumber = model.DocumentSequenceDetail.Number;
            instance.SelectedTitleLabel = model.TitleLabel;
            instance.SelectedAuthorizationType = model.AuthorizationType;
            instance.SelectedSequenceLabel = model.SequenceLabel;
            instance.SelectedAuthorizationKind = model.AuthorizationKind;
            instance.IsActiveAuthorization = model.IsActive;
            instance.TechnicalKey = model.TechnicalKey;
            instance.Reference = model.Reference;
            await ActivateItemAsync(instance, new System.Threading.CancellationToken());
        }

        public async Task ActivateDetailViewForNew()
        {
            DocumentSequenceDetailViewModel instance = new(this);
            instance.CleanControls();
            await ActivateItemAsync(instance, new System.Threading.CancellationToken());
        }
    }
}
