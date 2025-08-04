using AutoMapper;
using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Xpf.Core;
using Models.Billing;
using Models.Books;
using Models.DTO.Global;
using NetErp.Billing.Zones.DTO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Dictionaries.BooksDictionaries;

namespace NetErp.Billing.Customers.ViewModels
{
    public class CustomerViewModel : Conductor<object>.Collection.OneActive
    {
        public IMapper AutoMapper { get; private set; }
        public IEventAggregator EventAggregator { get; set; }

        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly IRepository<CustomerGraphQLModel> _customerService;
        
        private CustomerMasterViewModel? _customerMasterViewModel;
        public CustomerMasterViewModel? CustomerMasterViewModel
        {
            get
            {
                if (_customerMasterViewModel is null) _customerMasterViewModel = new CustomerMasterViewModel(this, _notificationService, _customerService);
                return _customerMasterViewModel;
            }
        }

        public CustomerViewModel(IMapper mapper,
                                 IEventAggregator eventAggregator,
                                 Helpers.Services.INotificationService notificationService,
                                 IRepository<CustomerGraphQLModel> customerService)
        {
            EventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            AutoMapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
        }

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            _ = ActivateMasterViewAsync();
        }

        public async Task ActivateMasterViewAsync()
        {
            try
            {
                await ActivateItemAsync(CustomerMasterViewModel ?? new CustomerMasterViewModel(this, _notificationService, _customerService), new System.Threading.CancellationToken());
            }
            catch (Exception ex)
            {

                throw new AsyncException(innerException: ex);
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


        public async Task ActivateDetailViewForEditAsync(CustomerGraphQLModel customer)
        {
            try
            {
                CustomerDetailViewModel instance = new(this, _customerService);
                await instance.Initialize();
                List<RetentionTypeDTO> retentionList = new List<RetentionTypeDTO>();
                ObservableCollection<ZoneDTO> zonesSelection = new ObservableCollection<ZoneDTO>();
                List<RetentionTypeDTO> retentionList = [];
                Application.Current.Dispatcher.Invoke(() =>
                {
                    instance.SelectedCaptureType = (CaptureTypeEnum)Enum.Parse(typeof(CaptureTypeEnum), customer.Entity.CaptureType); 
                    instance.SelectedIdentificationType = instance.IdentificationTypes.FirstOrDefault(x => x.Id == customer.Entity.IdentificationType.Id);
                    instance.Id = customer.Id;
                    instance.FirstName = customer.Entity.FirstName;
                    instance.MiddleName = customer.Entity.MiddleName;
                    instance.FirstLastName = customer.Entity.FirstLastName;
                    instance.MiddleLastName = customer.Entity.MiddleLastName;
                    instance.Phone1 = customer.Entity.Phone1;
                    instance.Phone2 = customer.Entity.Phone2;
                    instance.CellPhone1 = customer.Entity.CellPhone1;
                    instance.CellPhone2 = customer.Entity.CellPhone2;
                    instance.BusinessName = customer.Entity.BusinessName;
                    instance.Address = customer.Entity.Address;
                    instance.Emails = customer.Entity.Emails is null ? new System.Collections.ObjectModel.ObservableCollection<EmailDTO>() : new System.Collections.ObjectModel.ObservableCollection<EmailDTO>(customer.Entity.Emails.Select(x => x.Clone()).ToList()); // Este codigo copia la lista sin mantener referencia a la lista original
                    instance.IdentificationNumber = customer.Entity.IdentificationNumber;
                    instance.VerificationDigit = customer.Entity.VerificationDigit;
                    instance.SelectedCountry = instance.Countries.FirstOrDefault(c => c.Id == customer.Entity.Country.Id);
                    instance.SelectedDepartment = instance.SelectedCountry.Departments.FirstOrDefault(d => d.Id == customer.Entity.Department.Id);
                    instance.SelectedCityId = customer.Entity.City.Id;
                    foreach (RetentionTypeDTO retention in instance.RetentionTypes)
                    {
                        bool exist = customer.Retentions is null ? false : customer.Retentions.Any(x => x.Id == retention.Id);
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
                    foreach (ZoneGraphQLModel zone in instance.ZoneGraphQLModels)
                    {
                        bool exist = !(customer.Zones is null) && customer.Zones.Any(c => c.Id == zone.Id);
                        zonesSelection.Add(new ZoneDTO()
                        {
                            Id = zone.Id,
                            Name = zone.Name,
                            IsSelected = exist
                        });
                    }
                    instance.Zones = new ObservableCollection<ZoneDTO>(zonesSelection);

                });
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

        public async Task ActivateDetailViewForNewAsync()
        {
            try
            {
                CustomerDetailViewModel instance = new(this, _customerService);
                await instance.Initialize();
                instance.CleanUpControls();
                await ActivateItemAsync(instance, new System.Threading.CancellationToken());
            }
            catch(AsyncException ex)
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
