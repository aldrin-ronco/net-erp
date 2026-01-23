using Models.Books;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static Models.Global.GraphQLResponseTypes;

namespace Models.Treasury
{
    public class BankGraphQLModel
    {
        public int Id { get; set; }
        public AccountingEntityGraphQLModel AccountingEntity { get; set; } = new();
        public string PaymentMethodPrefix { get; set; } = string.Empty;
        public DateTime InsertedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public override string ToString()
        {
            return AccountingEntity.SearchName;
        }
    }

    public class BankCreateMessage
    {
        public BankGraphQLModel CreatedBank { get; set; } = new();
    }

    public class BankUpdateMessage
    {
        public BankGraphQLModel UpdatedBank { get; set; } = new();
    }

    public class BankDeleteMessage
    {
        public DeleteResponseType DeletedBank { get; set; } = new();
    }
}
