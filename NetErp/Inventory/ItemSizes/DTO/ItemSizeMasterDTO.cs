using Caliburn.Micro;
using Models.Inventory;
using NetErp.Books.AccountingAccounts.DTO;
using NetErp.Inventory.ItemSizes.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetErp.Inventory.ItemSizes.DTO
{
    public class ItemSizeMasterDTO : Screen, ItemSizeType
    {

        private ItemSizeMasterViewModel _context;

        public ItemSizeMasterViewModel Context
        {
            get { return _context; }
            set
            {
                if (_context != value)
                {
                    _context = value;
                    NotifyOfPropertyChange(nameof(Context));
                }
            }
        }

        private ObservableCollection<ItemSizeDetailDTO> _sizes;

        public ObservableCollection<ItemSizeDetailDTO> Sizes
        {
            get { return _sizes; }
            set
            {
                if (_sizes != value)
                {
                    _sizes = value;
                    NotifyOfPropertyChange(nameof(Sizes));
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

        private bool _isEditing = false;
        public bool IsEditing
        {
            get { return _isEditing; }
            set
            {
                if (_isEditing != value)
                {
                    _isEditing = value;
                    NotifyOfPropertyChange(nameof(IsEditing));
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

        public ItemSizeMasterDTO()
        {

        }

        public ItemSizeMasterDTO(int id, ItemSizeMasterViewModel context, string name, ObservableCollection<ItemSizeDetailDTO> childrens)
        {
            _id = id;
            _name = name;
            _sizes = childrens;
            _context = context;
        }
        public override string ToString()
        {
            return $"{_name}";
        }
    }
}
