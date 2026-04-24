using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Xpf.Core;
using Microsoft.VisualStudio.Threading;
using Models.Billing;
using Models.Books;
using NetErp.Billing.CreditLimit.ViewModels;
using NetErp.Helpers.Cache;
using NetErp.Helpers.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NetErp.Books.AccountingPeriods.ViewModels
{
    public class AccountingPeriodViewModel : Conductor<object>.Collection.OneActive
    {
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly IRepository<AccountingPeriodGraphQLModel> _accountingPeriodService;
        private AccountingPeriodMasterViewModel? _accountingPeriodMasterViewModel;
        private readonly CostCenterCache _costCenterCache;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly IRepository<AccountingEntryPeriodGraphQLModel> _accountingEntryPeriodService;
        private readonly Helpers.IDialogService _dialogService;
        private readonly IEventAggregator _eventAggregator;

        public AccountingPeriodMasterViewModel? AccountingPeriodMasterViewModel
        {
            get
            {
                _accountingPeriodMasterViewModel ??= new AccountingPeriodMasterViewModel(_costCenterCache, _notificationService, _joinableTaskFactory, _accountingPeriodService, _accountingEntryPeriodService, _dialogService, _eventAggregator);
                return _accountingPeriodMasterViewModel;
            }
        }
        public AccountingPeriodViewModel(Helpers.Services.INotificationService notificationService, CostCenterCache costCenterCache,
            JoinableTaskFactory joinableTaskFactory,
            IBackgroundQueueService backgroundQueueService,
            IRepository<AccountingPeriodGraphQLModel> accountingPeriodService,
            IRepository<AccountingEntryPeriodGraphQLModel> accountingEntryPeriodService,
            Helpers.IDialogService dialogService,
            IEventAggregator eventAggregator)
        {
            _costCenterCache = costCenterCache;
            _joinableTaskFactory = joinableTaskFactory;
            _accountingPeriodService = accountingPeriodService;
            _notificationService = notificationService;
            _accountingEntryPeriodService = accountingEntryPeriodService;
            _dialogService = dialogService;
                        _eventAggregator = eventAggregator;
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
                await ActivateItemAsync(AccountingPeriodMasterViewModel ?? new AccountingPeriodMasterViewModel(_costCenterCache, _notificationService, _joinableTaskFactory, _accountingPeriodService, _accountingEntryPeriodService, _dialogService, _eventAggregator), new System.Threading.CancellationToken());
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }

        }
    }
}
