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
        void RecalculateProductValues(PriceListDetailDTO priceListDetail, string modifiedProperty, PriceListGraphQLModel priceList);
        void CalculateFromDiscountMargin(PriceListDetailDTO priceListDetail);
        void CalculateFromMinimumPrice(PriceListDetailDTO priceListDetail);
        void CalculateFromPrice(PriceListDetailDTO priceListDetail, PriceListGraphQLModel priceList);
        void CalculateFromProfitMargin(PriceListDetailDTO priceListDetail, PriceListGraphQLModel priceList);
    }
}
