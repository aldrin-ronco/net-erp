using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Common.Helpers
{
    public static class GetCurrentMethodName
    {
        public static string Get([CallerMemberName] string name = "")
        {
            return name;
        }
    }
}
