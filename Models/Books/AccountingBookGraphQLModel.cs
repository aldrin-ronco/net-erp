using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Models.Global.GraphQLResponseTypes;

namespace Models.Books
{
    public class AccountingBookGraphQLModel
    {
        public int Id { get; set; } = 0;
        public string Name { get; set; } = string.Empty;
        public override string ToString()
        {
            return Name;
        }
    }

    public class AccountingBookDTO: AccountingBookGraphQLModel
    {
        public int? Id { get; set; } = 0;
        public bool IsChecked { get; set; } = false;
    }

    public class AccountingBookCreateMessage
    {
        public UpsertResponseType<AccountingBookGraphQLModel>  CreatedAccountingBook { get; set; } 
    }

    public class AccountingBookUpdateMessage
    {

        public UpsertResponseType<AccountingBookGraphQLModel> UpdatedAccountingBook { get; set; } 
    }

    public class AccountingBookDeleteMessage
    {
        public DeleteResponseType DeletedAccountingBook { get; set; } = new();
    }


}
