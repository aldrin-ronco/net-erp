using Caliburn.Micro;
using NetErp.Treasury.Masters.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetErp.Treasury.Masters.DTO
{
    public class TreasuryCostCenterMasterTreeDTO : Screen
    {
        public int Id { get; set; }

        public TreasuryRootMasterViewModel Context { get; set; }

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

        public TreasuryCostCenterMasterTreeDTO() { }

        public TreasuryCostCenterMasterTreeDTO(int id, string name, TreasuryRootMasterViewModel context)
        {
            Id = id;
            Name = name;
            Context = context;
        }
    }
}
