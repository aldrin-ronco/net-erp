using Models.Billing;
using Models.Books;
using NetErp.Billing.PriceList.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetErp.Billing.PriceList.PriceListHelpers
{
    public interface IPriceListCalculator
    {
        Dictionary<string, decimal> FormulaVariables { get; }
        void RecalculateProductValues(PriceListItemDTO priceListDetail, string modifiedProperty, PriceListGraphQLModel priceList);
        void CalculateFromDiscountMargin(PriceListItemDTO priceListDetail);
        void CalculateFromMinimumPrice(PriceListItemDTO priceListDetail);
        void CalculateFromPrice(PriceListItemDTO priceListDetail, PriceListGraphQLModel priceList);
        void CalculateFromProfitMargin(PriceListItemDTO priceListDetail, PriceListGraphQLModel priceList);
    }
}
