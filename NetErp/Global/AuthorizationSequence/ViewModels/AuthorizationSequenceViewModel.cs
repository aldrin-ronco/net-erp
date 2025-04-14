using AutoMapper;
using Caliburn.Micro;
using Common.Helpers;
using DevExpress.Xpf.Core;
using Models.Books;
using Models.Global;
using NetErp.Books.WithholdingCertificateConfig.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetErp.Global.AuthorizationSequence.ViewModels
{
    public class AuthorizationSequenceViewModel : Conductor<object>.Collection.OneActive
    {
        public IEventAggregator EventAggregator { get; private set; }
        public IMapper AutoMapper { get; private set; }

        private AuthorizationSequenceMasterViewModel _authorizationSequenceMasterViewModel;

        public AuthorizationSequenceMasterViewModel AuthorizationSequenceMasterViewModel
        {
            get
            {
                if (_authorizationSequenceMasterViewModel is null) _authorizationSequenceMasterViewModel = new AuthorizationSequenceMasterViewModel(this);
                return _authorizationSequenceMasterViewModel;
            }
        }


        public AuthorizationSequenceViewModel(IMapper mapper, IEventAggregator eventAggregator)
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
        }

        public async Task ActivateMasterViewModelAsync()
        {
            try
            {
                await ActivateItemAsync(AuthorizationSequenceMasterViewModel, new CancellationToken());
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }
        public async Task ActivateDetailViewForEdit(AuthorizationSequenceGraphQLModel? entity)
        {
            AuthorizationSequenceDetailViewModel instance = new(this, entity);


            await ActivateItemAsync(instance, new System.Threading.CancellationToken());
        }
    }
}
