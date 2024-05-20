using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Global
{
    public class ProcessTypeGraphQLModel
    {
        public int Id { get; set; } = 0;
        public string Name { get; set; } = string.Empty;
        public ModuleGraphQLModel Module { get; set; }
        public override string ToString()
        {
            return this.Name;
        }
    }
}
