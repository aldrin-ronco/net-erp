﻿using AutoMapper;
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
                if (_sellerMasterViewModel is null) _sellerMasterViewModel = new SellerMasterViewModel(this);
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

        public SellerViewModel(IMapper mapper,
                               IEventAggregator eventAggregator)
        {
            AutoMapper = mapper;
            EventAggregator = eventAggregator;
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
                SellerDetailViewModel instance = new SellerDetailViewModel(this);
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
                SellerDetailViewModel instance = new SellerDetailViewModel(this);
                await instance.Initialize();
                instance.Id = seller.Id;
                instance.SelectedIdentificationType = IdentificationTypes.FirstOrDefault(x => x.Code == "13");
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
                instance.SelectedCountry = Countries.FirstOrDefault(c => c.Id == seller.Entity.Country.Id);
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
