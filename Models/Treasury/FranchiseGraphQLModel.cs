using Models.Books;
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
        public decimal CommissionMargin { get; set; }
        public decimal ReteivaMargin { get; set; }
        public decimal ReteicaMargin { get; set; }
        public decimal RetefteMargin { get; set; }
        public decimal IvaMargin { get; set; }
        public AccountingAccountGraphQLModel AccountingAccountCommission { get; set; } = new();
        public BankAccountGraphQLModel BankAccount { get; set; } = new();
        public string FormulaCommission { get; set; } = string.Empty;
        public string FormulaReteiva { get; set; } = string.Empty;
        public string FormulaReteica { get; set; } = string.Empty;
        public string FormulaRetefte { get; set; } = string.Empty;
        public IEnumerable<FranchiseByCostCenterGraphQLModel> FranchiseSettingsByCostCenter { get; set; } = [];
        public DateTime InsertedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }

    public class FranchiseByCostCenterGraphQLModel
    {
        public int Id { get; set; }
        public int CostCenterId { get; set; }
        public decimal CommissionMargin { get; set; }
        public decimal ReteivaMargin { get; set; }
        public decimal ReteicaMargin { get; set; }
        public decimal RetefteMargin { get; set; }
        public decimal IvaMargin { get; set; }
        public int BankAccountId { get; set; }
        public string FormulaCommission { get; set; } = string.Empty;
        public string FormulaReteiva { get; set; } = string.Empty;
        public string FormulaReteica { get; set; } = string.Empty;
        public string FormulaRetefte { get; set; } = string.Empty;
        public int FranchiseId { get; set; }
    }

    public class FranchiseCreateMessage
    {
        public FranchiseGraphQLModel CreatedFranchise { get; set; } = new();
    }

    public class FranchiseUpdateMessage
    {
        public FranchiseGraphQLModel UpdatedFranchise { get; set; } = new();
    }

    public class FranchiseDeleteMessage
    {
        public DeleteResponseType DeletedFranchise { get; set; } = new();
    }
}
