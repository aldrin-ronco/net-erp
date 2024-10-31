using Caliburn.Micro;
using NetErp.Books.AccountingAccounts.DTO;
using NetErp.Treasury.Masters.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetErp.Treasury.Masters.DTO
{
    public class FranchiseDummyDTO : Screen, ITreasuryTreeMasterSelectedItem
    {
        public int Id { get; set; }

        public TreasuryRootMasterViewModel Context { get; set; }

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


        private ObservableCollection<TreasuryFranchiseMasterTreeDTO> _franchises = [];

        public ObservableCollection<TreasuryFranchiseMasterTreeDTO> Franchises
        {
            get { return _franchises; }
            set
            {
                if (_franchises != value)
                {
                    _franchises = value;
                    NotifyOfPropertyChange(nameof(Franchises));
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
                    if (_franchises != null)
                    {
                        if (_isExpanded && _franchises.Count > 0)
                        {
                            if (_franchises[0].IsDummyChild)
                            {
                                _ = Context.LoadFranchises(this);
                            }
                        }
                    }
                }
            }
        }

        public bool AllowContentControlVisibility { get => false; }
    }
}
