using Common.Config;
using Models.Global;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using Chilkat;

namespace NetErp.Helpers
{
    public static class GetAuthorizationSequences
    {
        private const string SoapActionPrefix = "http://wcf.dian.colombia/IWcfDianCustomerServices/";

        public static AuthorizationSequenceResponse GetNumberingRange(
            DianSoftwareConfigGraphQLModel config,
            DianCertificateGraphQLModel certificate)
        {
            try
            {
                Chilkat.Global glob = new Chilkat.Global();
                bool success = glob.UnlockBundle("ALDRNC.CB1022026_TnVCLrgM49mL");
                if (!success)
                {
                    throw new InvalidOperationException("Ha fallado el desbloqueo de Chilkat");
                }

                string wsAction = SoapActionPrefix + RequestMethods.GetNumberingRange;
                string body = GetBody(config.ProviderNit, config.SoftwareId);
                string request = MakeSignedRequest(wsAction, config.ServiceUrl, body, certificate.CertificatePem, certificate.PrivateKeyPem);
                string result = SendToDian(request, config.WsdlUrl);

                return GetSequencesFromXml(result);
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Error al consultar numeración DIAN: {e.Message}", e);
            }
        }

        private static AuthorizationSequenceResponse GetSequencesFromXml(string xmlString)
        {
            AuthorizationSequenceResponse result = new AuthorizationSequenceResponse();
            XmlDocument xDoc = new XmlDocument();
            xDoc.LoadXml(xmlString);
            XmlNamespaceManager namespaces = new XmlNamespaceManager(xDoc.NameTable);
            namespaces.AddNamespace("s", "http://www.w3.org/2003/05/soap-envelope");
            namespaces.AddNamespace("b", "http://schemas.datacontract.org/2004/07/NumberRangeResponseList");
            namespaces.AddNamespace("c", "http://schemas.datacontract.org/2004/07/NumberRangeResponse");

            XmlNode responseList = xDoc.SelectSingleNode("//b:ResponseList", namespaces);

            if (responseList != null)
            {
                ObservableCollection<AuthorizationSequenceGraphQLModel> authorizationSequences = [];
                foreach (XmlNode item in responseList.ChildNodes)
                {
                    AuthorizationSequenceGraphQLModel sec = new AuthorizationSequenceGraphQLModel();
                    sec.Number = item.SelectSingleNode("c:ResolutionNumber", namespaces)?.InnerText ?? string.Empty;
                    sec.Prefix = item.SelectSingleNode("c:Prefix", namespaces)?.InnerText ?? string.Empty;
                    sec.StartRange = int.Parse(item.SelectSingleNode("c:FromNumber", namespaces)?.InnerText ?? "0");
                    sec.EndRange = int.Parse(item.SelectSingleNode("c:ToNumber", namespaces)?.InnerText ?? "0");
                    sec.StartDate = GetDateOnly(item.SelectSingleNode("c:ValidDateFrom", namespaces)?.InnerText ?? "");
                    sec.EndDate = GetDateOnly(item.SelectSingleNode("c:ValidDateTo", namespaces)?.InnerText ?? "");
                    sec.TechnicalKey = item.SelectSingleNode("c:TechnicalKey", namespaces)?.InnerText ?? string.Empty;
                    sec.Description = $"AUTORIZACION DIAN No. {sec.Number} de {sec.StartDate}, prefijo: {sec.Prefix} del {sec.StartDate} al {sec.EndRange}";
                    authorizationSequences.Add(sec);
                }
                result.Status = true;
                result.AuthorizationSequences = authorizationSequences;
                return result;
            }
            else
            {
                result.Status = false;
                XmlNode fail = xDoc.SelectSingleNode("//s:Reason", namespaces);
                result.Message = fail?.InnerText ?? "Respuesta inesperada de la DIAN";
                return result;
            }
        }

        private static DateOnly GetDateOnly(string str)
        {
            string[] subs = str.Split('-');
            if (subs.Length >= 3)
            {
                return new DateOnly(int.Parse(subs[0]), int.Parse(subs[1]), int.Parse(subs[2]));
            }
            return new DateOnly();
        }

        private static string GetBody(string nit, string softwareId)
        {
            return $"<wcf:GetNumberingRange>" +
                   $"<wcf:accountCode>{nit}</wcf:accountCode>" +
                   $"<wcf:accountCodeT>{nit}</wcf:accountCodeT>" +
                   $"<wcf:softwareCode>{softwareId}</wcf:softwareCode>" +
                   $"</wcf:GetNumberingRange>";
        }

        private static string MakeSignedRequest(string webServiceMethod, string webService, string body, string certificatePem, string privateKeyPem)
        {
            string wsuCreated = DateTime.Now.AddHours(5).ToString("yyyy-MM-dd'T'HH:mm:ss.000Z");
            string wsuExpires = DateTime.Now.AddHours(22).ToString("yyyy-MM-dd'T'HH:mm:ss.000Z");

            StringBuilder sbXml = new StringBuilder();
            string base64Cert = GetB64Certificate(certificatePem);

            string template = $@"<soap:Envelope xmlns:soap=""http://www.w3.org/2003/05/soap-envelope"" xmlns:wcf=""http://wcf.dian.colombia"">
             <soap:Header xmlns:wsa=""http://www.w3.org/2005/08/addressing"">
             <wsse:Security xmlns:wsse=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd"" xmlns:wsu=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd"">
             <wsu:Timestamp wsu:Id=""TS-E18C26835F9A7946EA15544903041616"">
             <wsu:Created>{wsuCreated}</wsu:Created>
             <wsu:Expires>{wsuExpires}</wsu:Expires>
             </wsu:Timestamp>
             <wsse:BinarySecurityToken EncodingType=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-soap-message-security-1.0#Base64Binary"" ValueType=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-x509-token-profile-1.0#X509v3"" wsu:Id=""X509-E18C26835F9A7946EA15544903040561"">{base64Cert}</wsse:BinarySecurityToken></wsse:Security>
             <wsa:Action>{webServiceMethod}</wsa:Action>
             <wsa:To wsu:Id=""id-E18C26835F9A7946EA15544903041014"" xmlns:wsu=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd"">{webService}</wsa:To>
             </soap:Header>
             <soap:Body>
             {body}
             </soap:Body>
             </soap:Envelope>";
            sbXml.SetString(template);

            // Convertir PEM a PFX en memoria para firma con Chilkat
            using var netCert = X509Certificate2.CreateFromPem(certificatePem, privateKeyPem);
            byte[] pfxBytes = netCert.Export(X509ContentType.Pfx, "temp");
            string pfxBase64 = Convert.ToBase64String(pfxBytes);

            Pfx pfx = new Pfx();
            bool success = pfx.LoadPfxEncoded(pfxBase64, "base64", "temp");
            if (!success)
            {
                throw new InvalidOperationException("Ha fallado la carga del certificado");
            }

            Cert cert = pfx.GetCert(0);

            Xml refXml = new Xml();
            refXml.Tag = "wsse:SecurityTokenReference";
            refXml.UpdateAttrAt("wsse:Reference", true, "URI", "#X509-E18C26835F9A7946EA15544903040561");
            refXml.UpdateAttrAt("wsse:Reference", true, "ValueType", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-x509-token-profile-1.0#X509v3");
            refXml.EmitXmlDecl = false;
            Debug.WriteLine(refXml.GetXml());

            XmlDSigGen gen = new XmlDSigGen();
            gen.SigLocation = "soap:Envelope|soap:Header|wsse:Security";
            gen.SigId = "SIG-E18C26835F9A7946EA15544903041175";
            gen.KeyInfoId = "KI-E18C26835F9A7946EA15544903040902";
            gen.SignedInfoPrefixList = "wsa soap wcf";
            gen.AddSameDocRef("id-E18C26835F9A7946EA15544903041014", "sha256", "EXCL_C14N", "soap wcf", "");
            gen.KeyInfoType = "Custom";
            refXml.EmitCompact = true;
            gen.CustomKeyInfoXml = refXml.GetXml();
            gen.SetX509Cert(cert, true);

            success = gen.CreateXmlDSigSb(sbXml);
            if (!success)
            {
                throw new InvalidOperationException("Ha fallado la firma digital del request");
            }

            return sbXml.GetAsString();
        }

        private static string SendToDian(string request, string wsdlUrl)
        {
            Http loHttp = new Http();
            loHttp.SetRequestHeader("Content-Type", "application/soap+xml");
            var loResp = loHttp.PostXml(wsdlUrl, request, "utf-8");

            if (loResp == null)
            {
                throw new InvalidOperationException(
                    "No se recibió respuesta de la DIAN." + Environment.NewLine +
                    "1. Verifique que su conexión a internet no presenta inconvenientes" + Environment.NewLine +
                    "2. Intente más tarde a que se restablezcan los servicios de la DIAN");
            }

            Xml loRespXml = new Xml();
            loRespXml.LoadXml(loResp.BodyStr);
            string responseXml = loRespXml.GetXml();
            Debug.WriteLine("=== DIAN RESPONSE ===");
            Debug.WriteLine(responseXml);
            Debug.WriteLine("=== END DIAN RESPONSE ===");
            return responseXml;
        }

        private static string GetB64Certificate(string certificatePem)
        {
            using var cert = X509Certificate2.CreateFromPem(certificatePem);
            return Convert.ToBase64String(cert.RawData);
        }
    }
}
