using Models.Billing;
using Models.Global;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Models.Global.GraphQLResponseTypes;

namespace Models.Books
{
    public class TaxCategoryGraphQLModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool GeneratedTaxAccountIsRequired { get; set; }
        public bool GeneratedTaxRefundAccountIsRequired { get; set; }
        public bool DeductibleTaxAccountIsRequired { get; set; }
        public bool DeductibleTaxRefundAccountIsRequired { get; set; }
        public string Prefix { get; set; } = string.Empty;
    }


    public class TaxCategoryCreateMessage
    {
        public UpsertResponseType<TaxCategoryGraphQLModel> CreatedTaxCategory { get; set; } = new();
       
    }
    public class TaxCategoryDeleteMessage
    {
        public DeleteResponseType DeletedTaxCategory { get; set; }
       
    }

    public class TaxCategoryUpdateMessage
    {
        public UpsertResponseType<TaxCategoryGraphQLModel> UpdatedTaxCategory { get; set; }
    }
}
