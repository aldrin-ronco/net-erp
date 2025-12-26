using AutoMapper;
using Caliburn.Micro;
using Common.Extensions;
using Common.Interfaces;
using Models.Books;
using Models.DTO.Global;
using Models.Suppliers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using static Dictionaries.BooksDictionaries;

namespace NetErp.Suppliers.Suppliers.ViewModels
{
    public class SupplierViewModel : Conductor<Screen>.Collection.OneActive
    {
        public IMapper AutoMapper { get; private set; }
        public IEventAggregator EventAggregator { get; private set; }
        
        private readonly IRepository<SupplierGraphQLModel> _supplierService;
        private readonly Helpers.Services.INotificationService _notificationService;

        private SupplierMasterViewModel _supplierMasterViewModel;

        public SupplierMasterViewModel SupplierMasterViewModel
        {
            get 
            {
                if (_supplierMasterViewModel is null) _supplierMasterViewModel = new(this, _supplierService, _notificationService);
                return _supplierMasterViewModel; 
            }
            
        }

        public SupplierViewModel(
            IEventAggregator eventAggregator,
            IMapper mapper,
            IRepository<SupplierGraphQLModel> supplierService,
            Helpers.Services.INotificationService notificationService)
        {
            EventAggregator = eventAggregator;
            AutoMapper = mapper;
            _supplierService = supplierService;
            _notificationService = notificationService;
            _ = ActivateMasterView();
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
            SupplierDetailViewModel instance = new(this, _supplierService);
            instance.CleanUpControlsForNew();
            await ActivateItemAsync(instance, new System.Threading.CancellationToken());
        }

        public async Task ActivateDetailViewForEdit(SupplierGraphQLModel supplier)
        {
            SupplierDetailViewModel instance = new(this, _supplierService);
            List<WithholdingTypeDTO> withholdingTypes = [];
            instance.Id = supplier.Id;
            instance.SelectedCaptureType = (CaptureTypeEnum)Enum.Parse(typeof(CaptureTypeEnum), supplier.Entity.CaptureType);
            instance.SelectedIdentificationType = instance.IdentificationTypes.FirstOrDefault(x => x.Id == supplier.Entity.IdentificationType.Id);
            instance.FirstName = supplier.Entity.FirstName;
            instance.MiddleName = supplier.Entity.MiddleName;
            instance.FirstLastName = supplier.Entity.FirstLastName;
            instance.MiddleLastName = supplier.Entity.MiddleLastName;
            instance.PrimaryPhone = supplier.Entity.PrimaryPhone;
            instance.SecondaryPhone = supplier.Entity.SecondaryPhone;
            instance.PrimaryCellPhone = supplier.Entity.PrimaryCellPhone;
            instance.SecondaryCellPhone = supplier.Entity.SecondaryCellPhone;
            instance.BusinessName = supplier.Entity.BusinessName;
            instance.Address = supplier.Entity.Address;
            instance.IdentificationNumber = supplier.Entity.IdentificationNumber;
            instance.VerificationDigit = supplier.Entity.VerificationDigit;
            instance.SelectedCountry = instance.Countries.FirstOrDefault(c => c.Id == supplier.Entity.Country.Id);
            instance.SelectedDepartment = instance.SelectedCountry.Departments.FirstOrDefault(d => d.Id == supplier.Entity.Department.Id);
            instance.SelectedCityId = supplier.Entity.City.Id;
            instance.Emails = supplier.Entity.Emails is null ? [] : new ObservableCollection<EmailDTO>(AutoMapper.Map<ObservableCollection<EmailDTO>>(supplier.Entity.Emails)); // Este codigo copia la lista sin mantener referencia a la lista original

            foreach (WithholdingTypeDTO retention in instance.WithholdingTypes)
            {
                bool exist = !(supplier.Retentions is null) && supplier.Retentions.Any(x => x.Id == retention.Id);
                withholdingTypes.Add(new WithholdingTypeDTO()
                {
                    Id = retention.Id,
                    Name = retention.Name,
                    IsSelected = exist
                });
            }
            instance.WithholdingTypes = new System.Collections.ObjectModel.ObservableCollection<WithholdingTypeDTO>(withholdingTypes);
            await ActivateItemAsync(instance, new System.Threading.CancellationToken());
        }

    }
}
