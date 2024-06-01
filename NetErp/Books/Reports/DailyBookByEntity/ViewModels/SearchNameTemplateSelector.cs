using Models.Books;
using System.Windows;
using System.Windows.Controls;

namespace NetErp.Books.Reports.DailyBook.ViewModels
{
    public class SearchNameTemplateSelector : DataTemplateSelector
    {
        public DataTemplate HeaderTemplate { get; set; }
        public DataTemplate RecordTemplate { get; set; }
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is DailyBookByEntityGraphQLModel record)
            {
                return record.RecordType switch
                {
                    "H" => HeaderTemplate,
                    "N" => RecordTemplate,
                    _ => RecordTemplate
                };
            }
            else
            {
                return base.SelectTemplate(item, container);
            }
        }
    }
}
