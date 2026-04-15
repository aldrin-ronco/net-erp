using Models.Billing;
using NetErp.Billing.PriceList.DTO;

namespace NetErp.Billing.PriceList.PriceListHelpers
{
    public class StandardPriceListCalculator : IPriceListCalculator
    {
        public void RecalculateProductValues(PriceListItemDTO item, string modifiedProperty, PriceListGraphQLModel priceList)
        {
            switch (modifiedProperty)
            {
                case nameof(PriceListItemDTO.ProfitMargin):
                    RecalculateFromProfitMargin(item, priceList);
                    break;
                case nameof(PriceListItemDTO.Price):
                    RecalculateFromPrice(item, priceList);
                    break;
                case nameof(PriceListItemDTO.DiscountMargin):
                    RecalculateMinimumFromDiscount(item);
                    break;
                case nameof(PriceListItemDTO.MinimumPrice):
                    RecalculateDiscountFromMinimum(item);
                    break;
            }
        }

        private static void RecalculateFromProfitMargin(PriceListItemDTO item, PriceListGraphQLModel priceList)
        {
            if (item.ProfitMargin == 100m) return;

            decimal m = item.ProfitMargin / 100m;
            decimal ivaRate = GetEffectiveIvaRate(item, priceList) / 100m;

            decimal priceSinIva = priceList.UseAlternativeFormula
                ? item.Cost * (1 + m)
                : item.Cost / (1 - m);

            decimal price = priceSinIva * (1 + ivaRate);
            item.UpdatePropertySilently(nameof(PriceListItemDTO.Price), price);

            decimal priceWithDiscount = price * (1 - item.DiscountMargin / 100m);
            item.UpdatePropertySilently(nameof(PriceListItemDTO.MinimumPrice), priceWithDiscount);
        }

        private static void RecalculateFromPrice(PriceListItemDTO item, PriceListGraphQLModel priceList)
        {
            if (item.Price == 0) return;

            decimal ivaRate = GetEffectiveIvaRate(item, priceList) / 100m;
            decimal priceSinIva = item.Price / (1 + ivaRate);

            if (priceSinIva == 0) return;

            decimal m = priceList.UseAlternativeFormula
                ? (item.Cost == 0 ? 0 : priceSinIva / item.Cost - 1)
                : 1 - item.Cost / priceSinIva;

            item.UpdatePropertySilently(nameof(PriceListItemDTO.ProfitMargin), m * 100m);

            decimal priceWithDiscount = item.Price * (1 - item.DiscountMargin / 100m);
            item.UpdatePropertySilently(nameof(PriceListItemDTO.MinimumPrice), priceWithDiscount);
        }

        private static void RecalculateMinimumFromDiscount(PriceListItemDTO item)
        {
            decimal priceWithDiscount = item.Price * (1 - item.DiscountMargin / 100m);
            item.UpdatePropertySilently(nameof(PriceListItemDTO.MinimumPrice), priceWithDiscount);
        }

        private static void RecalculateDiscountFromMinimum(PriceListItemDTO item)
        {
            if (item.Price == 0) return;
            decimal discountMargin = (1 - item.MinimumPrice / item.Price) * 100m;
            item.UpdatePropertySilently(nameof(PriceListItemDTO.DiscountMargin), discountMargin);
        }

        private static decimal GetEffectiveIvaRate(PriceListItemDTO item, PriceListGraphQLModel priceList)
        {
            if (!priceList.IsTaxable || !priceList.PriceListIncludeTax) return 0;
            var ag = item.Item?.AccountingGroup;
            if (ag is null) return 0;
            if (ag.SalesPrimaryTax?.TaxCategory?.Prefix == "IVA") return ag.SalesPrimaryTax.Rate;
            if (ag.SalesSecondaryTax?.TaxCategory?.Prefix == "IVA") return ag.SalesSecondaryTax.Rate;
            return 0;
        }
    }
}
