using Caliburn.Micro;

namespace NetErp.Global.Collaborator.DTO
{
    public class AccountSearchResultDTO : PropertyChangedBase
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string MiddleName { get; set; } = string.Empty;
        public string FirstLastName { get; set; } = string.Empty;
        public string MiddleLastName { get; set; } = string.Empty;
        public string Profession { get; set; } = string.Empty;
        public string PhotoUrl { get; set; } = string.Empty;

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    NotifyOfPropertyChange(nameof(IsSelected));
                }
            }
        }
    }
}
