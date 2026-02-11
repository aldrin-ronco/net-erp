using AutoMapper;
using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Xpf.Core;
using Models.Billing;
using Models.Books;
using Models.Global;
using NetErp.Billing.CreditLimit.ViewModels;
using NetErp.Books.WithholdingCertificateConfig.ViewModels;
using NetErp.Global.CostCenters.DTO;
using NetErp.Helpers.Cache;
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
        private readonly CostCenterCache _costCenterCache;

        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly IRepository<AuthorizationSequenceGraphQLModel> _authorizationSequenceService;
        private AuthorizationSequenceMasterViewModel _authorizationSequenceMasterViewModel;
        private readonly AuthorizationSequenceTypeCache _authorizationSequenceTypeCache;

        public AuthorizationSequenceMasterViewModel AuthorizationSequenceMasterViewModel
        {
            get
            {
                if (_authorizationSequenceMasterViewModel is null) _authorizationSequenceMasterViewModel = new AuthorizationSequenceMasterViewModel(this, _notificationService, _authorizationSequenceService, _costCenterCache);
                return _authorizationSequenceMasterViewModel;
            }
        }


        public AuthorizationSequenceViewModel(IMapper mapper, IEventAggregator eventAggregator,  Helpers.Services.INotificationService notificationService, IRepository<AuthorizationSequenceGraphQLModel> authorizationSequenceService, CostCenterCache costCenterCache, AuthorizationSequenceTypeCache authorizationSequenceTypeCache)
        {
            AutoMapper = mapper;
            EventAggregator = eventAggregator;
            _notificationService = notificationService;
            _authorizationSequenceService = authorizationSequenceService;
            _costCenterCache = costCenterCache;
            _authorizationSequenceTypeCache = authorizationSequenceTypeCache;
            _= ActivateMasterViewModelAsync();
          
        }
       
        public async Task ActivateMasterViewModelAsync()
        {
            try
            {
                await ActivateItemAsync(AuthorizationSequenceMasterViewModel ?? new AuthorizationSequenceMasterViewModel(this, _notificationService, _authorizationSequenceService, _costCenterCache), new System.Threading.CancellationToken());
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }

        }
       
        public async Task ActivateDetailViewForEditAsync(int authorizationSequenceId)
        {
            AuthorizationSequenceDetailViewModel instance = new(this, _notificationService, _authorizationSequenceService, _costCenterCache, _authorizationSequenceTypeCache);

            AuthorizationSequenceGraphQLModel sequence = await instance.LoadDataForEditAsync(authorizationSequenceId);
            await ActivateItemAsync(instance, new System.Threading.CancellationToken());
        }
        public async Task ActivateDetailViewForNewAsync()
        {
            AuthorizationSequenceDetailViewModel instance = new(this, _notificationService, _authorizationSequenceService, _costCenterCache, _authorizationSequenceTypeCache);

            await ActivateItemAsync(instance, new System.Threading.CancellationToken());
        }

    }
}
