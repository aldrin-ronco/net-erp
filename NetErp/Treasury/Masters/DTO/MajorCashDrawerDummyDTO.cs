using Caliburn.Micro;
using NetErp.Global.CostCenters.DTO;
using NetErp.Treasury.Masters.ViewModels;
using Ninject.Activation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetErp.Treasury.Masters.DTO
{
    public class MajorCashDrawerDummyDTO: Screen, ITreasuryTreeMasterSelectedItem
    {

        public bool AllowContentControlVisibility { get => false; }
        public int Id { get; set; }

        public TreasuryRootMasterViewModel Context { get; set; }

        private string _name  = string.Empty;

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


        private ObservableCollection<TreasuryMajorCashDrawerCompanyLocationMasterTreeDTO> _locations = [];

        public ObservableCollection<TreasuryMajorCashDrawerCompanyLocationMasterTreeDTO> Locations
        {
            get { return _locations; }
            set
            {
                if (_locations != value)
                {
                    _locations = value;
                    NotifyOfPropertyChange(nameof(Locations));
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
                    if (_locations != null)
                    {
                        if (_isExpanded && _locations.Count > 0)
                        {
                            if (_locations[0].IsDummyChild)
                            {
                                _ = Context.LoadMajorCashDrawerCompanyLocations();
                            }
                        }
                    }
                }
            }
        }

        public MajorCashDrawerDummyDTO()
        {

        }

        public MajorCashDrawerDummyDTO(int id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}
