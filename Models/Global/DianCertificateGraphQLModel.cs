using System;

namespace Models.Global
{
    public class DianCertificateGraphQLModel
    {
        public int Id { get; set; }
        public string CertificatePem { get; set; } = string.Empty;
        public string PrivateKeyPem { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
        public bool IsActive { get; set; }
        public DateTime? InsertedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class ActiveDianCertificateDataContext
    {
        public DianCertificateGraphQLModel ActiveDianCertificate { get; set; }
    }
}
