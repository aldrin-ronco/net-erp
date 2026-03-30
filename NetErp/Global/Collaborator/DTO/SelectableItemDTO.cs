using Caliburn.Micro;

namespace NetErp.Global.Collaborator.DTO
{
    public class SelectableItemDTO : PropertyChangedBase
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

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

        public string DisplayText => string.IsNullOrEmpty(Description)
            ? Name
            : $"{Description} ({Name})";
    }
}
