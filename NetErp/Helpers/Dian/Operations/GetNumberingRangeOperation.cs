using Models.Global;
using System;
using System.Collections.ObjectModel;
using System.Xml;

namespace NetErp.Helpers.Dian.Operations
{
    /// <summary>
    /// Consulta los rangos de numeración (resoluciones autorizadas) que la DIAN ha
    /// emitido para el facturador electrónico identificado por su NIT y software.
    /// Reemplaza al antiguo helper estático <c>GetAuthorizationSequences</c>.
    /// </summary>
    public sealed class GetNumberingRangeOperation : IDianOperation<AuthorizationSequenceResponse>
    {
        public string OperationName => "GetNumberingRange";

        public string BuildRequestBody(DianSoftwareConfigGraphQLModel config)
            => "<wcf:GetNumberingRange>" +
               $"<wcf:accountCode>{config.ProviderNit}</wcf:accountCode>" +
               $"<wcf:accountCodeT>{config.ProviderNit}</wcf:accountCodeT>" +
               $"<wcf:softwareCode>{config.SoftwareId}</wcf:softwareCode>" +
               "</wcf:GetNumberingRange>";

        public AuthorizationSequenceResponse ParseResponse(string responseXml)
        {
            AuthorizationSequenceResponse result = new();
            XmlDocument xDoc = new();
            xDoc.LoadXml(responseXml);
            XmlNamespaceManager namespaces = new(xDoc.NameTable);
            namespaces.AddNamespace("s", "http://www.w3.org/2003/05/soap-envelope");
            namespaces.AddNamespace("b", "http://schemas.datacontract.org/2004/07/NumberRangeResponseList");
            namespaces.AddNamespace("c", "http://schemas.datacontract.org/2004/07/NumberRangeResponse");
            namespaces.AddNamespace("i", "http://www.w3.org/2001/XMLSchema-instance");

            XmlNode? responseList = xDoc.SelectSingleNode("//b:ResponseList", namespaces);
            bool isNil = responseList?.Attributes?["i:nil"]?.Value == "true";

            if (responseList != null && !isNil && responseList.HasChildNodes)
            {
                ObservableCollection<AuthorizationSequenceGraphQLModel> sequences = [];
                foreach (XmlNode item in responseList.ChildNodes)
                {
                    AuthorizationSequenceGraphQLModel seq = new()
                    {
                        Number = item.SelectSingleNode("c:ResolutionNumber", namespaces)?.InnerText ?? string.Empty,
                        Prefix = item.SelectSingleNode("c:Prefix", namespaces)?.InnerText ?? string.Empty,
                        StartRange = int.Parse(item.SelectSingleNode("c:FromNumber", namespaces)?.InnerText ?? "0"),
                        EndRange = int.Parse(item.SelectSingleNode("c:ToNumber", namespaces)?.InnerText ?? "0"),
                        StartDate = ParseDate(item.SelectSingleNode("c:ValidDateFrom", namespaces)?.InnerText ?? ""),
                        EndDate = ParseDate(item.SelectSingleNode("c:ValidDateTo", namespaces)?.InnerText ?? ""),
                        TechnicalKey = item.SelectSingleNode("c:TechnicalKey", namespaces)?.InnerText ?? string.Empty
                    };
                    seq.Description = $"AUTORIZACION DIAN No. {seq.Number} de {seq.StartDate}, prefijo: {seq.Prefix} del {seq.StartRange} al {seq.EndRange}";
                    sequences.Add(seq);
                }
                result.Status = true;
                result.AuthorizationSequences = sequences;
                return result;
            }

            result.Status = false;
            XmlNode? operationDesc = xDoc.SelectSingleNode("//b:OperationDescription", namespaces);
            XmlNode? fail = xDoc.SelectSingleNode("//s:Reason", namespaces);
            result.Message = operationDesc?.InnerText ?? fail?.InnerText ?? "Respuesta inesperada de la DIAN";
            return result;
        }

        private static DateOnly ParseDate(string str)
        {
            string[] subs = str.Split('-');
            if (subs.Length >= 3)
            {
                return new DateOnly(int.Parse(subs[0]), int.Parse(subs[1]), int.Parse(subs[2]));
            }
            return new DateOnly();
        }
    }
}
