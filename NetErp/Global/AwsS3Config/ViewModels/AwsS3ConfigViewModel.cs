using AutoMapper;
using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using Models.Books;
using Models.Global;
using NetErp.Books.AccountingGroups.ViewModels;
using NetErp.Helpers.Cache;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NetErp.Global.AwsS3Config.ViewModels
{
    public class AwsS3ConfigViewModel : Conductor<object>.Collection.OneActive
    {
        public IMapper AutoMapper { get; private set; }
        public IEventAggregator EventAggregator { get; set; }
        private readonly Helpers.Services.INotificationService _notificationService;
        private AwsS3ConfigMasterViewModel _awsS3ConfigMasterViewModel;
        private readonly IRepository<AwsS3ConfigGraphQLModel> _awsS3ConfigService;


        private AwsS3ConfigMasterViewModel AwsS3ConfigMasterViewModel
        {
            get
            {
                if (_awsS3ConfigMasterViewModel is null) _awsS3ConfigMasterViewModel = new AwsS3ConfigMasterViewModel(this, _notificationService, _awsS3ConfigService);
                return _awsS3ConfigMasterViewModel;

            }
        }
        public AwsS3ConfigViewModel(IMapper autoMapper, IEventAggregator eventAggregator, Helpers.Services.INotificationService notificationService, IRepository<AwsS3ConfigGraphQLModel> awsS3ConfigService)
        {
            AutoMapper = autoMapper ?? throw new ArgumentNullException(nameof(autoMapper));
            EventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _awsS3ConfigService = awsS3ConfigService ?? throw new ArgumentNullException(nameof(awsS3ConfigService));
            _ = Task.Run(ActivateMasterViewAsync);
        }
        public async Task ActivateMasterViewAsync()
        {
            try
            {
                await base.ActivateItemAsync(AwsS3ConfigMasterViewModel, new System.Threading.CancellationToken());
            }
            catch (Exception)
            {
                throw;
            }
        }
        public async Task ActivateDetailViewForEditAsync(int id)
        {
            try
            {
                AwsS3ConfigDetailViewModel instance = new AwsS3ConfigDetailViewModel(this, _awsS3ConfigService);
                AwsS3ConfigGraphQLModel group = await instance.LoadDataForEditAsync(id);
                instance.AcceptChanges();
                await ActivateItemAsync(instance, new System.Threading.CancellationToken());
            }
            catch
            {

            }
        }
        public async Task ActivateDetailViewForNewAsync()
        {
            try
            {
                AwsS3ConfigDetailViewModel instance = new AwsS3ConfigDetailViewModel(this, _awsS3ConfigService);
                //instance.CleanUpControls();
                await ActivateItemAsync(instance, new System.Threading.CancellationToken());
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}

