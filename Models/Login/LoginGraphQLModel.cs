using Models.Global;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Login
{
    public class LoginGraphQLModel
    {
        public LoginAccountGraphQLModel? Account { get; set; }
        public List<LoginCompanyGraphQLModel> Companies { get; set; } = [];
        public List<GlobalErrorGraphQLModel> Errors { get; set; } = [];
        public LoginTicketGraphQLModel AccessTicket { get; set; } = new();
        public string Message { get; set; } = string.Empty;
        public bool Success { get; set; } = false;
    }
}
