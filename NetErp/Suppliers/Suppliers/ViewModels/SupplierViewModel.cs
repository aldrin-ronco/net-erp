using AutoMapper;
using Caliburn.Micro;
using Common.Extensions;
using Common.Interfaces;
using Models.Books;
using Models.DTO.Global;
using Models.Suppliers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Dictionaries.BooksDictionaries;

namespace NetErp.Suppliers.Suppliers.ViewModels
{
    public class SupplierViewModel : Conductor<Screen>.Collection.OneActive
    {
        public IMapper AutoMapper { get; private set; }
        public IEventAggregator EventAggregator { get; private set; }

        private SupplierMasterViewModel _supplierMasterViewModel;

        public SupplierMasterViewModel SupplierMasterViewModel
        {
            get 
            {
                if (_supplierMasterViewModel is null) _supplierMasterViewModel = new(this);
                return _supplierMasterViewModel; 
            }
            
        }

        public SupplierViewModel(IEventAggregator eventAggregator,
                                 IMapper mapper)
        {
            EventAggregator = eventAggregator;
            AutoMapper = mapper;
            _ = Task.Run(() => ActivateMasterView());
        }

        public async Task ActivateMasterView()
        {
            try
            {
                await ActivateItemAsync(SupplierMasterViewModel, new System.Threading.CancellationToken());
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task ActivateDetailViewForNew()
        {
            SupplierDetailViewModel instance = new(this);
            instance.CleanUpControls();
            await ActivateItemAsync(instance, new System.Threading.CancellationToken());
        }

        public async Task ActivateDetailViewForEdit(SupplierGraphQLModel supplier)
        {
            SupplierDetailViewModel instance = new(this);
            List<RetentionTypeDTO> retentionList = new List<RetentionTypeDTO>();
            instance.Id = supplier.Id;
            instance.SelectedCaptureType = (CaptureTypeEnum)Enum.Parse(typeof(CaptureTypeEnum), supplier.Entity.CaptureType);
            instance.SelectedIdentificationType = instance.IdentificationTypes.FirstOrDefault(x => x.Id == supplier.Entity.IdentificationType.Id);
            instance.FirstName = supplier.Entity.FirstName;
            instance.MiddleName = supplier.Entity.MiddleName;
            instance.FirstLastName = supplier.Entity.FirstLastName;
            instance.MiddleLastName = supplier.Entity.MiddleLastName;
            instance.Phone1 = supplier.Entity.Phone1;
            instance.Phone2 = supplier.Entity.Phone2;
            instance.CellPhone1 = supplier.Entity.CellPhone1;
            instance.CellPhone2 = supplier.Entity.CellPhone2;
            instance.BusinessName = supplier.Entity.BusinessName;
            instance.Address = supplier.Entity.Address;
            instance.IdentificationNumber = supplier.Entity.IdentificationNumber;
            instance.VerificationDigit = supplier.Entity.VerificationDigit;
            instance.SelectedCountry = instance.Countries.FirstOrDefault(c => c.Id == supplier.Entity.Country.Id);
            instance.SelectedDepartment = instance.SelectedCountry.Departments.FirstOrDefault(d => d.Id == supplier.Entity.Department.Id);
            instance.SelectedCityId = supplier.Entity.City.Id;
            instance.Emails = supplier.Entity.Emails is null ? new System.Collections.ObjectModel.ObservableCollection<EmailDTO>() : new System.Collections.ObjectModel.ObservableCollection<EmailDTO>(supplier.Entity.Emails.Select(x => x.Clone()).ToList()); // Este codigo copia la lista sin mantener referencia a la lista original

            foreach (RetentionTypeDTO retention in instance.RetentionTypes)
            {
                bool exist = !(supplier.Retentions is null) && supplier.Retentions.Any(x => x.Id == retention.Id);
                retentionList.Add(new RetentionTypeDTO()
                {
                    Id = retention.Id,
                    Name = retention.Name,
                    Margin = retention.Margin,
                    InitialBase = retention.InitialBase,
                    AccountingAccountSale = retention.AccountingAccountSale,
                    AccountingAccountPurchase = retention.AccountingAccountPurchase,
                    IsSelected = exist
                });
            }
            instance.RetentionTypes = new System.Collections.ObjectModel.ObservableCollection<RetentionTypeDTO>(retentionList);
            await ActivateItemAsync(instance, new System.Threading.CancellationToken());
        }

    }
}
