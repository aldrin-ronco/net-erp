using Models.Books;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Treasury
{
    public class BankAccountGraphQLModel
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Number { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public AccountingAccountGraphQLModel AccountingAccount { get; set; } = new();
        public BankGraphQLModel Bank { get; set; } = new();
    }
}
