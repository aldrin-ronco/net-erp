using Caliburn.Micro;
using System.Collections.ObjectModel;

namespace NetErp.Inventory.CatalogItems.DTO
{
    public class CatalogDTO : PropertyChangedBase, ICatalogItem
    {
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

        private ObservableCollection<ItemTypeDTO> _itemTypes = [];
        public ObservableCollection<ItemTypeDTO> ItemTypes
        {
            get { return _itemTypes; }
            set
            {
                if (_itemTypes != value)
                {
                    _itemTypes = value;
                    NotifyOfPropertyChange(nameof(ItemTypes));
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
    }
}
