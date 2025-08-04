using Caliburn.Micro;
using Models.Books;
using NetErp.Books.AccountingAccountGroups.ViewModels;
using NetErp.Books.WithholdingCertificateConfig.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetErp.Books.AccountingAccountGroups.DTO
{
    public class AccountingAccountGroupDTO : PropertyChangedBase
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

		private string _code = string.Empty;

		public string Code
		{
			get { return _code; }
			set 
			{
				if (_code != value) 
				{
					_code = value;
					NotifyOfPropertyChange(nameof(Code));
				}
			}
		}


		private bool _isChecked = false;

		public bool IsChecked
		{
			get { return _isChecked; }
			set 
			{
				if (_isChecked != value) 
				{
					_isChecked = value;
					NotifyOfPropertyChange(nameof(IsChecked));
					Context.NotifyOfPropertyChange(nameof(Context.CanDeleteAccountingAccount));
				}
			}
		}

        public string FullName => $"{Code.Trim()} - {Name.Trim()}";

        public AccountingAccountGroupMasterViewModel Context { get; set; }

    }
    public class AccountingAccountGroupDetailDTO :  PropertyChangedBase
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
  
     
        public int GroupId { get; set; }

        private bool? _isChecked = false;

        public bool? IsChecked
        {
            get { return _isChecked; }
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                    NotifyOfPropertyChange(nameof(IsChecked));
                    Context?.NotifyOfPropertyChange(nameof(Context.CanSave));
                }
            }
        }
        public WithholdingCertificateConfigDetailViewModel? Context { get; set; }
    }
}
