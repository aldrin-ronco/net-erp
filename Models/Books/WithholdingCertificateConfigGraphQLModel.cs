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
    public class WithholdingCertificateConfigGraphQLModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public CostCenterGraphQLModel CostCenter { get; set; }
        public List<AccountingAccountGraphQLModel> AccountingAccounts { get; set; } 

    }

    public class WithholdingCertificateConfigDataContext
    {
        public PageType<CostCenterGraphQLModel> CostCenters { get; set; } 
        public PageType<AccountingAccountGroupGraphQLModel> AccountingAccountGroups { get; set; } 
    }   

    public class WithholdingCertificateConfigCreateMessage
    {
        public UpsertResponseType<WithholdingCertificateConfigGraphQLModel> CreatedWithholdingCertificateConfig { get; set; }
    }
    public class WithholdingCertificateConfigDeleteMessage
    {
        public DeleteResponseType DeletedWithholdingCertificateConfig { get; set; }
    }

    public class WithholdingCertificateConfigUpdateMessage
    {
        public UpsertResponseType<WithholdingCertificateConfigGraphQLModel> UpdatedWithholdingCertificateConfig { get; set; }
    }
}
