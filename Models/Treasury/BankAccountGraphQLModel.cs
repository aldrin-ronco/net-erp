using Models.Billing;
using Models.Books;
using Models.Global;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Treasury
{
    public class BankAccountGraphQLModel
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Number { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public AccountingAccountGraphQLModel AccountingAccount { get; set; } = new();
        public BankGraphQLModel Bank { get; set; } = new();
        public string Provider {  get; set; } = string.Empty;
        public PaymentMethodGraphQLModel PaymentMethod { get; set; } = new();
        public IEnumerable<CostCenterGraphQLModel> AllowedCostCenters { get; set; }
    }

    public class BankAccountCreateMessage
    {
        public BankAccountGraphQLModel CreatedBankAccount { get; set; } = new();
    }

    public class BankAccountUpdateMessage
    {
        public BankAccountGraphQLModel UpdatedBankAccount { get; set; } = new();
    }

    public class BankAccountDeleteMessage
    {
        public BankAccountGraphQLModel DeletedBankAccount { get; set; } = new();
    }
}
