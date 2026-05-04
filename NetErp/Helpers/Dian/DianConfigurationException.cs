using System;

namespace NetErp.Helpers.Dian
{
    /// <summary>
    /// Lanzada por <see cref="IDianSoapClient"/> cuando no hay configuración DIAN activa
    /// (DianSoftwareConfig por scope) o no hay certificado vigente (activeDianCertificate).
    /// Los ViewModels la capturan para mostrar un mensaje claro al usuario sin tratarla como
    /// un error técnico inesperado.
    /// </summary>
    public class DianConfigurationException(string message) : Exception(message);
}
