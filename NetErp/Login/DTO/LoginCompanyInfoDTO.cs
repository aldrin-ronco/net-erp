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
        
        public string RoleDisplay => $"Rol: {Role}";
        public string CompanyInfo => $"{CompanyName} - {RoleDisplay}";
    }
}