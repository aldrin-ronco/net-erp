using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Global
{
    public class StorageGraphQLModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public CityGraphQLModel City { get; set; }
        public string Address { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public CompanyLocationGraphQLModel Location { get; set; }
    }
}
