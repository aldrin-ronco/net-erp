using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Books
{
    public class AccountingAccountGraphQLModel
    {
        public int Id { get; set; } = 0;
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public char Nature { get; set; }
        public decimal Margin { get; set; } = 0;
        public int MarginBasis { get; set; } = 0;
        public string FullName => $"{Code.Trim()} - {Name.Trim()}";
        public override string ToString() => $"{Code} - {Name}";
    }

    public class AccountingAccountCreateListMessage
    {
        public List<AccountingAccountGraphQLModel> CreatedAccountingAccountList { get; set; }
    }

    public class AccountingAccountCreateMessage
    {
        public AccountingAccountGraphQLModel CreatedAccountingAccount { get; set; }
    }

    public class AccountingAccountUpdateMessage
    {
        public AccountingAccountGraphQLModel UpdatedAccountingAccount { get; set; }

    }

    public class AccountingAccountDeleteMessage
    {
        public AccountingAccountGraphQLModel DeletedAccountingAccount { get; set; }
    }

    public class CanDeleteAccountingAccount
    {
        public bool CanDelete { get; set; } = false;
        public string Message { get; set; } = string.Empty;
    }
}
