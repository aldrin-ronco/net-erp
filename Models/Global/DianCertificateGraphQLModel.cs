using System;
using static Models.Global.GraphQLResponseTypes;

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
        public bool IsExpired => ValidTo.HasValue && ValidTo.Value < DateTime.Now;
        public bool IsDefault { get; set; }
        public DateTime? InsertedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class ActiveDianCertificateDataContext
    {
        public DianCertificateGraphQLModel ActiveDianCertificate { get; set; }
    }

    public class GlobalConfigDianCertificateContext
    {
        public GlobalConfigDefaultCertificate GlobalConfig { get; set; }
    }

    public class GlobalConfigDefaultCertificate
    {
        public DianCertificateGraphQLModel DefaultDianCertificate { get; set; }
    }

    public class DianCertificateCreateMessage
    {
        public UpsertResponseType<DianCertificateGraphQLModel> CreatedCertificate { get; set; }
    }

    public class DianCertificateDeleteMessage
    {
        public DeleteResponseType DeletedCertificate { get; set; }
    }
}
