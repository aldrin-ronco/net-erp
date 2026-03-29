using Caliburn.Micro;
using Models.Login;

namespace NetErp.Login.DTO
{
    public class LoginCompanyInfoDTO : PropertyChangedBase
    {
        public System.Action<LoginCompanyInfoDTO>? OnSelectionChanged { get; set; }
        public int CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string OrganizationName { get; set; } = string.Empty;
        public LoginCompanyGraphQLModel OriginalData { get; set; } = new();
        
        private bool _isSelected;
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    NotifyOfPropertyChange();
                    
                    if (value)
                    {
                        OnSelectionChanged?.Invoke(this);
                    }
                }
            }
        }
        
        public string LastAccessedAt { get; set; } = string.Empty;

        public string LastAccessedDisplay => string.IsNullOrEmpty(LastAccessedAt)
            ? string.Empty
            : System.DateTime.TryParse(LastAccessedAt, null, System.Globalization.DateTimeStyles.RoundtripKind, out System.DateTime parsed)
                ? $"Último acceso: {parsed.ToLocalTime():dd/MM/yyyy hh:mm tt}"
                : string.Empty;

        public bool HasLastAccessed => !string.IsNullOrEmpty(LastAccessedAt);

        public string RoleDisplay => $"Rol: {Role.Replace("_", " ")}";
        public string RoleForeground => Role == "SYSTEM_ADMIN" ? "#c62828" : "#1976d2";
        public string CompanyInfo => $"{CompanyName} - {RoleDisplay}";
    }
}