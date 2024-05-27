using AutoMapper;
using Caliburn.Micro;
using Common.Extensions;
using Common.Interfaces;
using Models.Books;
using Models.DTO.Global;
using NetErp.Books.AccountingAccounts.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Dictionaries.BooksDictionaries;

namespace NetErp.Books.AccountingEntities.ViewModels
{
    public class AccountingEntityViewModel : Conductor<object>.Collection.OneActive
    {
        public IMapper AutoMapper { get; private set; }

        public IEventAggregator EventAggregator { get; set; }

        // MasterVM
        private AccountingEntityMasterViewModel _accountingEntityMasterViewModel;
        public AccountingEntityMasterViewModel AccountingEntityMasterViewModel
        {
            get
            {
                if (_accountingEntityMasterViewModel is null) _accountingEntityMasterViewModel = new AccountingEntityMasterViewModel(this);
                return _accountingEntityMasterViewModel;
            }
        }

        private bool _enableOnViewReady = true;

        public bool EnableOnViewReady
        {
            get { return _enableOnViewReady; }
            set
            {
                _enableOnViewReady = value;
            }
        }

        public AccountingEntityViewModel(IMapper mapper,
                                         IEventAggregator eventAggregator)
        {
            EventAggregator = eventAggregator;
            AutoMapper = mapper;
            Task.Run(ActivateMasterView);
        }

        public async Task ActivateMasterView()
        {
            try
            {
                await ActivateItemAsync(AccountingEntityMasterViewModel, new System.Threading.CancellationToken());
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task ActivateDetailViewForEdit(AccountingEntityGraphQLModel selectedItem)
        {
            AccountingEntityDetailViewModel instance = new(this);
            instance.SelectedIdentificationType = instance.IdentificationTypes.FirstOrDefault(x => x.Id == selectedItem.IdentificationType.Id);
            instance.Id = selectedItem.Id;
            instance.VerificationDigit = selectedItem.VerificationDigit;
            instance.SelectedRegime = selectedItem.Regime;
            instance.IdentificationNumber = selectedItem.IdentificationNumber;
            instance.SelectedCaptureType = (CaptureTypeEnum)Enum.Parse(typeof(CaptureTypeEnum), selectedItem.CaptureType);
            instance.BusinessName = selectedItem.BusinessName;
            instance.FirstName = selectedItem.FirstName;
            instance.MiddleName = selectedItem.MiddleName;
            instance.FirstLastName = selectedItem.FirstLastName;
            instance.MiddleLastName = selectedItem.MiddleLastName;
            instance.Phone1 = selectedItem.Phone1;
            instance.Phone2 = selectedItem.Phone2;
            instance.CellPhone1 = selectedItem.CellPhone1;
            instance.CellPhone2 = selectedItem.CellPhone2;
            instance.Address = selectedItem.Address;
            instance.SelectedCountry = instance.Countries.FirstOrDefault(c => c.Id == selectedItem.Country.Id);
            instance.SelectedDepartment = instance.SelectedCountry.Departments.FirstOrDefault(d => d.Id == selectedItem.Department.Id);
            instance.SelectedCityId = selectedItem.City.Id;
            instance.Emails = new ObservableCollection<EmailDTO>(selectedItem.Emails.Select(x => x.Clone()).ToList()); // Este codigo copia la lista sin mantener referencia a la lista original
            await ActivateItemAsync(instance, new System.Threading.CancellationToken());
        }

        public async Task ActivateDetailViewForNew()
        {
            try
            {
                AccountingEntityDetailViewModel instance = new(this);
                instance.CleanUpControlsForNew();
                await ActivateItemAsync(instance, new System.Threading.CancellationToken());
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
