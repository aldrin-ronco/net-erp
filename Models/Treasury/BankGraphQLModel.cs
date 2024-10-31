using Models.Books;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Models.Treasury
{
    public class BankGraphQLModel
    {
        public int Id { get; set; }
        public AccountingEntityGraphQLModel AccountingEntity { get; set; } = new();
        public string PaymentMethodPrefix { get; set; } = string.Empty;
        public override string ToString()
        {
            return AccountingEntity.SearchName;
        }
    }
}
