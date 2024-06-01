using System.Windows;
using System.Windows.Controls;

namespace NetErp.Helpers
{
    public class ComboBoxItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate SelectedTemplate { get; set; }
        public DataTemplate DropDownTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var presenter = (ContentPresenter)container;
            return (presenter.TemplatedParent is ComboBox) ? SelectedTemplate : DropDownTemplate;
        }
    }
}
