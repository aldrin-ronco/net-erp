using AutoMapper;
using Caliburn.Micro;
using Common.Interfaces;
using Models.Books;
using Services.Books.DAL.PostgreSQL;
using System.Threading.Tasks;

namespace NetErp.Books.IdentificationTypes.ViewModels
{
    public class IdentificationTypeViewModel : Conductor<Screen>.Collection.OneActive
    {

        // MasterViewModel
        private IdentificationTypeMasterViewModel _identificationTypeMasterViewModel;
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly IRepository<IdentificationTypeGraphQLModel> _identificationTypeService;
        public IdentificationTypeMasterViewModel IdentificationTypeMasterViewModel
        {
            get
            {
                if (_identificationTypeMasterViewModel == null) this._identificationTypeMasterViewModel = new IdentificationTypeMasterViewModel(this, _identificationTypeService, _notificationService);
                return this._identificationTypeMasterViewModel;
            }
        }

        // IBooksIdentificationType
        public IMapper AutoMapper { get; private set; }
        public IEventAggregator EventAggregator { get; set; }

        public IdentificationTypeViewModel(IMapper mapper,
                                 IEventAggregator eventAggregator, IRepository<IdentificationTypeGraphQLModel> identificationTypeService, Helpers.Services.INotificationService notificationService)
        {
            EventAggregator = eventAggregator;
            this._identificationTypeService = identificationTypeService;
            this._notificationService = notificationService;
            AutoMapper = mapper;
           _= this.ActivateMasterViewAsync();
        }

        public async Task ActivateMasterViewAsync()
        {
            await ActivateItemAsync(this.IdentificationTypeMasterViewModel, new System.Threading.CancellationToken());
        }

        public async Task ActivateDetailViewForNewAsync()
        {
            IdentificationTypeDetailViewModel instance = new(this, _identificationTypeService);
            instance.CleanUpControlsForNew();
            await ActivateItemAsync(instance, new System.Threading.CancellationToken());
        }

        public async Task ActivateDetailViewForEditAsync(IdentificationTypeGraphQLModel model)
        {
            IdentificationTypeDetailViewModel instance = new(this, _identificationTypeService);
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
