using Models.Books;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Global
{
    public class CompanyGraphQLModel
    {
        public int Id { get; set; }
        public AccountingEntityGraphQLModel AccountingEntityCompany { get; set; } = new();
        public IEnumerable<CompanyLocationGraphQLModel> Locations { get; set; } = [];
    }

    public class CompanyCreateMessage
    {
        public CompanyGraphQLModel CreatedCompany { get; set; }
    }

    public class CompanyUpdateMessage 
    {
        public CompanyGraphQLModel UpdatedCompany { get; set; }
    }

    public class CompanyDeleteMessage 
    {
        public CompanyGraphQLModel DeletedCompany { get; set; }
    }
}
