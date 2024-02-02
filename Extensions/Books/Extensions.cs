using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Models.Books;

namespace Extensions.Books
{
    public static class Extensions
    {
        public static void Replace(this List<AccountingAccountGraphQLModel> accounts, AccountingAccountGraphQLModel updatedAccount)
        {
            AccountingAccountGraphQLModel? accountToReplace = accounts.FirstOrDefault(accountingAccount => accountingAccount.Id == updatedAccount.Id);
            if (accountToReplace != null)
            {
                int index = accounts.IndexOf(accountToReplace);
                accounts.Remove(accountToReplace);
                accounts.Insert(index, updatedAccount);
            }
        }
    }
}
