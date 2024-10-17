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
        public AccountingEntityGraphQLModel AccountingEntityCompany { get; set; }
        public IEnumerable<CompanyLocationGraphQLModel> Locations { get; set; }
    }
}
