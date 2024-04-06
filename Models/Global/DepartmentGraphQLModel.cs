using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Global
{
    public class DepartmentGraphQLModel
    {
        public int Id { get; set; } = 0;
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public int CountryId { get; set; } = 0;
        public List<CityGraphQLModel> Cities { get; set; }
        public override string ToString()
        {
            return $"{this.Code} - {this.Name}";
        }
    }
}
