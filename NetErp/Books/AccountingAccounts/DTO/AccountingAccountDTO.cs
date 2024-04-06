using DevExpress.Mvvm;
using NetErp.Books.AccountingAccounts.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetErp.Books.AccountingAccounts.DTO
{
    public class AccountingAccountDTO : BindableBase
    {

        private AccountPlanMasterViewModel _context;
        public AccountPlanMasterViewModel Context
        {
            get { return _context; }
            set { SetValue(ref _context, value); }
        }

        private bool _isDummyChild = false;
        public bool IsDummyChild
        {
            get { return _isDummyChild; }
            set { SetValue(ref _isDummyChild, value); }
        }

        public bool IsAuxiliary
        {
            get { return (this._code.Trim().Length >= 8); }
        }

        private bool _isExpanded;
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                SetValue(ref _isExpanded, value, changedCallback: OnIsExpandedChanged);
            }
        }

        void OnIsExpandedChanged()
        {
            if (_childrens != null)
            {
                if (_isExpanded && _childrens.Count > 0)
                {
                    if (_childrens[0].IsDummyChild)
                        _context.LoadChildren(this, _context.accounts);
                }
            }
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get { return _isSelected; }
            set { SetValue(ref _isSelected, value); }
        }

        private int _id;
        public int Id
        {
            get { return _id; }
            set
            {
                SetValue(ref _id, value);
            }
        }

        private string _code = string.Empty;
        public string Code
        {
            get { return _code; }
            set
            {
                SetValue(ref _code, value);
            }
        }

        private string _name;
        public string Name
        {
            get { return _name; }
            set
            {
                SetValue(ref _name, value);
            }
        }

        private ObservableCollection<AccountingAccountDTO> _childrens;
        public ObservableCollection<AccountingAccountDTO> Childrens
        {
            get { return _childrens; }
            set
            {
                SetValue(ref _childrens, value);
            }
        }

        public AccountingAccountDTO()
        {

        }

        public AccountingAccountDTO(int id, string code, string name, ObservableCollection<AccountingAccountDTO> childrens)
        {
            this._id = id;
            this._code = code;
            this._name = name;
            this._childrens = childrens;
        }

        public override string ToString()
        {
            return $"{this._code} - {this._name}";
        }

    }
}
