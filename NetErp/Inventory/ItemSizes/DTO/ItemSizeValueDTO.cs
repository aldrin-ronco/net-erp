using Caliburn.Micro;

namespace NetErp.Inventory.ItemSizes.DTO
{
    public class ItemSizeValueDTO : PropertyChangedBase, ItemSizeType
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

        private int _itemSizeCategoryId;
        public int ItemSizeCategoryId
        {
            get { return _itemSizeCategoryId; }
            set
            {
                if (_itemSizeCategoryId != value)
                {
                    _itemSizeCategoryId = value;
                    NotifyOfPropertyChange(nameof(ItemSizeCategoryId));
                }
            }
        }

        private int _displayOrder;
        public int DisplayOrder
        {
            get { return _displayOrder; }
            set
            {
                if (_displayOrder != value)
                {
                    _displayOrder = value;
                    NotifyOfPropertyChange(nameof(DisplayOrder));
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

        public ItemSizeValueDTO()
        {

        }

        public ItemSizeValueDTO(int id, string name, int sizeCategoryId)
        {
            _id = id;
            _name = name;
            _itemSizeCategoryId = sizeCategoryId;
        }
        public override string ToString()
        {
            return $"{_name}";
        }
    }
}
