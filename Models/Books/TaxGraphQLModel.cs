using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Books
{
    public class TaxGraphQLModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Margin { get; set; }
        public AccountingAccountGraphQLModel GeneratedTaxAccount { get; set; } = new();
        public AccountingAccountGraphQLModel GeneratedTaxRefundAccount { get; set; } = new();
        public AccountingAccountGraphQLModel DeductibleTaxAccount { get; set; } = new();
        public AccountingAccountGraphQLModel DeductibleTaxRefundAccount { get; set; } = new();
        public TaxTypeGraphQLModel TaxType { get; set; } = new();
        public bool IsActive { get; set; }
        public string Formula { get; set; } = string.Empty;
        public string AlternativeFormula { get; set; } = string.Empty;
    }
}
