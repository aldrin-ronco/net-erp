using Caliburn.Micro;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;

namespace NetErp.Global.CompanyPermissionDefault.DTO
{
    public enum PermissionTreeNodeType
    {
        Module,
        Group,
        Item,
        PermissionTypeGroup,
        Permission
    }

    public enum PermissionDefaultValue
    {
        Allowed,
        Denied,
        Required,
        Optional
    }

    public record PermissionDefaultValueOption(PermissionDefaultValue Value, string Display);

    public class PermissionTreeNodeDTO : PropertyChangedBase
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public PermissionTreeNodeType NodeType { get; set; }
        public PermissionTreeNodeDTO? Parent { get; set; }
        public ObservableCollection<PermissionTreeNodeDTO> Children { get; set; } = [];

        public bool IsPermission => NodeType == PermissionTreeNodeType.Permission;

        public FontWeight NodeFontWeight => NodeType switch
        {
            PermissionTreeNodeType.Group => FontWeights.Medium,
            _ => FontWeights.Normal
        };

        public double NodeFontSize => NodeType switch
        {
            PermissionTreeNodeType.Module => 13,
            PermissionTreeNodeType.Group => 12.5,
            PermissionTreeNodeType.Item => 12,
            PermissionTreeNodeType.PermissionTypeGroup => 11.5,
            _ => 12
        };

        public string DisplayName => NodeType == PermissionTreeNodeType.Module ? Name.ToUpperInvariant() : Name;

        private static readonly SolidColorBrush ModuleBrush = new((Color)ColorConverter.ConvertFromString("#555555"));
        private static readonly SolidColorBrush GroupBrush = new((Color)ColorConverter.ConvertFromString("#666666"));
        private static readonly SolidColorBrush ItemBrush = new((Color)ColorConverter.ConvertFromString("#777777"));
        private static readonly SolidColorBrush PermTypeBrush = new((Color)ColorConverter.ConvertFromString("#888888"));
        private static readonly SolidColorBrush PermissionBrush = new((Color)ColorConverter.ConvertFromString("#888888"));

        public SolidColorBrush NodeForeground => NodeType switch
        {
            PermissionTreeNodeType.Module => ModuleBrush,
            PermissionTreeNodeType.Group => GroupBrush,
            PermissionTreeNodeType.Item => ItemBrush,
            PermissionTreeNodeType.PermissionTypeGroup => PermTypeBrush,
            _ => PermissionBrush
        };

        private static readonly SolidColorBrush ModuleBgBrush = new((Color)ColorConverter.ConvertFromString("#F0F0F0"));
        private static readonly SolidColorBrush GroupBgBrush = new((Color)ColorConverter.ConvertFromString("#F5F5F5"));
        private static readonly SolidColorBrush ItemBgBrush = new((Color)ColorConverter.ConvertFromString("#FAFAFA"));
        private static readonly SolidColorBrush PermTypeGroupBgBrush = new((Color)ColorConverter.ConvertFromString("#FDFDFD"));
        private static readonly SolidColorBrush TransparentBrush = Brushes.Transparent;

        public SolidColorBrush NodeBackground => NodeType switch
        {
            PermissionTreeNodeType.Module => ModuleBgBrush,
            PermissionTreeNodeType.Group => GroupBgBrush,
            PermissionTreeNodeType.Item => ItemBgBrush,
            PermissionTreeNodeType.PermissionTypeGroup => PermTypeGroupBgBrush,
            _ => TransparentBrush
        };

        // Permission-level properties
        public string Code { get; set; } = string.Empty;
        public string PermissionType { get; set; } = string.Empty;
        public string SystemDefault { get; set; } = string.Empty;
        public int? CompanyPermissionDefaultId { get; set; }

        public PermissionDefaultValue? CompanyDefaultValue
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(CompanyDefaultValue));
                    NotifyOfPropertyChange(nameof(HasChanged));
                    NotifyOfPropertyChange(nameof(HasCompanyOverride));
                    NotifyOfPropertyChange(nameof(IsEffectivelyPositive));
                    NotifyOfPropertyChange(nameof(PositiveIconOpacity));
                    NotifyOfPropertyChange(nameof(NegativeIconOpacity));
                }
            }
        }

        public PermissionDefaultValue? OriginalCompanyDefaultValue { get; set; }

        public string SystemDefaultDisplay => SystemDefault switch
        {
            "ALLOWED" => "Permitido",
            "DENIED" => "Denegado",
            "REQUIRED" => "Requerido",
            "OPTIONAL" => "Opcional",
            _ => SystemDefault
        };

        public bool HasCompanyOverride => CompanyDefaultValue != null;
        public bool HasChanged => CompanyDefaultValue != OriginalCompanyDefaultValue;

        /// <summary>
        /// Returns the combo options list appropriate for this permission's type.
        /// ACTION → Permitido/Denegado, FIELD → Requerido/Opcional
        /// </summary>
        public IReadOnlyList<PermissionDefaultValueOption> ApplicableOptions =>
            PermissionType == "ACTION" ? ActionOptions : FieldOptions;

        /// <summary>
        /// Whether the effective value is "positive" (Allowed for ACTION, Optional for FIELD).
        /// Used to show the green check icon vs the gray lock icon.
        /// </summary>
        public bool IsEffectivelyPositive
        {
            get
            {
                if (CompanyDefaultValue != null)
                    return CompanyDefaultValue is PermissionDefaultValue.Allowed or PermissionDefaultValue.Optional;

                return SystemDefault is "ALLOWED" or "OPTIONAL";
            }
        }

        public double PositiveIconOpacity => HasCompanyOverride ? 1.0 : 0.5;
        public double NegativeIconOpacity => HasCompanyOverride ? 0.8 : 0.4;

        // Static option lists
        public static readonly IReadOnlyList<PermissionDefaultValueOption> ActionOptions =
        [
            new(PermissionDefaultValue.Allowed, "Permitido"),
            new(PermissionDefaultValue.Denied, "Denegado")
        ];

        public static readonly IReadOnlyList<PermissionDefaultValueOption> FieldOptions =
        [
            new(PermissionDefaultValue.Required, "Requerido"),
            new(PermissionDefaultValue.Optional, "Opcional")
        ];
    }
}
