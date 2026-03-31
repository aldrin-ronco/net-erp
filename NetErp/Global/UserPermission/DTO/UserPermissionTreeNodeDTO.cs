using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;

namespace NetErp.Global.UserPermission.DTO
{
    public enum UserPermissionTreeNodeType
    {
        Module,
        Group,
        Item,
        PermissionTypeGroup,
        Permission
    }

    public enum UserPermissionValue
    {
        Allowed,
        Denied,
        Required,
        Optional
    }

    public record UserPermissionValueOption(UserPermissionValue Value, string Display);

    public class UserPermissionTreeNodeDTO : PropertyChangedBase
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public UserPermissionTreeNodeType NodeType { get; set; }
        public UserPermissionTreeNodeDTO? Parent { get; set; }
        public ObservableCollection<UserPermissionTreeNodeDTO> Children { get; set; } = [];

        public bool IsPermission => NodeType == UserPermissionTreeNodeType.Permission;

        public FontWeight NodeFontWeight => NodeType switch
        {
            UserPermissionTreeNodeType.Group => FontWeights.Medium,
            _ => FontWeights.Normal
        };

        public double NodeFontSize => NodeType switch
        {
            UserPermissionTreeNodeType.Module => 13,
            UserPermissionTreeNodeType.Group => 12.5,
            UserPermissionTreeNodeType.Item => 12,
            UserPermissionTreeNodeType.PermissionTypeGroup => 11.5,
            _ => 12
        };

        public string DisplayName => NodeType == UserPermissionTreeNodeType.Module ? Name.ToUpperInvariant() : Name;

        private static readonly SolidColorBrush ModuleBrush = new((Color)ColorConverter.ConvertFromString("#555555"));
        private static readonly SolidColorBrush GroupBrush = new((Color)ColorConverter.ConvertFromString("#666666"));
        private static readonly SolidColorBrush ItemBrush = new((Color)ColorConverter.ConvertFromString("#777777"));
        private static readonly SolidColorBrush PermTypeBrush = new((Color)ColorConverter.ConvertFromString("#888888"));
        private static readonly SolidColorBrush PermissionBrush = new((Color)ColorConverter.ConvertFromString("#888888"));

        public SolidColorBrush NodeForeground => NodeType switch
        {
            UserPermissionTreeNodeType.Module => ModuleBrush,
            UserPermissionTreeNodeType.Group => GroupBrush,
            UserPermissionTreeNodeType.Item => ItemBrush,
            UserPermissionTreeNodeType.PermissionTypeGroup => PermTypeBrush,
            _ => PermissionBrush
        };

        // Permission-level properties
        public string Code { get; set; } = string.Empty;
        public string PermissionType { get; set; } = string.Empty;
        public string EffectiveDefault { get; set; } = string.Empty;
        public int? UserPermissionId { get; set; }

        public UserPermissionValue? UserValue
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    if (value == null) ExpiresAt = null;
                    NotifyOfPropertyChange(nameof(UserValue));
                    NotifyOfPropertyChange(nameof(HasChanged));
                    NotifyOfPropertyChange(nameof(HasUserOverride));
                    NotifyOfPropertyChange(nameof(IsEffectivelyPositive));
                    NotifyOfPropertyChange(nameof(PositiveIconOpacity));
                    NotifyOfPropertyChange(nameof(NegativeIconOpacity));
                }
            }
        }

        public UserPermissionValue? OriginalUserValue { get; set; }

        public DateTime? ExpiresAt
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(ExpiresAt));
                    NotifyOfPropertyChange(nameof(HasChanged));
                }
            }
        }

        public DateTime? OriginalExpiresAt { get; set; }

        public string EffectiveDefaultDisplay => EffectiveDefault switch
        {
            "ALLOWED" => "Permitido",
            "DENIED" => "Denegado",
            "REQUIRED" => "Requerido",
            "OPTIONAL" => "Opcional",
            _ => EffectiveDefault
        };

        public IReadOnlyList<UserPermissionValueOption> ApplicableOptions =>
            PermissionType == "ACTION" ? ActionOptions : FieldOptions;

        public bool HasUserOverride => UserValue != null;
        public bool HasChanged => UserValue != OriginalUserValue || ExpiresAt != OriginalExpiresAt;

        public bool IsEffectivelyPositive
        {
            get
            {
                if (UserValue != null)
                    return UserValue is UserPermissionValue.Allowed or UserPermissionValue.Optional;

                return EffectiveDefault is "ALLOWED" or "OPTIONAL";
            }
        }

        public double PositiveIconOpacity => HasUserOverride ? 1.0 : 0.5;
        public double NegativeIconOpacity => HasUserOverride ? 0.8 : 0.4;

        // Static option lists
        public static readonly IReadOnlyList<UserPermissionValueOption> ActionOptions =
        [
            new(UserPermissionValue.Allowed, "Permitido"),
            new(UserPermissionValue.Denied, "Denegado")
        ];

        public static readonly IReadOnlyList<UserPermissionValueOption> FieldOptions =
        [
            new(UserPermissionValue.Required, "Requerido"),
            new(UserPermissionValue.Optional, "Opcional")
        ];
    }
}
