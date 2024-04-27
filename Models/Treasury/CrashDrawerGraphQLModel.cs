﻿using Models.Books;
using Models.Global;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Treasury
{
    public class CashDrawerGraphQLModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool CashReviewRequired { get; set; }
        public bool AutoAdjustBalance { get; set; }
        public bool AutoTransfer { get; set; }
        public CashDrawerGraphQLModel CashDrawerAutoTransfer { get; set; }
        public CostCenterGraphQLModel CostCenter { get; set; }
        public AccountingAccountGraphQLModel AccountingAccountCash { get; set; }
        public AccountingAccountGraphQLModel AccountingAccountCheck { get; set; }
        public AccountingAccountGraphQLModel AccountingAccountCard { get; set; }
        public override string ToString()
        {
            return Name;
        }
    }

    public class TreasuryCashDrawerDTO : CashDrawerGraphQLModel
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

    public class TreasuryCashDrawerCreateMessage
    {
        public CashDrawerGraphQLModel CreatedCashDrawer { get; set; }
    }

    public class TreasuryCashDrawerUpdateMessage
    {
        public CashDrawerGraphQLModel UpdatedCashDrawer { get; set; }
    }

    public class TreasuryCashDrawerDeleteMessage
    {
        public CashDrawerGraphQLModel DeletedCashDrawer { get; set; }
    }
}
