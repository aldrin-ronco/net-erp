using Models.Books;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace Models.Treasury
{
    public class FranchiseGraphQLModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public float CommissionMargin { get; set; }
        public float ReteivaMargin { get; set; }
        public float ReteicaMargin { get; set; }
        public float RetefteMargin { get; set; }
        public float IvaMargin { get; set; }
        public AccountingAccountGraphQLModel AccountingAccountCommission { get; set; } = new();
        public BankAccountGraphQLModel BankAccount { get; set; } = new();
        public string FormulaCommission { get; set; } = string.Empty;
        public string FormulaReteiva { get; set; } = string.Empty;
        public string FormulaReteica { get; set; } = string.Empty;
        public string FormulaRetefte { get; set; } = string.Empty;
    }
}
