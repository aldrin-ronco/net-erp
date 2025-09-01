using Caliburn.Micro;
using System.Collections.ObjectModel;

namespace NetErp.Login.DTO
{
    public class LoginOrganizationDTO : PropertyChangedBase
    {
        private bool _isExpanded = true; // Por defecto expandido

        public int OrganizationId { get; set; }
        public string OrganizationName { get; set; } = string.Empty;
        public ObservableCollection<LoginCompanyInfoDTO> Companies { get; set; } = [];
        public int CompanyCount => Companies.Count;

        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    NotifyOfPropertyChange();
                    NotifyOfPropertyChange(nameof(ExpandCollapseIcon));
                }
            }
        }

        public string ExpandCollapseIcon => IsExpanded ? "▼" : "▶";
    }
}