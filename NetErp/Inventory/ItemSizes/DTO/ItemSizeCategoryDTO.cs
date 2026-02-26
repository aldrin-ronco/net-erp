using Caliburn.Micro;
using System.Collections.ObjectModel;

namespace NetErp.Inventory.ItemSizes.DTO
{
    public class ItemSizeCategoryDTO : PropertyChangedBase, ItemSizeType
    {
        private ObservableCollection<ItemSizeValueDTO> _itemSizeValues;

        public ObservableCollection<ItemSizeValueDTO> ItemSizeValues
        {
            get { return _itemSizeValues; }
            set
            {
                if (_itemSizeValues != value)
                {
                    _itemSizeValues = value;
                    NotifyOfPropertyChange(nameof(ItemSizeValues));
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

        public override string ToString()
        {
            return $"{_name}";
        }
    }
}
