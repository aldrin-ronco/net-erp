using AutoMapper;
using Caliburn.Micro;
using Common.Interfaces;
using Models.Books;
using NetErp.Books.AccountingEntities.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetErp.Books.AccountingBooks.ViewModels
{
    public class AccountingBookViewModel: Conductor<object>.Collection.OneActive
    {
        public IMapper AutoMapper { get; private set; }
        public IEventAggregator EventAggregator { get; set; }
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly IRepository<AccountingBookGraphQLModel> _accountingBookService;
        private AccountingBookMasterViewModel _accountingBookMasterViewModel;
        private AccountingBookMasterViewModel AccountingBookMasterViewModel
        {
            get
            {
                if (_accountingBookMasterViewModel is null) _accountingBookMasterViewModel = new AccountingBookMasterViewModel(this, _notificationService, _accountingBookService);
                return _accountingBookMasterViewModel;

            }
        }
        public AccountingBookViewModel(IMapper mapper, IEventAggregator eventAggregator, Helpers.Services.INotificationService notificationService,
            IRepository<AccountingBookGraphQLModel> accountingBookService)
        {
            _accountingBookService = accountingBookService;
            _notificationService = notificationService;
            EventAggregator = eventAggregator;
            AutoMapper = mapper;

            _ = Task.Run(ActivateMasterViewAsync);
        }

        public async Task ActivateMasterViewAsync()
        {
            try
            {
                await ActivateItemAsync(AccountingBookMasterViewModel, new System.Threading.CancellationToken());
            }
            catch (Exception)
            {
                throw;
            }
        }
        public async Task ActivateDetailViewForEditAsync(AccountingBookGraphQLModel accountingBook)
        {
            try
            {
                AccountingBookDetailViewModel instance = new(this, _accountingBookService);
                instance.AccountingBookId = accountingBook.Id;
                instance.Name = accountingBook.Name;
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
                AccountingBookDetailViewModel instance = new(this, _accountingBookService);
                instance.CleanUpControls();
                await ActivateItemAsync(instance, new System.Threading.CancellationToken());
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
