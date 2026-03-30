using System.Collections.ObjectModel;

namespace Models.Global
{
    public class AccountGraphQLModel
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string MiddleName { get; set; } = string.Empty;
        public string FirstLastName { get; set; } = string.Empty;
        public string MiddleLastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Profession { get; set; } = string.Empty;
        public string PhotoUrl { get; set; } = string.Empty;
        public ObservableCollection<EmailGraphQLModel> Emails { get; set; } = [];
        public ObservableCollection<CostCenterGraphQLModel> CostCenters { get; set; } = [];
        public ObservableCollection<AccessProfileGraphQLModel> AccessProfiles { get; set; } = [];
    }
}
