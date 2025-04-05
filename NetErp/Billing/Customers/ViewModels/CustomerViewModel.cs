using AutoMapper;
using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using DevExpress.Xpf.Core;
using Models.Billing;
using Models.Books;
using Models.DTO.Global;
using System;
using System.Collections.Generic;
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

        private CustomerMasterViewModel? _customerMasterViewModel;
        public CustomerMasterViewModel? CustomerMasterViewModel
        {
            get
            {
                if (_customerMasterViewModel is null) _customerMasterViewModel = new CustomerMasterViewModel(this);
                return _customerMasterViewModel;
            }
        }

        public CustomerViewModel(IMapper mapper,
                                 IEventAggregator eventAggregator)
        {
            EventAggregator = eventAggregator;
            AutoMapper = mapper;
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
            });
        }

        public async Task ActivateMasterViewAsync()
        {
            try
            {
                await ActivateItemAsync(CustomerMasterViewModel ?? new CustomerMasterViewModel(this), new System.Threading.CancellationToken());
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


        public async Task ActivateDetailViewForEdit(CustomerGraphQLModel customer)
        {
            try
            {
                CustomerDetailViewModel instance = new(this);
                await instance.Initialize();
                List<RetentionTypeDTO> retentionList = new List<RetentionTypeDTO>();
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

        public async Task ActivateDetailViewForNew()
        {
            try
            {
                CustomerDetailViewModel instance = new(this);
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
