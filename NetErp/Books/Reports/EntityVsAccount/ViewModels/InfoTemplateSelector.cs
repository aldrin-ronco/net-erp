using Models.Books;
using System.Windows;
using System.Windows.Controls;

namespace NetErp.Books.Reports.EntityVsAccount.ViewModels
{
    public class InfoTemplateSelector : DataTemplateSelector
    {
        public DataTemplate AccountingEntityTemplate { get; set; }
        public DataTemplate TotalAccountingEntityTemplate { get; set; }
        public DataTemplate AcountingAccountTemplate { get; set; }
        public DataTemplate PreviousBalanceTemplate { get; set; }
        public DataTemplate RecordTemplate { get; set; }
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is EntityVsAccountGraphQLModel record)
            {
                return record.RecordType switch
                {
                    "A" => AcountingAccountTemplate,
                    "E" => AccountingEntityTemplate,
                    "S" => PreviousBalanceTemplate,
                    "T" => TotalAccountingEntityTemplate,
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
