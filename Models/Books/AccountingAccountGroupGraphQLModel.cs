using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Books
{
    public class AccountingAccountGroupGraphQLModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Key {  get; set; } = string.Empty;
        public IEnumerable<AccountingAccountGroupDetailGraphQLModel> AccountingAccounts { get; set; } = [];
    }

    public class AccountingAccountGroupDetailGraphQLModel : AccountingAccountGraphQLModel
    {
        public int GroupId { get; set; }
        public bool? IsChecked { get; set; }
    }

    public class AccountingAccountGroupDataContext
    {
        public IEnumerable<AccountingAccountGroupGraphQLModel> AccountingAccountGroups { get; set; } = [];
        public IEnumerable<AccountingAccountGraphQLModel> AccountingAccounts { get; set; } = [];
    }

    public class AccountingAccountGroupUpdateMessage
    {
        public AccountingAccountGroupGraphQLModel UpdateAccountingAccountGroup { get; set; }
    }
}
