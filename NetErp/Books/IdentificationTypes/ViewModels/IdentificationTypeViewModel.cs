using AutoMapper;
using Caliburn.Micro;
using Models.Books;
using System.Threading.Tasks;

namespace NetErp.Books.IdentificationTypes.ViewModels
{
    public class IdentificationTypeViewModel : Conductor<Screen>.Collection.OneActive
    {

        // MasterViewModel
        private IdentificationTypeMasterViewModel _identificationTypeMasterViewModel;
        public IdentificationTypeMasterViewModel IdentificationTypeMasterViewModel
        {
            get
            {
                if (_identificationTypeMasterViewModel == null) this._identificationTypeMasterViewModel = new IdentificationTypeMasterViewModel(this);
                return this._identificationTypeMasterViewModel;
            }
        }

        // IBooksIdentificationType
        public IMapper AutoMapper { get; private set; }
        public IEventAggregator EventAggregator { get; set; }

        public IdentificationTypeViewModel(IMapper mapper,
                                 IEventAggregator eventAggregator)
        {
            EventAggregator = eventAggregator;
            AutoMapper = mapper;
            Task.Run(() => this.ActivateMasterView());
        }

        public async Task ActivateMasterView()
        {
            await ActivateItemAsync(this.IdentificationTypeMasterViewModel, new System.Threading.CancellationToken());
        }

        public async Task ActivateDetailViewForNew()
        {
            IdentificationTypeDetailViewModel instance = new(this);
            instance.CleanUpControlsForNew();
            await ActivateItemAsync(instance, new System.Threading.CancellationToken());
        }

        public async Task ActivateDetailViewForEdit(IdentificationTypeGraphQLModel model)
        {
            IdentificationTypeDetailViewModel instance = new(this);
            App.Current.Dispatcher.Invoke(() =>
            {
                instance.Id = model.Id;
                instance.Code = model.Code;
                instance.Name = model.Name;
                instance.HasVerificationDigit = model.HasVerificationDigit;
                instance.MinimumDocumentLength = model.MinimumDocumentLength;
            });
            await ActivateItemAsync(instance, new System.Threading.CancellationToken());
        }
    }
}
