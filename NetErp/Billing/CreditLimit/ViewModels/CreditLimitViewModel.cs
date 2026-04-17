using AutoMapper;
using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using Common.Validators;
using DevExpress.Xpf.Core;
using Microsoft.VisualStudio.Threading;
using Models.Billing;
using NetErp.Helpers;
using NetErp.Helpers.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NetErp.Billing.CreditLimit.ViewModels
{
    public class CreditLimitViewModel : Conductor<object>.Collection.OneActive
    {
        public IMapper AutoMapper { get; }
        public IEventAggregator EventAggregator { get; }
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly INotificationService _notificationService;
        private readonly ICreditLimitValidator _validator;
        private readonly IRepository<CreditLimitGraphQLModel> _creditLimitService;
        private readonly IBackgroundQueueService _backgroundQueueService;
        private readonly DebouncedAction _searchDebounce;

        public CreditLimitMasterViewModel CreditLimitMasterViewModel =>
            field ??= new CreditLimitMasterViewModel(
                this, _notificationService, _validator, _backgroundQueueService, _creditLimitService, _joinableTaskFactory, _searchDebounce);

        public CreditLimitViewModel(
            IMapper autoMapper,
            IEventAggregator eventAggregator,
            INotificationService notificationService,
            ICreditLimitValidator validator,
            IRepository<CreditLimitGraphQLModel> creditLimitService,
            IBackgroundQueueService backgroundQueueService,
            JoinableTaskFactory joinableTaskFactory,
            DebouncedAction searchDebounce)
        {
            AutoMapper = autoMapper ?? throw new ArgumentNullException(nameof(autoMapper));
            EventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _creditLimitService = creditLimitService ?? throw new ArgumentNullException(nameof(creditLimitService));
            _backgroundQueueService = backgroundQueueService ?? throw new ArgumentNullException(nameof(backgroundQueueService));
            _searchDebounce = searchDebounce ?? throw new ArgumentNullException(nameof(searchDebounce));
            _joinableTaskFactory = joinableTaskFactory;
        }

        protected override async Task OnInitializedAsync(CancellationToken cancellationToken)
        {
            try
            {
                await ActivateItemAsync(CreditLimitMasterViewModel, cancellationToken);
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Error Inesperado!",
                    text: $"{GetType().Name}.{nameof(OnInitializedAsync)} \r\n{ex.GetErrorMessage()}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
            }

            await base.OnInitializedAsync(cancellationToken);
        }
    }
}
