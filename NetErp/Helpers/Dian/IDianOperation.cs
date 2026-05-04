using Models.Global;

namespace NetErp.Helpers.Dian
{
    /// <summary>
    /// Contrato de una operación SOAP de la DIAN. Cada operación encapsula:
    /// el nombre canónico del WSDL, la construcción del cuerpo XML del request,
    /// y el parseo tipado del response. Es agnóstica a firma WS-Security y transporte HTTP
    /// — esas responsabilidades viven en <see cref="IDianSoapClient"/> y sus internals.
    /// </summary>
    public interface IDianOperation<TResponse>
    {
        /// <summary>
        /// Nombre tal como lo expone el WSDL (p. ej. "GetAcquirer", "GetNumberingRange",
        /// "SendBillSync"). El cliente lo usa para construir el wsa:Action y el SOAPAction.
        /// </summary>
        string OperationName { get; }

        /// <summary>
        /// Construye el contenido del &lt;soap:Body&gt;. Recibe la configuración DIAN porque
        /// algunas operaciones necesitan datos como ProviderNit/SoftwareId; las que no, lo ignoran.
        /// </summary>
        string BuildRequestBody(DianSoftwareConfigGraphQLModel config);

        /// <summary>
        /// Parsea el XML completo de respuesta (SOAP envelope) y retorna el DTO tipado.
        /// </summary>
        TResponse ParseResponse(string responseXml);
    }
}
