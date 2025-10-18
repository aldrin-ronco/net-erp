using Models.Global;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Models.Global.GraphQLResponseTypes;

namespace Models.Books
{
    public class AccountingAccountGraphQLModel
    {
        public int Id { get; set; } = 0;
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Nature { get; set; } = string.Empty;
        public decimal Margin { get; set; } = 0;
        public int MarginBasis { get; set; } = 0;
        public DateTime InsertedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public CompanyGraphQLModel Company { get; set; } = new();
        public string FullName => $"{Code.Trim()} - {Name.Trim()}";
        public override string ToString() => $"{Code} - {Name}";
    }

    public class AccountingAccountCreateListMessage
    {
        public UpsertResponseType<List<AccountingAccountGraphQLModel>> UpsertList { get; set; }
        //TODO propiedad obsoleta, eliminar despues de migrar
        public List<AccountingAccountGraphQLModel> CreatedAccountingAccountList { get; set; }
    }

    public class AccountingAccountCreateMessage
    {
        public AccountingAccountGraphQLModel CreatedAccountingAccount { get; set; }
    }

    public class AccountingAccountUpdateMessage
    {
        public UpsertResponseType<AccountingAccountGraphQLModel> UpsertAccount { get; set; }
        //TODO propiedad obsoleta, eliminar despues de migrar
        public AccountingAccountGraphQLModel UpdatedAccountingAccount { get; set; }

    }
    public class AccountingAccountUpdateMasterListMessage
    {

        public List<AccountingAccountGraphQLModel> AccountingAccounts { get; set; }

    }
    public class CostCenterUpdateMasterListMessage
    {
        public List<CostCenterGraphQLModel> CostCenters { get; set; }

    }
    public class AccountingSourceUpdateMasterListMessage
    {
        public List<AccountingSourceGraphQLModel> AccountingSources { get; set; }

    }
    public class AccountingBookUpdateMasterListMessage
    {
        public List<AccountingBookGraphQLModel> AccountingBooks { get; set; }

    }
    
    public class AccountingAccountDeleteMessage
    {
        public DeleteResponseType DeletedResponseType { get; set; }
        public AccountingAccountGraphQLModel DeletedAccountingAccount { get; set; }
    }
}
