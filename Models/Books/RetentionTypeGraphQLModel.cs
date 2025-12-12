using Models.Global;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Books
{
    public class WithholdingTypeGraphQLModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal BaseAmountFrom { get; set; }
        public int BaseCalculationType { get; set; }
        public CompanyGraphQLModel Company { get; set; } = new();
        public AccountingAccountGraphQLModel PurchaseAccountingAccount { get; set; } = new();
        public AccountingAccountGraphQLModel SaleAccountingAccount { get; set; } = new();
        public AccountingAccountGraphQLModel SelfWithholdingSaleAccountingAccount { get; set; } = new();
        public string WithholdingGroup { get; set; } = string.Empty;
        public decimal WithholdingRate { get; set; }
        public DateTime InsertedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }

    public class WithholdingTypeDTO : WithholdingTypeGraphQLModel
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
