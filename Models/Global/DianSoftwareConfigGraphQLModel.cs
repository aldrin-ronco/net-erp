using System;

namespace Models.Global
{
    public class DianSoftwareConfigGraphQLModel
    {
        public int Id { get; set; }
        public string Environment { get; set; } = string.Empty;
        public string DocumentCategory { get; set; } = string.Empty;
        public string ProviderNit { get; set; } = string.Empty;
        public string ProviderDv { get; set; } = string.Empty;
        public string SoftwareId { get; set; } = string.Empty;
        public string SoftwarePin { get; set; } = string.Empty;
        public string TestSetId { get; set; } = string.Empty;
        public string ServiceUrl { get; set; } = string.Empty;
        public string WsdlUrl { get; set; } = string.Empty;
        public DateTime? InsertedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public CompanyGraphQLModel Company { get; set; } = new();
    }

    public class DianSoftwareConfigByScopeDataContext
    {
        public DianSoftwareConfigGraphQLModel DianSoftwareConfigByScope { get; set; }
    }
}
