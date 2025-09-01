using Models.Global;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using static Models.Global.GraphQLResponseTypes;

namespace Models.Books
{
    public class AccountingSourceGraphQLModel
    {
        public int Id { get; set; } = 0;
        public string Code { get; set; } = string.Empty;
        public string AnnulmentCode { get; set; } = string.Empty;
        public string FullCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public char AnnulmentCharacter { get; set; } = 'A';
        public bool IsSystemSource { get; set; } = false;
        public bool IsKardexTransaction { get; set; } = false;
        public char KardexFlow { get; set; } = 'N';
        public AccountingAccountGraphQLModel AccountingAccount { get; set; }
        public ProcessTypeGraphQLModel ProcessType { get; set; }
        public override string ToString()
        {
            return this.Name;
        }
    }

    public class AccountingSourceDTO : AccountingSourceGraphQLModel
    {
        private bool _isChecked;
        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                }
            }
        }
    }

    public class AccountingSourceCreateMessage
    {
        public AccountingSourceGraphQLModel CreatedAccountingSource { get; set; } = new();
    }

    public class AccountingSourceUpdateMessage
    {
        public AccountingSourceGraphQLModel UpdatedAccountingSource { get; set; } = new();
    }

    public class AccountingSourceDeleteMessage
    {
        public AccountingSourceGraphQLModel DeletedAccountingSource { get; set; } = new();
    }

    public class AccountingSourceDataContext
    {
        public ObservableCollection<ModuleGraphQLModel> Modules { get; set; }
        public ObservableCollection<AccountingAccountGraphQLModel> AccountingAccounts { get; set; }

        public ObservableCollection<ProcessTypeGraphQLModel> ProcessTypes {  get; set; }
        public PageType<AccountingSourceGraphQLModel> AccountingSourcePage {  get; set; } 
    }

}
