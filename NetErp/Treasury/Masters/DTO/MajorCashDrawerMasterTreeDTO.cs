using Models.Books;
using Models.Treasury;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetErp.Treasury.Masters.DTO
{
    public class MajorCashDrawerMasterTreeDTO : CashDrawerMasterTreeDTO, ITreasuryTreeMasterSelectedItem
    {
		private ObservableCollection<TreasuryAuxiliaryCashDrawerMasterTreeDTO> _auxiliaryCashDrawers = [];

		public ObservableCollection<TreasuryAuxiliaryCashDrawerMasterTreeDTO> AuxiliaryCashDrawers 
		{
			get { return _auxiliaryCashDrawers; }
			set 
			{
				if(_auxiliaryCashDrawers != value)
				{
					_auxiliaryCashDrawers = value;
					NotifyOfPropertyChange(nameof(AuxiliaryCashDrawers));
				}
			}
		}

		public bool AllowContentControlVisibility { get => true; }

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
                    if (_auxiliaryCashDrawers != null)
                    {
                        if (_isExpanded && _auxiliaryCashDrawers.Count > 0)
                        {
                            if (_auxiliaryCashDrawers[0].IsDummyChild)
                            {
                                _ = Context.LoadAuxiliaryCashDrawers(this);
                            }
                        }
                    }
                }
            }
        }

        //Propiedad dummy no usada, creada para evitar comportamiento poco probable, pero posible en el arbol
        public AccountingEntityGraphQLModel _accountingEntity = new();

        //Propiedad dummy no usada, creada para evitar comportamiento poco probable, pero posible en el arbol

        private string _description = string.Empty;

        public string Description
        {
            get { return _description = string.Empty; }
            set { _description = value; }
        }


        public AccountingEntityGraphQLModel AccountingEntity
        {
            get { return _accountingEntity; }
            set { _accountingEntity = value; }
        }
    }
}
