using Caliburn.Micro;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace NetErp.Global.AccessProfile.DTO
{
    public enum MenuTreeNodeType
    {
        Module,
        Group,
        Item
    }

    public class MenuTreeNodeDTO : PropertyChangedBase
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public MenuTreeNodeType NodeType { get; set; }
        public bool IsItem => NodeType == MenuTreeNodeType.Item;
        public MenuTreeNodeDTO? Parent { get; set; }
        public ObservableCollection<MenuTreeNodeDTO> Children { get; set; } = [];

        // Presentación por nivel
        public FontWeight NodeFontWeight => NodeType switch
        {
            MenuTreeNodeType.Group => FontWeights.Medium,
            _ => FontWeights.Normal
        };

        public double NodeFontSize => NodeType switch
        {
            MenuTreeNodeType.Module => 13,
            MenuTreeNodeType.Group => 12.5,
            _ => 12
        };

        public string DisplayName => NodeType == MenuTreeNodeType.Module ? Name.ToUpperInvariant() : Name;

        private static readonly SolidColorBrush ModuleBrush = new((Color)ColorConverter.ConvertFromString("#555555"));
        private static readonly SolidColorBrush GroupBrush = new((Color)ColorConverter.ConvertFromString("#666666"));
        private static readonly SolidColorBrush ItemBrush = new((Color)ColorConverter.ConvertFromString("#777777"));

        public SolidColorBrush NodeForeground => NodeType switch
        {
            MenuTreeNodeType.Module => ModuleBrush,
            MenuTreeNodeType.Group => GroupBrush,
            _ => ItemBrush
        };

        private static readonly SolidColorBrush ModuleBgBrush = new((Color)ColorConverter.ConvertFromString("#F0F0F0"));
        private static readonly SolidColorBrush GroupBgBrush = new((Color)ColorConverter.ConvertFromString("#F5F5F5"));

        public SolidColorBrush NodeBackground => NodeType switch
        {
            MenuTreeNodeType.Module => ModuleBgBrush,
            MenuTreeNodeType.Group => GroupBgBrush,
            _ => Brushes.Transparent
        };

        private bool _isChecked;
        private bool _propagating;

        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                    NotifyOfPropertyChange(nameof(IsChecked));

                    if (!_propagating && NodeType != MenuTreeNodeType.Item)
                    {
                        _propagating = true;
                        foreach (MenuTreeNodeDTO child in Children)
                            child.IsChecked = value;
                        _propagating = false;
                    }

                    if (!_propagating)
                        UpdateParentCheck();
                }
            }
        }

        private void UpdateParentCheck()
        {
            if (Parent is null) return;

            Parent._propagating = true;
            bool allChecked = Parent.Children.All(c => c.IsChecked);
            if (Parent._isChecked != allChecked)
            {
                Parent._isChecked = allChecked;
                Parent.NotifyOfPropertyChange(nameof(IsChecked));
            }
            Parent._propagating = false;

            Parent.UpdateParentCheck();
        }
    }
}
