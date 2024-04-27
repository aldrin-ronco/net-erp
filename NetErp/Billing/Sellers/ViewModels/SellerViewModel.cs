using AutoMapper;
using Caliburn.Micro;
using Common.Extensions;
using Common.Interfaces;
using Extensions.Common;
using Interfaces.Billing.BillingSellers;
using Models.Billing;
using Models.Books;
using Models.DTO.Global;
using Models.Global;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace NetErp.Billing.Sellers.ViewModels
{
    public class SellerViewModel : Conductor<Screen>.Collection.OneActive
    {
        public IMapper AutoMapper { get; private set; }
        public IEventAggregator EventAggregator { get; private set; }     

        private ObservableCollection<IdentificationTypeGraphQLModel> _identificationTypes;
        public ObservableCollection<IdentificationTypeGraphQLModel> IdentificationTypes
        {
            get => _identificationTypes;
            set
            {
                if (_identificationTypes != value)
                {
                    _identificationTypes = value;
                    NotifyOfPropertyChange(nameof(IdentificationTypes));
                }
            }
        }

        private ObservableCollection<CostCenterDTO> _costCenters;
        public ObservableCollection<CostCenterDTO> CostCenters
        {
            get => _costCenters;
            set
            {
                if (_costCenters != value)
                {
                    _costCenters = value;
                    NotifyOfPropertyChange(nameof(CostCenters));
                }
            }
        }

        public SellerMasterViewModel SellerMasterViewModel { get; private set; }

        public SellerViewModel(IMapper mapper,
                               IEventAggregator eventAggregator)
        {
            AutoMapper = mapper;
            EventAggregator = eventAggregator;
            _ = Task.Run(() => ActivateMasterView());
        }

        public async Task ActivateMasterView()
        {
            if (SellerMasterViewModel is null) SellerMasterViewModel = new SellerMasterViewModel(this);
            await ActivateItemAsync(SellerMasterViewModel, new System.Threading.CancellationToken());
        }

        public async Task ActivateDetailViewForNew()
        {
            SellerDetailViewModel instance = new SellerDetailViewModel(this);
            instance.CleanUpControls();
            await ActivateItemAsync(instance, new System.Threading.CancellationToken());
        }

        public async Task ActivateDetailViewForEdit(SellerGraphQLModel seller)
        {
            ObservableCollection<CostCenterDTO> costCentersSelection = new ObservableCollection<CostCenterDTO>();
            SellerDetailViewModel instance = new SellerDetailViewModel(this);
            instance.Id = seller.Id;
            instance.SelectedIdentificationType = instance.IdentificationTypes.FirstOrDefault(x => x.Id == seller.Entity.IdentificationType.Id);
            instance.IdentificationNumber = seller.Entity.IdentificationNumber;
            instance.FirstName = seller.Entity.FirstName;
            instance.MiddleName = seller.Entity.MiddleName;
            instance.FirstLastName = seller.Entity.FirstLastName;
            instance.MiddleLastName = seller.Entity.MiddleLastName;
            instance.Phone1 = seller.Entity.Phone1;
            instance.Phone2 = seller.Entity.Phone2;
            instance.CellPhone1 = seller.Entity.CellPhone1;
            instance.CellPhone2 = seller.Entity.CellPhone2;
            instance.Emails = seller.Entity.Emails is null ? new ObservableCollection<EmailDTO>() : new ObservableCollection<EmailDTO>(seller.Entity.Emails.Select(x => x.Clone()).ToList());
            instance.SelectedCountry = instance.Countries.FirstOrDefault(c => c.Id == seller.Entity.Country.Id);
            instance.SelectedDepartment = instance.SelectedCountry.Departments.FirstOrDefault(d => d.Id == seller.Entity.Department.Id);
            instance.SelectedCityId = seller.Entity.City.Id;
            instance.Address = seller.Entity.Address;
            foreach (CostCenterDTO costCenter in CostCenters)
            {
                bool exist = !(seller.CostCenters is null) && seller.CostCenters.Any(c => c.Id == costCenter.Id);
                costCentersSelection.Add(new CostCenterDTO()
                {
                    Id = costCenter.Id,
                    Name = costCenter.Name,
                    IsSelected = exist
                });
            }
            instance.CostCenters = new ObservableCollection<CostCenterDTO>(costCentersSelection);
            await ActivateItemAsync(instance, new System.Threading.CancellationToken());
        }
    }
}
