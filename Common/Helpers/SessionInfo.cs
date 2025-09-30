using Models.Global;
using Models.Login;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Common.Helpers
{
    public static class SessionInfo
    {
        public static string UserEmail { get; set; } = "Prueba@emaildummy.com";

        public static string ComputerName { get; set; } = string.Empty;

        public static string SessionId { get; set; } = string.Empty;
        public static CompanyGraphQLModel? CurrentCompany { get; set; }
        // Cuando aún no se ha establecido CurrentCompany, pero necesitamos enviar database-id
        public static string? PendingCompanyReference { get; set; }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetSystemMetrics(int nIndex);

        private const int SM_REMOTESESSION = 0x1000;

        public static string GetComputerName()
        {
            if(!string.IsNullOrEmpty(ComputerName)) return ComputerName;

            bool isRemoteSession = GetSystemMetrics(SM_REMOTESESSION);

            if (isRemoteSession)
            {
                ComputerName = Environment.GetEnvironmentVariable("CLIENTNAME") ?? "";
                return ComputerName;
            }
            else
            {
                ComputerName = Environment.MachineName;
                return ComputerName;
            }
        }
    }
}
