using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Login
{
    public class LoginCompanyGraphQLModel
    {
        public LoginCompanyInfoGraphQLModel Company { get; set; } = new();
        public string Role { get; set; } = string.Empty;
    }
}
