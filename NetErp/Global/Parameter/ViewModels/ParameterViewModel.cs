using AutoMapper;
using Caliburn.Micro;
using Common.Helpers;
using DevExpress.Xpf.Core;
using NetErp.Global.AuthorizationSequence.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetErp.Global.Parameter.ViewModels
{
    public class ParameterViewModel : Conductor<object>.Collection.OneActive
    {
        public IEventAggregator EventAggregator { get; private set; }
        public IMapper AutoMapper { get; private set; }
        private ParameterMasterViewModel _parameterMasterViewModel;

        public ParameterMasterViewModel ParameterMasterViewModel
        {
            get
            {
                if (_parameterMasterViewModel is null) _parameterMasterViewModel = new ParameterMasterViewModel(this);
                return _parameterMasterViewModel;
            }
        }
        public ParameterViewModel(IMapper mapper, IEventAggregator eventAggregator)
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
                await ActivateItemAsync(ParameterMasterViewModel, new CancellationToken());
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }
    }
}
