using System;

namespace Common.Helpers
{
    /// <summary>
    /// Permite definir la ruta personalizada en el ExpandoObject para una propiedad
    /// y opcionalmente indicar que, si el valor es un tipo complejo, se serialice
    /// solo su identificador (Id).
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ExpandoPathAttribute : Attribute
    {
        /// <summary>
        /// Ruta (path) a usar dentro del ExpandoObject.
        /// Puede ser algo como "customer.countryId" o "address.country".
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Si es true y el valor de la propiedad es un tipo complejo (clase, no string),
        /// el collector intentará obtener el valor de una propiedad Id (o la indicada en IdPropertyName)
        /// y enviará solo ese Id al payload.
        /// </summary>
        public bool SerializeAsId { get; set; }

        /// <summary>
        /// Nombre de la propiedad que contiene el identificador.
        /// Por defecto se usa "Id" (case-insensitive).
        /// </summary>
        public string? IdPropertyName { get; set; }

        public ExpandoPathAttribute(string path)
        {
            Path = path;
        }
    }
}
