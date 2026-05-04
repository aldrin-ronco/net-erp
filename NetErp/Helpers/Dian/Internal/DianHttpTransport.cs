using Chilkat;
using System;
using System.Diagnostics;

namespace NetErp.Helpers.Dian.Internal
{
    /// <summary>
    /// Componente interno: POST SOAP 1.2 al endpoint DIAN. Espera el envelope ya firmado
    /// y retorna el XML de respuesta (string). Usa Chilkat.Http porque la app ya depende
    /// de esta librería para la firma; mantener un único stack HTTP/XML reduce sorpresas.
    /// </summary>
    internal static class DianHttpTransport
    {
        public static string Post(string signedRequestXml, string endpointUrl)
        {
            Http loHttp = new();
            loHttp.SetRequestHeader("Content-Type", "application/soap+xml");
            HttpResponse loResp = loHttp.PostXml(endpointUrl, signedRequestXml, "utf-8") ?? throw new InvalidOperationException(
                    "No se recibió respuesta de la DIAN." + Environment.NewLine +
                    "1. Verifique que su conexión a internet no presenta inconvenientes" + Environment.NewLine +
                    "2. Intente más tarde a que se restablezcan los servicios de la DIAN");

            Xml loRespXml = new();
            loRespXml.LoadXml(loResp.BodyStr);
            string responseXml = loRespXml.GetXml();
            Debug.WriteLine("=== DIAN RESPONSE ===");
            Debug.WriteLine(responseXml);
            Debug.WriteLine("=== END DIAN RESPONSE ===");
            return responseXml;
        }
    }
}
