using Caliburn.Micro;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace NetErp.Global.UserPermission.DTO
{
    public class BatchPermissionNodeDTO : PropertyChangedBase
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public UserPermissionTreeNodeType NodeType { get; set; }
        public BatchPermissionNodeDTO? Parent { get; set; }
        public ObservableCollection<BatchPermissionNodeDTO> Children { get; set; } = [];

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

        private static readonly SolidColorBrush ModuleBgBrush = new((Color)ColorConverter.ConvertFromString("#F0F0F0"));
        private static readonly SolidColorBrush GroupBgBrush = new((Color)ColorConverter.ConvertFromString("#F5F5F5"));
        private static readonly SolidColorBrush ItemBgBrush = new((Color)ColorConverter.ConvertFromString("#FAFAFA"));
        private static readonly SolidColorBrush TransparentBrush = Brushes.Transparent;

        public SolidColorBrush NodeBackground => NodeType switch
        {
            UserPermissionTreeNodeType.Module => ModuleBgBrush,
            UserPermissionTreeNodeType.Group => GroupBgBrush,
            UserPermissionTreeNodeType.Item => ItemBgBrush,
            _ => TransparentBrush
        };

        // Permission-level
        public string Code { get; set; } = string.Empty;
        public string PermissionType { get; set; } = string.Empty;

        // Selection with parent↔child propagation
        private bool _isSelected;
        private bool _propagating;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    NotifyOfPropertyChange(nameof(IsSelected));

                    if (!_propagating && !IsPermission)
                    {
                        _propagating = true;
                        foreach (BatchPermissionNodeDTO child in Children)
                            child.IsSelected = value;
                        _propagating = false;
                    }

                    if (!_propagating)
                        UpdateParentSelection();
                }
            }
        }

        private void UpdateParentSelection()
        {
            if (Parent is null) return;

            Parent._propagating = true;
            bool allSelected = Parent.Children.All(c => c.IsSelected);
            if (Parent._isSelected != allSelected)
            {
                Parent._isSelected = allSelected;
                Parent.NotifyOfPropertyChange(nameof(IsSelected));
            }
            Parent._propagating = false;

            Parent.UpdateParentSelection();
        }
    }
}
