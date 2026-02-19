using Models.Books;
using Models.Global;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using static Models.Global.GraphQLResponseTypes;

namespace Models.Treasury
{
    public class FranchiseGraphQLModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public decimal CommissionRate { get; set; }
        public decimal ReteivaRate { get; set; }
        public decimal ReteicaRate { get; set; }
        public decimal RetefteRate { get; set; }
        public decimal TaxRate { get; set; }
        public AccountingAccountGraphQLModel CommissionAccountingAccount { get; set; } = new();
        public BankAccountGraphQLModel BankAccount { get; set; } = new();
        public string FormulaCommission { get; set; } = string.Empty;
        public string FormulaReteiva { get; set; } = string.Empty;
        public string FormulaReteica { get; set; } = string.Empty;
        public string FormulaRetefte { get; set; } = string.Empty;
        public IEnumerable<FranchiseByCostCenterGraphQLModel> FranchisesByCostCenter { get; set; } = [];
        public DateTime InsertedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }

    public class FranchiseByCostCenterGraphQLModel
    {
        public int Id { get; set; }
        public CostCenterGraphQLModel CostCenter { get; set; } = new();
        public decimal CommissionRate { get; set; }
        public decimal ReteivaRate { get; set; }
        public decimal ReteicaRate { get; set; }
        public decimal RetefteRate { get; set; }
        public decimal TaxRate { get; set; }
        public BankAccountGraphQLModel BankAccount { get; set; } = new();
        public AccountingAccountGraphQLModel CommissionAccountingAccount { get; set; } = new();
        public string FormulaCommission { get; set; } = string.Empty;
        public string FormulaReteiva { get; set; } = string.Empty;
        public string FormulaReteica { get; set; } = string.Empty;
        public string FormulaRetefte { get; set; } = string.Empty;
        public FranchiseGraphQLModel Franchise { get; set; } = new();
    }

    public class FranchiseCreateMessage
    {
        public UpsertResponseType<FranchiseGraphQLModel> CreatedFranchise { get; set; } = new();
    }

    public class FranchiseUpdateMessage
    {
        public UpsertResponseType<FranchiseGraphQLModel> UpdatedFranchise { get; set; } = new();
    }

    public class FranchiseDeleteMessage
    {
        public DeleteResponseType DeletedFranchise { get; set; } = new();
    }
}
