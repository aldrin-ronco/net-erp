using static Models.Global.GraphQLResponseTypes;

namespace Models.Books
{
    public class TaxCategoryGraphQLModel
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool UsesPercentage { get; set; }
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
        public required DeleteResponseType DeletedTaxCategory { get; set; }
       
    }

    public class TaxCategoryUpdateMessage
    {
        public required UpsertResponseType<TaxCategoryGraphQLModel> UpdatedTaxCategory { get; set; }
    }
}
