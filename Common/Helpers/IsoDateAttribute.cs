using System;

namespace Common.Helpers
{
    /// <summary>
    /// Marca una propiedad DateTime para que el ChangeCollector la serialice
    /// como IsoDate: <c>"yyyy-MM-dd"</c> (scalar <c>IsoDate</c> del schema GraphQL).
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class IsoDateAttribute : Attribute { }

    /// <summary>
    /// Marca una propiedad DateTime para que el ChangeCollector la serialice
    /// como IsoDatetime: <c>"yyyy-MM-ddTHH:mm:ssZ"</c> (scalar <c>IsoDatetime</c> del schema GraphQL).
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class IsoDatetimeAttribute : Attribute { }
}
