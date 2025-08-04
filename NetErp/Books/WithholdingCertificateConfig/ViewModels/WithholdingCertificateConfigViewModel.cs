using AutoMapper;
using Caliburn.Micro;
using Common.Helpers;
using DevExpress.Xpf.Core;
using Models.Books;
using NetErp.Books.AccountingAccountGroups.ViewModels;
using NetErp.Global.CostCenters.DTO;
using Ninject.Activation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetErp.Books.WithholdingCertificateConfig.ViewModels
{
    public class WithholdingCertificateConfigViewModel : Conductor<object>.Collection.OneActive
    {
        public IMapper AutoMapper { get; private set; }

        public IEventAggregator EventAggregator { get; private set; }

        private WithholdingCertificateConfigMasterViewModel _withholdingCertificateConfigMasterViewModel;

        public WithholdingCertificateConfigMasterViewModel WithholdingCertificateConfigMasterViewModel
        {
            get
            {
                if (_withholdingCertificateConfigMasterViewModel is null) _withholdingCertificateConfigMasterViewModel = new WithholdingCertificateConfigMasterViewModel(this);
                return _withholdingCertificateConfigMasterViewModel;
            }
        }

        public WithholdingCertificateConfigViewModel(IMapper mapper, IEventAggregator eventAggregator)
        {
            AutoMapper = mapper;
            EventAggregator = eventAggregator;
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
            EventAggregator = eventAggregator;
        }

        public async Task ActivateMasterViewModelAsync()
        {
            try
            {
                await ActivateItemAsync(WithholdingCertificateConfigMasterViewModel, new CancellationToken());
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }
        public async Task ActivateDetailViewForEdit(WithholdingCertificateConfigGraphQLModel? selectedItem)
        {
            WithholdingCertificateConfigDetailViewModel instance = new(this, selectedItem);
           

            await ActivateItemAsync(instance, new System.Threading.CancellationToken());
        }
    }
}
