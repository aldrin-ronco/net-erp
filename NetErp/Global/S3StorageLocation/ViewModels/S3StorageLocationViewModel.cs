using AutoMapper;
using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using Models.Global;
using NetErp.Global.AwsS3Config.ViewModels;
using NetErp.Helpers.Cache;
using Services.Global.DAL.PostgreSQL;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NetErp.Global.S3StorageLocation.ViewModels
{
    public class S3StorageLocationViewModel : Conductor<object>.Collection.OneActive
    {
        public IMapper AutoMapper { get; private set; }
        public IEventAggregator EventAggregator { get; set; }
        private readonly Helpers.Services.INotificationService _notificationService;
        private S3StorageLocationMasterViewModel _s3StorageLocationMasterViewModel;
        private readonly IRepository<S3StorageLocationGraphQLModel> _s3StorageLocationService;
        private readonly AwsS3ConfigCache _awsS3ConfigCache;


        private S3StorageLocationMasterViewModel S3StorageLocationMasterViewModel
        {
            get
            {
                if (_s3StorageLocationMasterViewModel is null) _s3StorageLocationMasterViewModel = new S3StorageLocationMasterViewModel(this, _notificationService, _s3StorageLocationService);
                return _s3StorageLocationMasterViewModel;

            }
        }

        public S3StorageLocationViewModel(IMapper autoMapper, IEventAggregator eventAggregator, Helpers.Services.INotificationService notificationService,
            IRepository<S3StorageLocationGraphQLModel> s3StorageLocationService,
                    AwsS3ConfigCache awsS3ConfigCache)
        {
            EventAggregator = eventAggregator;
            _notificationService = notificationService;
            _s3StorageLocationService = s3StorageLocationService;
            _awsS3ConfigCache = awsS3ConfigCache;

            _ = Task.Run(ActivateMasterViewAsync);
        }

        public async Task ActivateMasterViewAsync()
        {
            try
            {
                await base.ActivateItemAsync(S3StorageLocationMasterViewModel, new System.Threading.CancellationToken());
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
                S3StorageLocationDetailViewModel instance = new S3StorageLocationDetailViewModel(this, _notificationService, _s3StorageLocationService, _awsS3ConfigCache);
                S3StorageLocationGraphQLModel group = await instance.LoadDataForEditAsync(id);
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
                S3StorageLocationDetailViewModel instance = new S3StorageLocationDetailViewModel(this, _notificationService, _s3StorageLocationService, _awsS3ConfigCache);
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
