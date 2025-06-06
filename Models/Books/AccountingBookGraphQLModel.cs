﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Books
{
    public class AccountingBookGraphQLModel
    {
        public int Id { get; set; } = 0;
        public string Name { get; set; } = string.Empty;
        public bool IsChecked { get; set; } 
        public override string ToString()
        {
            return Name;
        }
    }

    public class AccountingBookCreateMessage
    {
        public AccountingBookGraphQLModel CreatedAccountingBook { get; set; } = new AccountingBookGraphQLModel();
    }

    public class AccountingBookUpdateMessage
    {
        public AccountingBookGraphQLModel UpdatedAccountingBook { get; set; } = new AccountingBookGraphQLModel();
    }

    public class AccountingBookDeleteMessage
    {
        public AccountingBookGraphQLModel DeletedAccountingBook { get; set; } = new AccountingBookGraphQLModel();
    }


}
