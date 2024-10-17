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
    public class CostCenterDummyDTO: Screen, ICostCentersItems
    {
        public CostCenterMasterViewModel Context { get; set; }


        public int Id
        {
            get { return 1; }
        }


        private string _name = "CENTROS DE COSTOS";

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

        private ObservableCollection<CostCenterDTO> _costCenters;

        public ObservableCollection<CostCenterDTO> CostCenters
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
                    if (_costCenters != null)
                    {
                        if (_isExpanded && _costCenters.Count > 0)
                        {
                            if (_costCenters[0].IsDummyChild)
                            {
                                _ = Context.LoadCostCenters(Location, this);
                            }
                        }
                    }
                }
            }
        }


        public CostCenterDummyDTO()
        {

        }

        public CostCenterDummyDTO(CostCenterMasterViewModel context, CompanyLocationDTO location)
        {
            Context = context;
            this._location = location;
            this._costCenters = [new() { IsDummyChild = true, Name = "Fucking Dummy" }];
        }
    }
    
}

