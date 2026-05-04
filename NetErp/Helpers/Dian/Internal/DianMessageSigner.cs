using Chilkat;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace NetErp.Helpers.Dian.Internal
{
    /// <summary>
    /// Componente interno: firma WS-Security X.509 sobre el envelope SOAP 1.2 con
    /// RSA-SHA256, canonicalización exclusiva (EXCL_C14N) y digest SHA-256.
    /// Encapsula el desbloqueo de Chilkat (una vez por proceso, thread-safe).
    /// </summary>
    internal static class DianMessageSigner
    {
        private const string ChilkatUnlockKey = "ALDRNC.CB1022026_TnVCLrgM49mL";
        private static bool _chilkatUnlocked;
        private static readonly Lock _unlockLock = new();

        public static void EnsureUnlocked()
        {
            if (_chilkatUnlocked) return;
            lock (_unlockLock)
            {
                if (_chilkatUnlocked) return;
                Chilkat.Global glob = new();
                bool success = glob.UnlockBundle(ChilkatUnlockKey);
                if (!success) throw new InvalidOperationException("Ha fallado el desbloqueo de Chilkat");
                _chilkatUnlocked = true;
            }
        }

        public static string Sign(string webServiceMethod, string webService, string body, string certificatePem, string privateKeyPem)
        {
            EnsureUnlocked();

            string wsuCreated = DateTime.Now.AddHours(5).ToString("yyyy-MM-dd'T'HH:mm:ss.000Z");
            string wsuExpires = DateTime.Now.AddHours(22).ToString("yyyy-MM-dd'T'HH:mm:ss.000Z");

            StringBuilder sbXml = new();
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

            using var netCert = X509Certificate2.CreateFromPem(certificatePem, privateKeyPem);
            byte[] pfxBytes = netCert.Export(X509ContentType.Pfx, "temp");
            string pfxBase64 = Convert.ToBase64String(pfxBytes);

            Pfx pfx = new();
            bool success = pfx.LoadPfxEncoded(pfxBase64, "base64", "temp");
            if (!success) throw new InvalidOperationException("Ha fallado la carga del certificado");

            Cert cert = pfx.GetCert(0);

            Xml refXml = new() { Tag = "wsse:SecurityTokenReference" };
            refXml.UpdateAttrAt("wsse:Reference", true, "URI", "#X509-E18C26835F9A7946EA15544903040561");
            refXml.UpdateAttrAt("wsse:Reference", true, "ValueType", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-x509-token-profile-1.0#X509v3");
            refXml.EmitXmlDecl = false;

            XmlDSigGen gen = new()
            {
                SigLocation = "soap:Envelope|soap:Header|wsse:Security",
                SigId = "SIG-E18C26835F9A7946EA15544903041175",
                KeyInfoId = "KI-E18C26835F9A7946EA15544903040902",
                SignedInfoPrefixList = "wsa soap wcf"
            };
            gen.AddSameDocRef("id-E18C26835F9A7946EA15544903041014", "sha256", "EXCL_C14N", "soap wcf", "");
            gen.KeyInfoType = "Custom";
            refXml.EmitCompact = true;
            gen.CustomKeyInfoXml = refXml.GetXml();
            gen.SetX509Cert(cert, true);

            success = gen.CreateXmlDSigSb(sbXml);
            if (!success) throw new InvalidOperationException("Ha fallado la firma digital del request");

            return sbXml.GetAsString();
        }

        private static string GetB64Certificate(string certificatePem)
        {
            using var cert = X509Certificate2.CreateFromPem(certificatePem);
            return Convert.ToBase64String(cert.RawData);
        }
    }
}
