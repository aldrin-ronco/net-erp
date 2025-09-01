using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Login
{
    public class LoginLicenseGraphQLModel
    {
        public int Id { get; set; }
        public LoginOrganizationGraphQLModel Organization { get; set; } = new();
    }
}
