using System;

namespace NetErp.Global.Collaborator.DTO
{
    public class CollaboratorDisplayDTO
    {
        public int AccountId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Profession { get; set; } = string.Empty;
        public string PhotoUrl { get; set; } = string.Empty;
        public string ProfileNames { get; set; } = string.Empty;
        public string InvitedBy { get; set; } = string.Empty;
        public bool IsOwner { get; set; }
        public string RoleDisplay => IsOwner ? "Propietario" : "Colaborador";
        public string RoleForeground => IsOwner ? "#c62828" : "#1976d2";
        public DateTime JoinedAt { get; set; }
        public string JoinedAtDisplay => JoinedAt == DateTime.MinValue ? string.Empty : JoinedAt.ToLocalTime().ToString("dd/MM/yyyy");
    }
}
