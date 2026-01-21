using Caliburn.Micro;
using NetErp.Treasury.Masters.ViewModels;
using System.Collections.ObjectModel;

namespace NetErp.Treasury.Masters.DTO
{
    /// <summary>
    /// DTO unificado para el nodo raíz de Cajas Generales y Cajas Menores en el árbol.
    /// Reemplaza MajorCashDrawerDummyDTO y MinorCashDrawerDummyDTO.
    /// </summary>
    public class CashDrawerDummyDTO : Screen, ITreasuryTreeMasterSelectedItem
    {
        public bool AllowContentControlVisibility => false;

        public CashDrawerType Type { get; set; }

        public TreasuryRootMasterViewModel Context { get; set; }

        public int Id { get; set; }

        private string _name = string.Empty;
        public string Name
        {
            get => _name;
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
            get => _isDummyChild;
            set
            {
                if (_isDummyChild != value)
                {
                    _isDummyChild = value;
                    NotifyOfPropertyChange(nameof(IsDummyChild));
                }
            }
        }

        private ObservableCollection<CashDrawerCompanyLocationDTO> _locations = [];
        public ObservableCollection<CashDrawerCompanyLocationDTO> Locations
        {
            get => _locations;
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
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    NotifyOfPropertyChange(nameof(IsExpanded));

                    if (_isExpanded && _locations != null && _locations.Count > 0 && _locations[0].IsDummyChild)
                    {
                        _ = Context.LoadCashDrawerCompanyLocations(this);
                    }
                }
            }
        }

        public CashDrawerDummyDTO()
        {
        }

        public CashDrawerDummyDTO(int id, string name, CashDrawerType type)
        {
            Id = id;
            Name = name;
            Type = type;
        }
    }
}
