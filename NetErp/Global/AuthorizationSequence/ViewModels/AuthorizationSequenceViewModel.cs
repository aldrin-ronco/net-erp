using AutoMapper;
using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Xpf.Core;
using Models.Books;
using Models.Global;
using NetErp.Billing.CreditLimit.ViewModels;
using NetErp.Books.WithholdingCertificateConfig.ViewModels;
using NetErp.Global.CostCenters.DTO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetErp.Global.AuthorizationSequence.ViewModels
{
    public class AuthorizationSequenceViewModel : Conductor<object>.Collection.OneActive
    {
        public IMapper AutoMapper { get; private set; }
        public IEventAggregator EventAggregator { get; private set; }

        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly IRepository<AuthorizationSequenceGraphQLModel> _authorizationSequenceService;
        private AuthorizationSequenceMasterViewModel _authorizationSequenceMasterViewModel;

        public AuthorizationSequenceMasterViewModel AuthorizationSequenceMasterViewModel
        {
            get
            {
                if (_authorizationSequenceMasterViewModel is null) _authorizationSequenceMasterViewModel = new AuthorizationSequenceMasterViewModel(this, _notificationService, _authorizationSequenceService);
                return _authorizationSequenceMasterViewModel;
            }
        }


        public AuthorizationSequenceViewModel(IMapper mapper, IEventAggregator eventAggregator,  Helpers.Services.INotificationService notificationService, IRepository<AuthorizationSequenceGraphQLModel> authorizationSequenceService)
        {
            AutoMapper = mapper;
            EventAggregator = eventAggregator;
            _notificationService = notificationService;
            _authorizationSequenceService = authorizationSequenceService;
            _= ActivateMasterViewModelAsync();
          
        }
       
        public async Task ActivateMasterViewModelAsync()
        {
            try
            {
                await ActivateItemAsync(AuthorizationSequenceMasterViewModel ?? new AuthorizationSequenceMasterViewModel(this, _notificationService, _authorizationSequenceService), new System.Threading.CancellationToken());
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }

        }
       
        public async Task ActivateDetailViewForEdit(AuthorizationSequenceGraphQLModel? entity,
            ObservableCollection<CostCenterDTO> costCenters)
        {
            AuthorizationSequenceDetailViewModel instance = new(this, entity, _notificationService, _authorizationSequenceService, costCenters);


            await ActivateItemAsync(instance, new System.Threading.CancellationToken());
        }

       
    }
}
