using Models.Global;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
}
