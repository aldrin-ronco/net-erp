using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Login
{
    public class LoginValidateTicketGraphQLModel
    {
        public LoginAccountGraphQLModel Account { get; set; } = new();
        public List<LoginErrorGraphQLModel> Errors { get; set; } = [];
        public string Message { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
        public bool Success { get; set; }
    }
}
