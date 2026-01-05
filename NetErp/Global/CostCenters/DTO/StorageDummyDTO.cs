using Caliburn.Micro;
using NetErp.Global.CostCenters.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetErp.Global.CostCenters.DTO
{
    public class StorageDummyDTO : Screen, ICostCentersItems
    {
        public CostCenterMasterViewModel Context { get; set; }

        public int Id
        {
            get { return 2; }
        }
    
        private string _name = "BODEGAS";

		public string Name
		{
			get { return _name; }
			set 
			{
                if (_name != value)
                {
                    _name = value;
                    NotifyOfPropertyChange(nameof(Name));
                }
			}
		}

        private ObservableCollection<StorageDTO> _storages;

        public ObservableCollection<StorageDTO> Storages
        {
            get { return _storages; }
            set 
            {
                if (_storages != value)
                {
                    _storages = value;
                    NotifyOfPropertyChange(nameof(Storages));
                }
            }
        }


        private bool _isDummyChild = false;
        public bool IsDummyChild
        {
            get { return _isDummyChild; }
            set
            {
                if (_isDummyChild != value)
                {
                    _isDummyChild = value;
                    NotifyOfPropertyChange(nameof(IsDummyChild));
                }
            }
        }

        private bool _isExpanded;

        public bool IsExpanded
        {
            get { return _isExpanded; }
            set 
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    NotifyOfPropertyChange(nameof(IsExpanded));
                    if (_storages != null)
                    {
                        if (_isExpanded && _storages.Count > 0)
                        {
                            if (_storages[0].IsDummyChild)
                            {
                                _ = Context.LoadStoragesAsync(Location, this);
                            }
                        }
                    }
                }
            }
        }

        private CompanyLocationDTO _location;

        public CompanyLocationDTO Location
        {
            get { return _location; }
            set 
            {
                if (_location != value)
                {
                    _location = value;
                    NotifyOfPropertyChange(nameof(Location));
                }
            }
        }



        public StorageDummyDTO() { }

        public StorageDummyDTO(CostCenterMasterViewModel context, CompanyLocationDTO location)
        {
            this.Context = context;
            this._location = location;
            this._storages = [new() { IsDummyChild = true, Name = "Fucking Dummy" }];
        }

    }
}
