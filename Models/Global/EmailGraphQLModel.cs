using Models.Books;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Global
{
    public class EmailGraphQLModel
    {
        public string Id { set; get; }
        public string Name { set; get; } = string.Empty;
        public string Email { set; get; } = string.Empty;
        public string Pwd { set; get; } = string.Empty;
        public bool IsCorporate { set; get; } = true;
        public bool SendElectronicInvoice { set; get; } = false;
        public AccountingEntityGraphQLModel AccountingEntity { get; set; }
        public SmtpGraphQLModel Smtp { get; set; }
        public AwsSesGraphQLModel AwsSes { get; set; }
    }
}
