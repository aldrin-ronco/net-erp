using Models.Books;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetErp.Treasury.Masters.DTO
{
    public class TreasuryMinorCashDrawerCompanyLocationMasterTreeDTO : TreasuryCompanyLocationMasterTreeDTO, ITreasuryTreeMasterSelectedItem
    {
        public bool AllowContentControlVisibility { get => false; }
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
                    if (_costCenters != null)
                    {
                        if (_isExpanded && _costCenters.Count > 0)
                        {
                            if (_costCenters[0].IsDummyChild)
                            {
                                _ = Context.LoadMinorCashDrawerCostCenters(this);
                            }
                        }
                    }
                }
            }
        }

        private MinorCashDrawerDummyDTO _dummyParent;

        public MinorCashDrawerDummyDTO DummyParent
        {
            get { return _dummyParent; }
            set
            {
                if (_dummyParent != value)
                {
                    _dummyParent = value;
                    NotifyOfPropertyChange(nameof(DummyParent));
                }
            }
        }


        private ObservableCollection<TreasuryMinorCashDrawerCostCenterMasterTreeDTO> _costCenters = [];

        public ObservableCollection<TreasuryMinorCashDrawerCostCenterMasterTreeDTO> CostCenters
        {
            get { return _costCenters; }
            set
            {
                if (_costCenters != value)
                {
                    _costCenters = value;
                    NotifyOfPropertyChange(nameof(CostCenters));
                }
            }
        }

        //Propiedad dummy no usada, creada para evitar comportamiento poco probable, pero posible en el arbol

        private AccountingEntityGraphQLModel _accountingEntity = new();

        public AccountingEntityGraphQLModel AccountingEntity
        {
            get { return _accountingEntity; }
            set { _accountingEntity = value; }
        }

    }
}
