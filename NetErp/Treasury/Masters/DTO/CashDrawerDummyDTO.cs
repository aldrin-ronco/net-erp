using Caliburn.Micro;
using NetErp.Treasury.Masters.ViewModels;
using System.Collections.ObjectModel;

namespace NetErp.Treasury.Masters.DTO
{
    /// <summary>
    /// DTO unificado para el nodo raíz de Cajas Generales y Cajas Menores en el árbol.
    /// </summary>
    public class CashDrawerDummyDTO : PropertyChangedBase, ITreasuryTreeMasterSelectedItem
    {
        public int Id { get; set; }
        public CashDrawerType Type { get; set; }
        public TreasuryRootMasterViewModel? Context { get; set; }

        public string Name
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Name));
                }
            }
        } = string.Empty;

        public ObservableCollection<CashDrawerCompanyLocationDTO> Locations
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Locations));
                }
            }
        } = [];

        public bool IsExpanded
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IsExpanded));
                }
            }
        }

        public bool AllowContentControlVisibility => false;
    }
}
