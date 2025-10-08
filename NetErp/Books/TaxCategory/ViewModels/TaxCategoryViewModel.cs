using AutoMapper;
using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Xpf.Core;
using Models.Books;
using Models.Global;
using NetErp.Global.AuthorizationSequence.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetErp.Books.TaxCategory.ViewModels
{
    public class TaxCategoryViewModel : Conductor<object>.Collection.OneActive
    {
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly IRepository<TaxCategoryGraphQLModel> _TaxCategoryService;

        public IEventAggregator EventAggregator { get; set; }

        public IMapper AutoMapper { get; private set; }
        private TaxCategoryMasterViewModel _TaxCategoryMasterViewModel;

        public TaxCategoryMasterViewModel TaxCategoryMasterViewModel
        {
            get
            {
                if (_TaxCategoryMasterViewModel is null) _TaxCategoryMasterViewModel = new TaxCategoryMasterViewModel(this, _notificationService, _TaxCategoryService);
                return _TaxCategoryMasterViewModel;
            }
        }
       
        public TaxCategoryViewModel(IMapper mapper, IEventAggregator eventAggregator,  Helpers.Services.INotificationService notificationService, IRepository<TaxCategoryGraphQLModel> TaxCategoryService)
        {
            AutoMapper = mapper;
            EventAggregator = eventAggregator;
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _TaxCategoryService = TaxCategoryService ?? throw new ArgumentNullException(nameof(TaxCategoryService));
            _ = Task.Run(async () =>
            {
                try
                {
                    await ActivateMasterViewModelAsync();
                }
                catch (AsyncException ex)
                {
                    await Execute.OnUIThreadAsync(() =>
                    {
                        ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{ex.MethodOrigin} \r\n{ex.InnerException?.Message ?? ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                        return Task.CompletedTask;
                    });
                }
            });
        }
        public async Task ActivateMasterViewModelAsync()
        {
            try
            {
                await ActivateItemAsync(TaxCategoryMasterViewModel, new CancellationToken());
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }
        public async Task ActivateDetailViewForEdit(TaxCategoryGraphQLModel? entity)
        {
            TaxCategoryDetailViewModel instance = new(this, entity, _TaxCategoryService);


            await ActivateItemAsync(instance, new System.Threading.CancellationToken());
        }
    }
}
