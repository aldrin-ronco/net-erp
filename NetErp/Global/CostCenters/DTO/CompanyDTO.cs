using Caliburn.Micro;
using DTOLibrary.Books;
using Models.Global;
using NetErp.Books.AccountingAccounts.DTO;
using NetErp.Global.CostCenters.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetErp.Global.CostCenters.DTO
{
    public class CompanyDTO: Screen, ICostCentersItems
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

		private AccountingEntityDTO _accountingEntityCompany;

		public AccountingEntityDTO AccountingEntityCompany
		{
			get { return _accountingEntityCompany; }
			set 
			{
				if (_accountingEntityCompany != value) 
				{
					_accountingEntityCompany = value;
					NotifyOfPropertyChange(nameof(AccountingEntityCompany));
				}
			}
		}

		private ObservableCollection<CompanyLocationDTO> _locations = [];

		public ObservableCollection<CompanyLocationDTO> Locations
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
					if(_locations != null)
					{
						if(_isExpanded && _locations.Count > 0)
						{
							if (_locations[0].IsDummyChild)
							{
								_ = Context.LoadCompaniesLocations(this);
                            }
						}
					}
                }
			}
		}

        private bool _isSelected;
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    NotifyOfPropertyChange(nameof(IsSelected));
                }
            }
        }


        public CompanyDTO()
        {

        }

        public CompanyDTO(int id, AccountingEntityDTO accountingEntityCompany, ObservableCollection<CompanyLocationDTO> locations, CostCenterMasterViewModel context)
        {
            this._id = id;
            this._accountingEntityCompany = accountingEntityCompany;
            this._locations = locations;
			this.Context = context;
        }
        public override string ToString()
        {
            return $"{_accountingEntityCompany.FullName}";
        }
    }
}
