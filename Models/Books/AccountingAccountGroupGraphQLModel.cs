using Models.Login;
using System.Collections.Generic;
using static Models.Global.GraphQLResponseTypes;

namespace Models.Books
{
    public class AccountingAccountGroupGraphQLModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Key {  get; set; } = string.Empty;
        public IEnumerable<AccountingAccountGroupDetailGraphQLModel> Accounts { get; set; } = [];
        public IEnumerable<AccountingAccountGroupFilterGraphQLModel> Filters { get; set; } = [];
    }

    public class AccountingAccountGroupDetailGraphQLModel : AccountingAccountGraphQLModel
    {
        public int GroupId { get; set; }
    }

    public class AccountingAccountGroupFilterGraphQLModel
    {
        public int Id { get; set; }
        public AccountingAccountGraphQLModel AccountingAccount { get; set; } = new();
    }

    public class FilterMutationResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public List<GlobalErrorGraphQLModel>? Errors { get; set; }
    }

    public class AttachAccountToGroupFilterResponse
    {
        public FilterMutationResult AttachAccountToAccountingAccountGroupFilter { get; set; } = new();
    }

    public class DetachAccountFromGroupFilterResponse
    {
        public FilterMutationResult DetachAccountFromAccountingAccountGroupFilter { get; set; } = new();
    }

    public class AccountingAccountGroupDataContext
    {
        public PageType<AccountingAccountGroupGraphQLModel> AccountingAccountGroups { get; set; }
        public PageType<AccountingAccountGraphQLModel> AccountingAccounts { get; set; }
    }

    public class AccountingAccountGroupUpdateMessage
    {
        public UpsertResponseType<AccountingAccountGroupGraphQLModel> UpsertAccountingAccountGroup { get; set; }
        public AccountingAccountGroupGraphQLModel UpdateAccountingAccountGroup { get; set; }
    }
}
