using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Login
{
    public class GlobalErrorGraphQLModel
    {
        public List<string> Fields { get; set; } = [];
        public string Message { get; set; } = string.Empty;
    }
}
