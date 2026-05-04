using Models.Global;
using System.Xml;

namespace NetErp.Helpers.Dian.Operations
{
    /// <summary>
    /// Consulta los datos del adquiriente registrados en la DIAN a partir del tipo y
    /// número de identificación, y los devuelve en <see cref="DianAcquirerResponse"/>.
    /// </summary>
    public sealed class GetAcquirerOperation(string identificationType, string identificationNumber)
        : IDianOperation<DianAcquirerResponse>
    {
        private readonly string _identificationType = identificationType;
        private readonly string _identificationNumber = identificationNumber;

        public string OperationName => "GetAcquirer";

        public string BuildRequestBody(DianSoftwareConfigGraphQLModel config)
            => "<wcf:GetAcquirer>" +
               $"<wcf:identificationType>{_identificationType}</wcf:identificationType>" +
               $"<wcf:identificationNumber>{_identificationNumber}</wcf:identificationNumber>" +
               "</wcf:GetAcquirer>";

        public DianAcquirerResponse ParseResponse(string responseXml)
        {
            XmlDocument xDoc = new();
            xDoc.LoadXml(responseXml);
            XmlNamespaceManager ns = new(xDoc.NameTable);
            ns.AddNamespace("s", "http://www.w3.org/2003/05/soap-envelope");
            ns.AddNamespace("a", "http://wcf.dian.colombia");
            ns.AddNamespace("b", "http://schemas.datacontract.org/2004/07/Gosocket.Dian.Services.Utils.Common");
            ns.AddNamespace("i", "http://www.w3.org/2001/XMLSchema-instance");

            XmlNode? result = xDoc.SelectSingleNode("//a:GetAcquirerResult", ns);
            if (result == null)
            {
                XmlNode? fail = xDoc.SelectSingleNode("//s:Reason", ns);
                return new DianAcquirerResponse
                {
                    Status = false,
                    Message = fail?.InnerText ?? "Respuesta inesperada de la DIAN"
                };
            }

            string statusCode = result.SelectSingleNode("b:StatusCode", ns)?.InnerText ?? string.Empty;
            string receiverName = result.SelectSingleNode("b:ReceiverName", ns)?.InnerText ?? string.Empty;
            string receiverEmail = result.SelectSingleNode("b:ReceiverEmail", ns)?.InnerText ?? string.Empty;

            XmlNode? messageNode = result.SelectSingleNode("b:Message", ns);
            bool messageIsNil = messageNode?.Attributes?["i:nil"]?.Value == "true";
            string message = messageIsNil ? string.Empty : messageNode?.InnerText ?? string.Empty;

            return new DianAcquirerResponse
            {
                StatusCode = statusCode,
                Status = statusCode == "200",
                Message = message,
                ReceiverName = receiverName,
                ReceiverEmail = receiverEmail
            };
        }
    }
}
