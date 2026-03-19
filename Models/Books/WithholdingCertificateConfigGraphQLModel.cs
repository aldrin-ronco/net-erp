using Models.Global;
using System.Collections.Generic;
using static Models.Global.GraphQLResponseTypes;

namespace Models.Books
{
    public class WithholdingCertificateConfigGraphQLModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public CostCenterGraphQLModel? CostCenter { get; set; }
        public AccountingAccountGroupGraphQLModel? AccountingAccountGroup { get; set; }
        public List<AccountingAccountGraphQLModel> AccountingAccounts { get; set; } = []; 

    }

    public class WithholdingCertificateConfigCreateMessage
    {
        public required UpsertResponseType<WithholdingCertificateConfigGraphQLModel> CreatedWithholdingCertificateConfig { get; set; }
    }
    public class WithholdingCertificateConfigDeleteMessage
    {
        public required DeleteResponseType DeletedWithholdingCertificateConfig { get; set; }
    }

    public class WithholdingCertificateConfigUpdateMessage
    {
        public required UpsertResponseType<WithholdingCertificateConfigGraphQLModel> UpdatedWithholdingCertificateConfig { get; set; }
    }
}
