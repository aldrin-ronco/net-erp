using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Login
{
    public class LoginTicketGraphQLModel
    {
        public DateTime ExpiresAt { get; set; }
        public string Ticket { get; set; } = string.Empty;
    }
}
