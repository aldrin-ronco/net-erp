using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Global
{
    public class CountryGraphQLModel
    {
        public int Id { get; set; } = 0;
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public List<DepartmentGraphQLModel> Departments { get; set; }
        public override string ToString()
        {
            return $"{Name}";
        }
    }

    public class CountryDTO : CountryGraphQLModel 
    {
        
    }
}
