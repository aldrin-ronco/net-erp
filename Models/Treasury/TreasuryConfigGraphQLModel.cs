using Models.Books;
using Models.Global;

namespace Models.Treasury
{
    public class TreasuryConfigGraphQLModel
    {
        public int Id { get; set; }
        public AccountingAccountGraphQLModel CardGroupAccountingAccount { get; set; } = new();
        public AccountingAccountGraphQLModel CashGroupAccountingAccount { get; set; } = new();
        public AccountingAccountGraphQLModel CheckGroupAccountingAccount { get; set; } = new();
        public AccountingAccountGraphQLModel CheckingGroupAccountingAccount { get; set; } = new();
        public AccountingAccountGraphQLModel SavingsGroupAccountingAccount { get; set; } = new();
        public CompanyGraphQLModel Company { get; set; } = new();
        public SystemAccountGraphQLModel CreatedBy { get; set; } = new();
        public DateTime InsertedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
