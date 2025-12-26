using AutoMapper;
using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Xpf.Core;
using Models.Billing;
using Models.Books;
using Models.DTO.Global;
using Models.Global;
using NetErp.Billing.Customers.ViewModels;
using NetErp.Billing.Zones.DTO;
using NetErp.Global.CostCenters.DTO;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace NetErp.Billing.Sellers.ViewModels
{
    public class SellerViewModel : Conductor<Screen>.Collection.OneActive
    {
        public IMapper AutoMapper { get; private set; }
        public IEventAggregator EventAggregator { get; private set; }
        private readonly IRepository<SellerGraphQLModel> _sellerService;
        private readonly IRepository<ZoneGraphQLModel> _zoneService;
        private readonly Helpers.Services.INotificationService _notificationService;     

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
        private ObservableCollection<ZoneDTO> _zones;
        public ObservableCollection<ZoneDTO> Zones
        {
            get => _zones;
            set
            {
                if (_zones != value)
                {
                    _zones = value;
                    NotifyOfPropertyChange(nameof(Zones));
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

        private ObservableCollection<CountryGraphQLModel> _countries;
        public ObservableCollection<CountryGraphQLModel> Countries
        {
            get => _countries;
            set
            {
                if (_countries != value)
                {
                    _countries = value;
                    NotifyOfPropertyChange(nameof(Countries));
                }
            }
        }

        private SellerMasterViewModel _sellerMasterViewModel;
        public SellerMasterViewModel SellerMasterViewModel 
        { 
            get 
            {
                if (_sellerMasterViewModel is null) _sellerMasterViewModel = new SellerMasterViewModel(this, _sellerService, _notificationService);
                return _sellerMasterViewModel;
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

        public SellerViewModel(
            IMapper mapper,
            IEventAggregator eventAggregator,
            IRepository<SellerGraphQLModel> sellerService,
            IRepository<ZoneGraphQLModel> zoneService,
            Helpers.Services.INotificationService notificationService)
        {
            AutoMapper = mapper;
            EventAggregator = eventAggregator;
            _sellerService = sellerService;
            _zoneService = zoneService;
            _notificationService = notificationService;
            _ = Task.Run(async () => 
            {
                try
                {
                    await ActivateMasterViewAsync();
                }
                catch (AsyncException ex)
                {
                    await Execute.OnUIThreadAsync(() =>
                    {
                        ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{ex.MethodOrigin} \r\n{ex.InnerException?.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                        return Task.CompletedTask;
                    });
                }
                catch (Exception ex)
                {
                    await Execute.OnUIThreadAsync(() =>
                    {
                        ThemedMessageBox.Show(title: "Error de inicialización", text: $"Error al activar vista de vendedores: {ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                        return Task.CompletedTask;
                    });
                }
            });
        }

        public async Task ActivateMasterViewAsync()
        {
            try
            {
                await ActivateItemAsync(SellerMasterViewModel, new System.Threading.CancellationToken());
            }
            catch (Exception ex)
            {

                throw new AsyncException(innerException: ex);
            }
        }


        public async Task ActivateDetailViewForNew()
        {
            try
            {
                SellerDetailViewModel instance = new SellerDetailViewModel(this, _sellerService, _zoneService);
                await instance.Initialize();
                instance.CleanUpControls();
                
                await ActivateItemAsync(instance, new System.Threading.CancellationToken());
            }
            catch (AsyncException ex)
            {
                await Execute.OnUIThreadAsync(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{ex.MethodOrigin} \r\n{ex.InnerException?.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return Task.CompletedTask;
                });
            }
            catch (Exception ex)
            {

                throw new AsyncException(innerException: ex);
            }
        }

        public async Task ActivateDetailViewForEdit(SellerGraphQLModel seller)
        {
            try
            {
                ObservableCollection<CostCenterDTO> costCentersSelection = new ObservableCollection<CostCenterDTO>();
                SellerDetailViewModel instance = new SellerDetailViewModel(this, _sellerService, _zoneService);
                await instance.Initialize();
                instance.Id = seller.Id;
                instance.SelectedIdentificationType = IdentificationTypes.FirstOrDefault(x => x.Code == seller.AccountingEntity.IdentificationType.Code);
                instance.IdentificationNumber = seller.AccountingEntity.IdentificationNumber;
                instance.FirstName = seller.AccountingEntity.FirstName;
                instance.MiddleName = seller.AccountingEntity.MiddleName;
                instance.FirstLastName = seller.AccountingEntity.FirstLastName;
                instance.MiddleLastName = seller.AccountingEntity.MiddleLastName;
                instance.PrimaryPhone = seller.AccountingEntity.PrimaryPhone;
                instance.SecondaryPhone = seller.AccountingEntity.SecondaryPhone;
                instance.PrimaryCellPhone = seller.AccountingEntity.PrimaryCellPhone;
                instance.SecondaryCellPhone = seller.AccountingEntity.SecondaryCellPhone;
                instance.Emails = seller.AccountingEntity.Emails is null ? new ObservableCollection<EmailDTO>() : new ObservableCollection<EmailDTO>(seller.AccountingEntity.Emails.Select(x => x.Clone()).ToList());
                instance.SelectedCountry = Countries.FirstOrDefault(c => c.Id == seller.AccountingEntity.Country.Id);
                instance.SelectedDepartment = instance.SelectedCountry.Departments.FirstOrDefault(d => d.Id == seller.AccountingEntity.Department.Id);
                instance.SelectedCityId = seller.AccountingEntity.City.Id;
                instance.Address = seller.AccountingEntity.Address;
                instance.ZoneId = seller.Zone?.Id;
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
               
                instance.Zones = [.. Zones];

                instance.CostCenters = costCentersSelection;
                instance.AcceptChanges();
                await ActivateItemAsync(instance, new System.Threading.CancellationToken());
            }
            catch (AsyncException ex)
            {
                await Execute.OnUIThreadAsync(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{ex.MethodOrigin} \r\n{ex.InnerException?.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return Task.CompletedTask;
                });
            }
            catch (Exception ex)
            {

                throw new AsyncException(innerException: ex);
            }
        }
    }
}
