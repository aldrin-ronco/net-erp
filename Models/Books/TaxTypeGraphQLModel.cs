using Models.Global;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Books
{
    public class TaxTypeGraphQLModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool GeneratedTaxAccountIsRequired { get; set; }
        public bool GeneratedTaxRefundAccountIsRequired { get; set; }
        public bool DeductibleTaxAccountIsRequired { get; set; }
        public bool DeductibleTaxRefundAccountIsRequired { get; set; }
        public string Prefix { get; set; } = string.Empty;
    }


    public class TaxTypeCreateMessage
    {
        public TaxTypeGraphQLModel CreatedTaxType { get; set; }
        public ObservableCollection<TaxTypeGraphQLModel> TaxTypes { get; set; }
    }
    public class TaxTypeDeleteMessage
    {
        public TaxTypeGraphQLModel DeletedTaxType { get; set; }
       
    }

    public class TaxTypeUpdateMessage
    {
        public TaxTypeGraphQLModel UpdatedTaxType { get; set; }
        public ObservableCollection<TaxTypeGraphQLModel> TaxTypes { get; set; }
    }
}
