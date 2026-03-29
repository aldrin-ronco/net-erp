using System.Collections.Generic;
using System.Threading.Tasks;

namespace Common.Interfaces
{
    public interface IAdminRecentCompanyService
    {
        Task<List<AdminRecentCompanyEntry>> GetRecentCompaniesAsync(int accountId, int limit = 10);
        Task SaveRecentCompanyAsync(int accountId, int companyId, string companyData, string displayName, string organizationName);
    }

    public class AdminRecentCompanyEntry
    {
        public int AccountId { get; set; }
        public int CompanyId { get; set; }
        public string CompanyData { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string OrganizationName { get; set; } = string.Empty;
        public string LastAccessedAt { get; set; } = string.Empty;
    }
}
