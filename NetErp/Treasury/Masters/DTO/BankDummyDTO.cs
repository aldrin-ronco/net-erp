using Caliburn.Micro;
using NetErp.Treasury.Masters.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetErp.Treasury.Masters.DTO
{
    public class BankDummyDTO: Screen, ITreasuryTreeMasterSelectedItem
    {
        public int Id { get; set; }
        private string _name = string.Empty;

        public TreasuryRootMasterViewModel Context { get; set; }

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


        private ObservableCollection<TreasuryBankMasterTreeDTO> _banks = [];

        public ObservableCollection<TreasuryBankMasterTreeDTO> Banks
        {
            get { return _banks; }
            set
            {
                if (_banks != value)
                {
                    _banks = value;
                    NotifyOfPropertyChange(nameof(Banks));
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
                    if (_banks != null)
                    {
                        if (_isExpanded && _banks.Count > 0)
                        {
                            if (_banks[0].IsDummyChild)
                            {
                                _ = Context.LoadBanks();
                            }
                        }
                    }
                }
            }
        }

        public bool AllowContentControlVisibility { get => false; }
    }
}
