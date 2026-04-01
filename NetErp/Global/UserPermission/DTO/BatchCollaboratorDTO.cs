using Caliburn.Micro;

namespace NetErp.Global.UserPermission.DTO
{
    public class BatchCollaboratorDTO : PropertyChangedBase
    {
        public int AccountId { get; set; }
        public string FullName { get; set; } = string.Empty;

        public bool IsSelected
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IsSelected));
                }
            }
        }
    }
}
