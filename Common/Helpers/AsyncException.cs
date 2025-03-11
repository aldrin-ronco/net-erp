using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Common.Helpers
{
    public class AsyncException : Exception
    {
        public string MethodOrigin { get; }
        public AsyncException(
            [CallerMemberName] string methodOrigin = "",
            Exception innerException = null
        ) : base($"(Origen: {methodOrigin})", innerException)
        {
            MethodOrigin = methodOrigin;
        }
    }
}
