using AutoMapper;
using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using Common.Validators;
using DevExpress.Xpf.Core;
using Models.Billing;
using NetErp.Billing.Customers.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetErp.Billing.CreditLimit.ViewModels
{
    public class CreditLimitViewModel: Conductor<object>.Collection.OneActive
    {
        public IMapper AutoMapper { get; set; }
        public IEventAggregator EventAggregator { get; set; }
        
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly ICreditLimitValidator _validator;
        private readonly IRepository<CreditLimitGraphQLModel> _creditLimitService;

        private CreditLimitMasterViewModel? _creditLimitMasterViewModel;

        public CreditLimitMasterViewModel? CreditLimitMasterViewModel
        {
            get 
            {
                _creditLimitMasterViewModel ??= new CreditLimitMasterViewModel(this, _notificationService, _validator, _creditLimitService);
                return _creditLimitMasterViewModel;
            }
        }

        public CreditLimitViewModel(
            IMapper autoMapper, 
            IEventAggregator eventAggregator,
            Helpers.Services.INotificationService notificationService,
            ICreditLimitValidator validator,
            IRepository<CreditLimitGraphQLModel> creditLimitService)
        {
            AutoMapper = autoMapper ?? throw new ArgumentNullException(nameof(autoMapper));
            EventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _creditLimitService = creditLimitService ?? throw new ArgumentNullException(nameof(creditLimitService));
        }

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            _ = InitializeAsync().ConfigureAwait(false);
        }

        private async Task InitializeAsync()
        {
            try
            {
                await ActivateMasterViewAsync();
            }
            catch (AsyncException ex)
            {
                await Execute.OnUIThreadAsync(() =>
                {
                    ThemedMessageBox.Show(
                        title: "Atención!", 
                        text: $"{this.GetType().Name}.{ex.MethodOrigin} \r\n{ex.InnerException?.Message}", 
                        messageBoxButtons: MessageBoxButton.OK, 
                        image: MessageBoxImage.Error);
                    return Task.CompletedTask;
                });
            }
            catch (Exception ex)
            {
                await Execute.OnUIThreadAsync(() =>
                {
                    ThemedMessageBox.Show(
                        title: "Error Inesperado!", 
                        text: $"{this.GetType().Name}.InitializeAsync \r\n{ex.Message}", 
                        messageBoxButtons: MessageBoxButton.OK, 
                        image: MessageBoxImage.Error);
                    return Task.CompletedTask;
                });
            }
        }

        public async Task ActivateMasterViewAsync()
        {
            try
            {
                await ActivateItemAsync(CreditLimitMasterViewModel ?? new CreditLimitMasterViewModel(this, _notificationService, _validator, _creditLimitService), new System.Threading.CancellationToken());
            }
            catch(Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }

        }
    }
}
