using Models.Books;
using System.Windows;
using System.Windows.Controls;

namespace NetErp.Books.Reports.AuxiliaryBook.ViewModels
{
    public class HeaderTemplateSelector : DataTemplateSelector
    {
        public DataTemplate AccountingEntityTemplate { get; set; }
        public DataTemplate AcountingAccountTemplate { get; set; }
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is AuxiliaryBookGraphQLModel record)
            {
                return record.RecordType == "H" ? AcountingAccountTemplate : AccountingEntityTemplate;
            }
            else
            {
                return base.SelectTemplate(item, container);
            }
        }
    }
}
