using Caliburn.Micro;
using Models.Global;
using NetErp.Global.CostCenters.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetErp.Global.CostCenters.DTO
{
    public class StorageDTO: Screen, ICostCentersItems, ICloneable
    {
        public CostCenterMasterViewModel Context { get; set; }

        private int _id;

        public int Id
        {
            get { return _id; }
            set 
            {
                if (_id != value)
                {
                    _id = value;
                    NotifyOfPropertyChange(nameof(Id));
                }
            }
        }

        private string _name;

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

        private string _state;

        public string State
        {
            get { return _state; }
            set 
            {
                if (_state != value)
                {
                    _state = value;
                    NotifyOfPropertyChange(nameof(State));
                }
            }
        }


        private string _address;

        public string Address
        {
            get { return _address; }
            set 
            {
                if (_address != value)
                {
                    _address = value;
                    NotifyOfPropertyChange(nameof(Address));
                }
            }
        }

        private CityDTO _city;

        public CityDTO City
        {
            get { return _city; }
            set
            {
                if (_city != value)
                {
                    _city = value;
                    NotifyOfPropertyChange(nameof(City));
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

        private bool _isExpanded = false;

        public bool IsExpanded
        {
            get { return _isExpanded; }
            set 
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    NotifyOfPropertyChange(nameof(IsExpanded));
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
        public StorageDTO() { }

        public StorageDTO(int id, string name)
        {
            _id = id;
            _name = name;
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}
