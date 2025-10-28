using System;

namespace Common.Helpers
{
    /// <summary>
    /// Permite definir la ruta personalizada en el ExpandoObject para una propiedad.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ExpandoPathAttribute : Attribute
    {
        public string Path { get; }

        public ExpandoPathAttribute(string path)
        {
            Path = path;
        }
    }
}
