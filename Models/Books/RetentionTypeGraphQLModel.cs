using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Books
{
    public class RetentionTypeGraphQLModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal InitialBase { get; set; } = 0;
        public decimal Margin { get; set; } = 0;
        public AccountingAccountGraphQLModel AccountingAccountPurchase { get; set; }
        public AccountingAccountGraphQLModel AccountingAccountSale { get; set; }
    }

    public class RetentionTypeDTO : RetentionTypeGraphQLModel
    {
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value) _isSelected = value;
            }
        }

    }
}
