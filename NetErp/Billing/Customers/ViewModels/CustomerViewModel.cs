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
                await instance.InitializeAsync();
                ObservableCollection<ZoneDTO> zonesSelection = [];
                List<WithholdingTypeDTO> withholdingTypes = [];
                Application.Current.Dispatcher.Invoke(() =>
                {
                    instance.SelectedCaptureType = (CaptureTypeEnum)Enum.Parse(typeof(CaptureTypeEnum), customer.AccountingEntity.CaptureType); 
                    instance.SelectedIdentificationType = instance.IdentificationTypes.FirstOrDefault(x => x.Id == customer.AccountingEntity.IdentificationType.Id);
                    instance.Id = customer.Id;
                    instance.FirstName = customer.AccountingEntity.FirstName;
                    instance.MiddleName = customer.AccountingEntity.MiddleName;
                    instance.FirstLastName = customer.AccountingEntity.FirstLastName;
                    instance.MiddleLastName = customer.AccountingEntity.MiddleLastName;
                    instance.PrimaryPhone = customer.AccountingEntity.PrimaryPhone;
                    instance.SecondaryPhone = customer.AccountingEntity.SecondaryPhone;
                    instance.PrimaryCellPhone = customer.AccountingEntity.PrimaryCellPhone;
                    instance.SecondaryCellPhone = customer.AccountingEntity.SecondaryCellPhone;
                    instance.BusinessName = customer.AccountingEntity.BusinessName;
                    instance.Address = customer.AccountingEntity.Address;
                    instance.Emails = customer.AccountingEntity.Emails is null ? new System.Collections.ObjectModel.ObservableCollection<EmailDTO>() : new System.Collections.ObjectModel.ObservableCollection<EmailDTO>(customer.AccountingEntity.Emails.Select(x => x.Clone()).ToList()); // Este codigo copia la lista sin mantener referencia a la lista original
                    instance.IdentificationNumber = customer.AccountingEntity.IdentificationNumber;
                    instance.VerificationDigit = customer.AccountingEntity.VerificationDigit;
                    instance.SelectedCountry = instance.Countries.FirstOrDefault(c => c.Id == customer.AccountingEntity.Country.Id);
                    instance.SelectedDepartment = instance.SelectedCountry.Departments.FirstOrDefault(d => d.Id == customer.AccountingEntity.Department.Id);
                    instance.SelectedCityId = customer.AccountingEntity.City.Id;
                    foreach (WithholdingTypeDTO retention in instance.WithholdingTypes)
                    {
                        bool exist = customer.WithholdingTypes is null ? false : customer.WithholdingTypes.Any(x => x.Id == retention.Id);
                        withholdingTypes.Add(new WithholdingTypeDTO()
                        {
                            Id = retention.Id,
                            Name = retention.Name,
                            IsSelected = exist
                        });
                    }
                    instance.WithholdingTypes = new System.Collections.ObjectModel.ObservableCollection<WithholdingTypeDTO>(withholdingTypes);
                    instance.SelectedZone = instance.Zones.FirstOrDefault(z => z.Id == customer.Zone.Id);

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
                await instance.InitializeAsync();
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
