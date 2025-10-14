using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Models.Global.GraphQLResponseTypes;

namespace Models.Books
{
    public class TaxGraphQLModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Margin { get; set; }
        public AccountingAccountGraphQLModel GeneratedTaxAccount { get; set; } = new();
        public AccountingAccountGraphQLModel GeneratedTaxRefundAccount { get; set; } = new();
        public AccountingAccountGraphQLModel DeductibleTaxAccount { get; set; } = new();
        public AccountingAccountGraphQLModel DeductibleTaxRefundAccount { get; set; } = new();
        public TaxCategoryGraphQLModel TaxCategory { get; set; } = new();
        public bool IsActive { get; set; }
        public string Formula { get; set; } = string.Empty;
        public string AlternativeFormula { get; set; } = string.Empty;
    }

    public class TaxDataContext
    {
        public PageType<TaxCategoryGraphQLModel> TaxCategories { get; set; } 
        public PageType<AccountingAccountGraphQLModel> AccountingAccounts { get; set; }
    }
    public class TaxCreateMessage
    {
        public UpsertResponseType<TaxGraphQLModel> CreatedTax { get; set; } = new();

    }
    public class TaxDeleteMessage
    {
        public DeleteResponseType DeletedTax { get; set; }
    }

    public class TaxUpdateMessage
    {
        public UpsertResponseType<TaxGraphQLModel> UpdatedTax { get; set; }
    }
}
