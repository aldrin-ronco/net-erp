using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Models.Global.GraphQLResponseTypes;

namespace Models.Books
{
    public class AccountingAccountGroupGraphQLModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Key {  get; set; } = string.Empty;
        public IEnumerable<AccountingAccountGroupDetailGraphQLModel> Accounts { get; set; } = [];
    }

    public class AccountingAccountGroupDetailGraphQLModel : AccountingAccountGraphQLModel
    {
        public int GroupId { get; set; }
    }

    public class AccountingAccountGroupDataContext
    {
        public PageType<AccountingAccountGroupGraphQLModel> AccountingAccountGroups { get; set; } 
        public PageType<AccountingAccountGraphQLModel> AccountingAccounts { get; set; } 
    }

    public class AccountingAccountGroupUpdateMessage
    {
        public AccountingAccountGroupGraphQLModel UpdateAccountingAccountGroup { get; set; }
    }
}
