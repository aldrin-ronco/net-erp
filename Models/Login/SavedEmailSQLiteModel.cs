using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Login
{
    public class SavedEmailSQLiteModel
    {
        public string Email { get; set; } = string.Empty;
        public DateTime LastUsed { get; set; }
        public int UseCount { get; set; }
    }
}
