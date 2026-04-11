using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Xpf.Core;
using Microsoft.VisualStudio.Threading;
using Models.Books;
using NetErp.Books.AccountingAccounts.Validators;
using NetErp.Helpers;
using NetErp.Helpers.Cache;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace NetErp.Books.AccountingAccounts.ViewModels
{
    public class AccountPlanViewModel : Conductor<object>.Collection.OneActive
    {
        #region Dependencies

        private readonly IRepository<AccountingAccountGraphQLModel> _accountingAccountService;
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly Helpers.IDialogService _dialogService;
        private readonly IEventAggregator _eventAggregator;
        private readonly StringLengthCache _stringLengthCache;
        private readonly PermissionCache _permissionCache;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly AccountPlanValidator _validator;
        private readonly DebouncedAction _searchDebounce;

        #endregion

        #region Constructor

        public AccountPlanViewModel(
            IRepository<AccountingAccountGraphQLModel> accountingAccountService,
            Helpers.Services.INotificationService notificationService,
            Helpers.IDialogService dialogService,
            IEventAggregator eventAggregator,
            StringLengthCache stringLengthCache,
            PermissionCache permissionCache,
            JoinableTaskFactory joinableTaskFactory,
            AccountPlanValidator validator,
            DebouncedAction searchDebounce)
        {
            _accountingAccountService = accountingAccountService;
            _notificationService = notificationService;
            _dialogService = dialogService;
            _eventAggregator = eventAggregator;
            _stringLengthCache = stringLengthCache;
            _permissionCache = permissionCache;
            _joinableTaskFactory = joinableTaskFactory;
            _validator = validator;
            _searchDebounce = searchDebounce;
        }

        #endregion

        #region Master ViewModel

        private AccountPlanMasterViewModel? _accountPlanMasterViewModel;

        public AccountPlanMasterViewModel AccountPlanMasterViewModel
        {
            get
            {
                _accountPlanMasterViewModel ??= new AccountPlanMasterViewModel(
                    _accountingAccountService,
                    _notificationService,
                    _dialogService,
                    _eventAggregator,
                    _stringLengthCache,
                    _permissionCache,
                    _joinableTaskFactory,
                    _validator,
                    _searchDebounce);
                return _accountPlanMasterViewModel;
            }
        }

        #endregion

        #region Lifecycle

#pragma warning disable CS0618, CS0672
        protected override async Task OnActivateAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _stringLengthCache.EnsureEntitiesLoadedAsync(StringLengthEntities.AccountingAccount);
                await ActivateItemAsync(AccountPlanMasterViewModel, cancellationToken);
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"Error al inicializar el módulo.\r\n{GetType().Name}.{nameof(OnActivateAsync)}: {ex.GetErrorMessage()}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
                await TryCloseAsync();
            }
            await base.OnActivateAsync(cancellationToken);
        }
#pragma warning restore CS0618, CS0672

        #endregion
    }
}
