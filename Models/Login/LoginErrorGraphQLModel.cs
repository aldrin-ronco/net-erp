using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Login
{
    public class GlobalErrorGraphQLModel
    {
        string Code { get; set; } = string.Empty;
        public List<string> Fields { get; set; } = [];
        public string Message { get; set; } = string.Empty;
    }
}
