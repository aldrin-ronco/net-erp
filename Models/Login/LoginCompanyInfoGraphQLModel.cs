using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Login
{
    public class LoginCompanyInfoGraphQLModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public LoginLicenseGraphQLModel License { get; set; } = new();
        public string Reference { get; set; } = string.Empty;
    }
}
