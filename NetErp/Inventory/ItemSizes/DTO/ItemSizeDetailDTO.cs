﻿using Caliburn.Micro;
using Common.Helpers;
using DevExpress.Mvvm;
using NetErp.Helpers;
using NetErp.Inventory.ItemSizes.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Xceed.Wpf.Toolkit.Primitives;

namespace NetErp.Inventory.ItemSizes.DTO
{
    public class ItemSizeDetailDTO : Screen, ItemSizeType
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

        private int _itemSizeMasterId;
        public int ItemSizeMasterId
        {
            get { return _itemSizeMasterId; }
            set
            {
                if (_itemSizeMasterId != value)
                {
                    _itemSizeMasterId = value;
                    NotifyOfPropertyChange(nameof(ItemSizeMasterId));
                }
            }
        }

        private int _presentationOrder;
        public int PresentationOrder
        {
            get { return _presentationOrder; }
            set
            {
                if (_presentationOrder != value)
                {
                    _presentationOrder = value;
                    NotifyOfPropertyChange(nameof(PresentationOrder));
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
                }
            }
        }

        public ItemSizeDetailDTO()
        {

        }

        public ItemSizeDetailDTO(int id, string name, int sizeMasterId)
        {
            _id = id;
            _name = name;
            _itemSizeMasterId = sizeMasterId;
        }
        public override string ToString()
        {
            return $"{_name}";
        }
    }
}
