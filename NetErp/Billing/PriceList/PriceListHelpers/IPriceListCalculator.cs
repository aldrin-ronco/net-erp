using Models.Billing;
using NetErp.Billing.PriceList.DTO;

namespace NetErp.Billing.PriceList.PriceListHelpers
{
    public interface IPriceListCalculator
    {
        void RecalculateProductValues(PriceListItemDTO priceListDetail, string modifiedProperty, PriceListGraphQLModel priceList);
    }
}
