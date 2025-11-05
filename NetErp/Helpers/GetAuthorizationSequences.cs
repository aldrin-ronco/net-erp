using Common.Config;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using System.Diagnostics;
using System.Xml;
using Chilkat;
using System.Collections.ObjectModel;
using System.Windows.Forms;
using Models.Global;
namespace NetErp.Helpers
{
    public static class GetAuthorizationSequences
    {


        public static AuthorizationSequenceResponse GetNumberingRange(string Method)
        {
            try
            {
                Chilkat.Global glob = new Chilkat.Global();
                bool Success = glob.UnlockBundle("ALDRNC.CB1022026_TnVCLrgM49mL");
                if (!Success)
                {
                    throw new ArgumentException("Ha fallado el desbloqueo de Chilkat", "");
                }
                    string _WS_Action = WSParameters.Url + Method;
                    string body = GetBody(Method, WSParameters.Nit, WSParameters.Nit, WSParameters.SoftwareId);
                    string request = MakeSignedRequest(_WS_Action, WSParameters.WebService, body, WSParameters.CertificatePath, WSParameters.CertificatePassword);
                    string result = SendToDian(request, WSParameters.WebServiceWsdl, WSParameters.OutputPath);

                    return (GetSequencesFromXml(result));
                
               
            }
            catch (Exception e)
            {
                throw new ArgumentException(e.Message, "");
            }
               

        }
        private static AuthorizationSequenceResponse GetSequencesFromXml(string XmlString)
        {
            AuthorizationSequenceResponse result = new AuthorizationSequenceResponse();
            XmlDocument xDoc = new XmlDocument();
            xDoc.LoadXml(XmlString);
            XmlNamespaceManager namespaces = new XmlNamespaceManager(xDoc.NameTable);
            namespaces.AddNamespace("s", "http://www.w3.org/2003/05/soap-envelope");
            namespaces.AddNamespace("b", "http://schemas.datacontract.org/2004/07/NumberRangeResponseList");
            namespaces.AddNamespace("c", "http://schemas.datacontract.org/2004/07/NumberRangeResponse");

            XmlNode ResponseList = xDoc.SelectSingleNode("//b:ResponseList", namespaces);


           
            if(ResponseList != null)
            {
                ObservableCollection<AuthorizationSequenceGraphQLModel> authorizationSequences = [];
                foreach (XmlNode Item in ResponseList.ChildNodes)
                {
                    AuthorizationSequenceGraphQLModel sec = new AuthorizationSequenceGraphQLModel();
                    sec.Number = Item.SelectSingleNode("//c:ResolutionNumber", namespaces).InnerText;
                    //sec.ResolutionDate = Item.SelectSingleNode("//c:ResolutionDate", namespaces).InnerText;
                    sec.Prefix = Item.SelectSingleNode("//c:Prefix", namespaces).InnerText;
                    sec.StartRange =  Int32.Parse(Item.SelectSingleNode("//c:FromNumber", namespaces).InnerText);
                    sec.EndRange = Int32.Parse(Item.SelectSingleNode("//c:ToNumber", namespaces).InnerText);

                    sec.StartDate = GetDateOnly(Item.SelectSingleNode("//c:ValidDateFrom", namespaces).InnerText);
                    sec.EndDate = GetDateOnly(Item.SelectSingleNode("//c:ValidDateTo", namespaces).InnerText);
                    sec.TechnicalKey = Item.SelectSingleNode("//c:TechnicalKey", namespaces).InnerText;
                    sec.Description =  $"AUTORIZACION DIAN No. {sec.Number} de {sec.StartDate}, prefijo: {sec.Prefix} del {sec.StartDate} al {sec.EndRange}";
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
                result.Message = fail.InnerText;
                return result;
               
            }


                
        }
        private static DateOnly GetDateOnly(string str)
        {
            string[] subs = str.Split('-');
            if (subs.Length >= 2)
            {
                DateOnly date = new DateOnly(Int32.Parse(subs[0]), Int32.Parse(subs[1]), Int32.Parse(subs[2]));
                return date;
            }
            return new DateOnly();
        }
        private static string GetBody(string Method, string AccountCode, string ContentFile, string SoftwareCode)
        {
            string _BODYN = "";

            if(Method.Equals(RequestMethods.GetNumberingRange ))
             {
                 _BODYN = "<wcf:GetNumberingRange><wcf:accountCode>" + AccountCode + "</wcf:accountCode><wcf:accountCodeT>" + ContentFile + "</wcf:accountCodeT><wcf:softwareCode>" + SoftwareCode + "</wcf:softwareCode></wcf:GetNumberingRange>";
             }
            if(Method.Equals(RequestMethods.GetStatusEvent) || Method.Equals(RequestMethods.GetStatusZip) || Method.Equals(RequestMethods.GetXmlByDocumentKey) || Method.Equals(RequestMethods.GetStatus)) 
            {
                    _BODYN = "<wcf:"+ Method + "><wcf:trackId>" + AccountCode + "</wcf:trackId></wcf:"+ Method + ">";
            }          
            if(Method.Equals(RequestMethods.SendBillAsync) || Method.Equals(RequestMethods.SendBillSync) || Method.Equals(RequestMethods.SendTestSetAsync))
            {
                    _BODYN = "<wcf:"+ Method + "><wcf:fileName>" + AccountCode + "</wcf:fileName><wcf:contentFile>" + ContentFile + "</wcf:contentFile></wcf:"+ Method + ">";
            }
            if (Method.Equals(RequestMethods.SendNominaSync) || Method.Equals(RequestMethods.SendEventUpdateStatus))
            {
                    _BODYN = "<wcf:"+ Method + "><wcf:contentFile>" + ContentFile + "</wcf:contentFile></wcf:"+ Method + ">";
            }
                     

            return _BODYN;
        }
        private static string MakeSignedRequest(string WebServiceMethod, string WebService, string Body, string CertificatePath, string CertificatePassword)
        {
            string _wsuCreated = DateTime.Now.AddHours(5).ToString("yyyy-MM-dd'T'HH:mm:ss.000Z");  //+5 ?
            string _wsuExpires = DateTime.Now.AddHours(22).ToString("yyyy-MM-dd'T'HH:mm:ss.000Z"); //5+17 ?

            StringBuilder SbXml = new StringBuilder();
            Http http = new Http();
            
        string _base64Cert = GetB64Certificate(CertificatePath, CertificatePassword);

        string template = $@"<soap:Envelope xmlns:soap=""http://www.w3.org/2003/05/soap-envelope"" xmlns:wcf=""http://wcf.dian.colombia"">
             <soap:Header xmlns:wsa=""http://www.w3.org/2005/08/addressing"">
             <wsse:Security xmlns:wsse=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd"" xmlns:wsu=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd"">
             <wsu:Timestamp wsu:Id=""TS-E18C26835F9A7946EA15544903041616"">
             <wsu:Created>{_wsuCreated}</wsu:Created>
             <wsu:Expires>{_wsuExpires}</wsu:Expires>
             </wsu:Timestamp>
             <wsse:BinarySecurityToken EncodingType=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-soap-message-security-1.0#Base64Binary"" ValueType=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-x509-token-profile-1.0#X509v3"" wsu:Id=""X509-E18C26835F9A7946EA15544903040561"">{_base64Cert}</wsse:BinarySecurityToken></wsse:Security>
             <wsa:Action>{WebServiceMethod}</wsa:Action>
             <wsa:To wsu:Id=""id-E18C26835F9A7946EA15544903041014"" xmlns:wsu=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd"">{WebService}</wsa:To>
             </soap:Header>
             <soap:Body>
             {Body}
             </soap:Body>
             </soap:Envelope>";
            SbXml.SetString(template);

            // Step 2: Get the test certificate and private key stored in a.pfx
            BinData pfxData = new BinData();

            bool success = pfxData.LoadFile(CertificatePath);
             if (!success)
             {
                throw new ArgumentException("Ha fallado la carga de archivo", http.LastErrorText);
             }
            

            Pfx pfx = new Pfx();

            success = pfx.LoadPfxEncoded(pfxData.GetEncoded("base64"), "base64", CertificatePassword);
            if (!success)
            {
                throw new ArgumentException("Ha fallado la carga de archivo", http.LastErrorText);
            }
            
            Cert cert = new Cert();
            cert = pfx.GetCert(0);
            Xml refXml = new Xml();
            refXml.Tag = "wsse:SecurityTokenReference";
            refXml.UpdateAttrAt("wsse:Reference", true, "URI", "#X509-E18C26835F9A7946EA15544903040561");
            refXml.UpdateAttrAt("wsse:Reference", true, "ValueType", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-x509-token-profile-1.0#X509v3");
            refXml.EmitXmlDecl = false;
            Debug.WriteLine(refXml.GetXml()); //verificar
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


            success = gen.CreateXmlDSigSb(SbXml);
            if (!success)
            {
                throw new ArgumentException("Ha fallado la carga de archivo", http.LastErrorText);
            }
           

            return SbXml.GetAsString();

           
        }
        private static string SendToDian(string request, string WebServiceWsdl, string OutputPath)
        {
            Http loHttp = new Http();
            Xml loXml = new Xml();
            loHttp.SetRequestHeader("Content-Type", "application/soap+xml");
            var loResp = loHttp.PostXml(WebServiceWsdl, request, "utf-8");
            Xml loRespXml = new Xml();

            //validamos que la DIAN no este es caida
            try
            {
                if (loResp != null)
                {
                    loRespXml.LoadXml(loResp.BodyStr);
                }
                else
                {
                    throw new ArgumentException("Respuesta de la DIAN vacia");
                }
            }
            catch (HttpRequestException)
            {
                System.Windows.Forms.MessageBox.Show("No se puede conectar el equipo a los servidores de la DIAN" + Environment.NewLine + "¿Qué debo hacer?" + Environment.NewLine + "1. Verifique que su conexión a internet no presenta inconvenientes" + Environment.NewLine + "2. Intente más tarde a que se restablezcan los servicios de la DIAN", "Atención!!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return "";
            }
            catch (TaskCanceledException)
            {
                System.Windows.Forms.MessageBox.Show("El tiempo de espera supera el límite de conexión con los servidores de la DIAN" + Environment.NewLine + "¿Qué debo hacer?" + Environment.NewLine + "1. Verifique que su conexión a internet no presenta inconvenientes" + Environment.NewLine + "2. Intente más tarde a que se restablezcan los servicios de la DIAN", "Atención!!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return "";
            }
            catch (XmlException)
            {
                System.Windows.Forms.MessageBox.Show("Error al procesar la respuesta de los servidores de la DIAN" + Environment.NewLine + "¿Qué debo hacer?" + Environment.NewLine + "1. Verifique que la respuesta del servidor es válida" + Environment.NewLine + "2. Intente más tarde a que se restablezcan los servicios de la DIAN", "Atención!!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return "";
            }
            catch (ArgumentException ex)
            {
                System.Windows.Forms.MessageBox.Show("Parámetro no encontrado: " + ex.Message, "Atención!!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return "";
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("Ocurrió un error inesperado: " + ex.Message + Environment.NewLine + "¿Qué debo hacer?" + Environment.NewLine + "1. Verifique que su conexión a internet no presenta inconvenientes" + Environment.NewLine + "2. Intente más tarde a que se restablezcan los servicios de la DIAN", "Atención!!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return "";
            }
            return loRespXml.GetXml();
        }
        private static string GetB64Certificate(string CertificatePath, string CertificatePassword)
        {
            X509Certificate2 MonCertificat = new X509Certificate2(CertificatePath, CertificatePassword);
            X509Chain X509Chain = new X509Chain();
            X509Chain.Build(MonCertificat);
            return Convert.ToBase64String(X509Chain.ChainElements[0].Certificate.RawData);
          
        }
    }
}
