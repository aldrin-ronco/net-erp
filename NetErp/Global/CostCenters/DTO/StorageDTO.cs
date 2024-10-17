using Caliburn.Micro;
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


        public StorageDTO() { }

        public StorageDTO(int id, string name, CostCenterMasterViewModel context)
        {
            _id = id;
            _name = name;
            Context = context;
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}
