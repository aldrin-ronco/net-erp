using System;
using System.Runtime.CompilerServices;

namespace Common.Helpers
{
    public class AsyncException : Exception
    {
        public string MethodOrigin { get; }
        public string TypeOrigin { get; }

        /// <summary>
        /// Full constructor with type and inner exception.
        /// Usage: throw new AsyncException(GetType(), ex);
        /// </summary>
        public AsyncException(
            Type typeOrigin,
            Exception innerException,
            [CallerMemberName] string methodOrigin = ""
        ) : base($"{typeOrigin.Name}.{methodOrigin}: {innerException?.Message}", innerException)
        {
            MethodOrigin = methodOrigin;
            TypeOrigin = typeOrigin.Name;
        }

        /// <summary>
        /// Legacy constructor for backward compatibility.
        /// Usage: throw new AsyncException(innerException: ex);
        /// </summary>
        public AsyncException(
            [CallerMemberName] string methodOrigin = "",
            Exception innerException = null
        ) : base($"(Origen: {methodOrigin}): {innerException?.Message}", innerException)
        {
            MethodOrigin = methodOrigin;
            TypeOrigin = string.Empty;
        }
    }
}
