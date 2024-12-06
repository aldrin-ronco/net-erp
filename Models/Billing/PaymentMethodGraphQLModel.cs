using Models.Books;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Billing
{
    public class PaymentMethodGraphQLModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Abbreviation { get; set; } = string.Empty;
        public bool RequiresDocumentNumber { get; set; }
        public bool IsActive { get; set; }
        public int DisplayOrder { get; set; }
        public AccountingAccountGraphQLModel AccountingAccount { get; set; } = new();
    }
}
