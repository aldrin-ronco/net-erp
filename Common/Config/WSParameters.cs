using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Config
{
    public static class WSParameters
    {
        
        public static string Nit => "901539255";
        public static string SoftwareId => "6d3e7233-3fcd-482e-9c08-94b6ebfc5e38";
        public static string OutputPath => "C:\\QualityExe\\Temp_Fe\\";
        public static string CertificatePath => "C:\\QualityExe\\Certificado_FE\\Certificado_ELECTRICS\\Certificado.p12";
        public static string CertificatePassword => "LmZ561x9eCl7l8Xg";
        public static string Url => "http://wcf.dian.colombia/IWcfDianCustomerServices/";

        public static string WebService => "https://vpfe.dian.gov.co/WcfDianCustomerServices.svc";
        public static string WebServiceWsdl => "https://vpfe.dian.gov.co/WcfDianCustomerServices.svc?wsdl";
        



    }

    public static class RequestMethods
    {

        public const string GetNumberingRange = "GetNumberingRange";
        public const string GetStatus = "GetStatus";
        public const string GetStatusZip = "GetStatusZip";
        public const string GetXmlByDocumentKey = "GetXmlByDocumentKey";
        public const string SendBillAsync = "SendBillAsync";
        public const string SendBillSync = "SendBillSync";
        public const string SendTestSetAsync = "SendTestSetAsync";
        public const string SendNominaSync = "SendNominaSync";
        public const string SendEventUpdateStatus = "SendEventUpdateStatus";
        public const string GetStatusEvent = "GetStatusEvent";
    }
}
