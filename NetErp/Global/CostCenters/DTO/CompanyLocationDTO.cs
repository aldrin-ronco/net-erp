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
    public class CompanyLocationDTO: Screen, ICloneable, ICostCentersItems
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

        private string _name = string.Empty;
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

        private CompanyDTO _company;
        public CompanyDTO Company
        {
            get { return _company; }
            set
            {
                if (_company != value)
                {
                    _company = value;
                    NotifyOfPropertyChange(nameof(Company));
                }
            }
        }

        private ObservableCollection<object> _dummyItems = [];
        public ObservableCollection<object> DummyItems
        {
            get { return _dummyItems; }
            set
            {
                if (_dummyItems != value)
                {
                    _dummyItems = value;
                    NotifyOfPropertyChange(nameof(DummyItems));
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


        public override string ToString()
        {
            return Name;
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }

        public CompanyLocationDTO()
        {

        }
        public CompanyLocationDTO(int id, CompanyDTO company, string name, CostCenterMasterViewModel context)
        {
            this._id = id;
            this._name = name;
            this._company = company;
            this.Context = context;
        }
    }
}
