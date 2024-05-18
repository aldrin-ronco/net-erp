﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Books
{
    public class AccountingPresentationGraphQLModel
    {
        public int Id { get; set; } = 0;
        public string Name { get; set; } = string.Empty;
        public bool AllowsAccountingClosure { get; set; } = false;
        public AccountingBookGraphQLModel AccountingBookClosure { get; set; }
        public ObservableCollection<AccountingBookGraphQLModel> AccountingBooks { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
