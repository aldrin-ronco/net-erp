using Models.Billing;
using Models.Books;
using NetErp.Billing.PriceList.DTO;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NetErp.Billing.PriceList.PriceListHelpers
{
    public class AlternativePriceListCalculator : IPriceListCalculator
    {
        public Dictionary<string, decimal> FormulaVariables { get; private set; }

        public AlternativePriceListCalculator()
        {
            FormulaVariables = new Dictionary<string, decimal>
            {
                { "COSTO", 0 },
                { "MARGEN_UTILIDAD", 0 },
                { "PRECIO_DE_VENTA", 0 },
                { "MARGEN_DCTO", 0 },
                { "PRECIO_MINIMO", 0 },
                { "TOTAL_TAX_RATE", 0 },
                { "PRECIO_CON_DCTO", 0 },
                { "PRECIO_SIN_DCTO", 0 },
                { "MARGEN_IMPUESTO", 0 }

            };
        }
        public void RecalculateProductValues(PriceListDetailDTO priceListDetail, string modifiedProperty, PriceListGraphQLModel priceList)
        {
            FormulaVariables = new Dictionary<string, decimal>
            {
                { "COSTO", priceListDetail.Cost},
                { "MARGEN_UTILIDAD", priceListDetail.ProfitMargin},
                { "PRECIO_DE_VENTA", priceListDetail.Price },
                { "MARGEN_DCTO", priceListDetail.DiscountMargin},
                { "PRECIO_MINIMO", priceListDetail.MinimumPrice},
                { "TOTAL_TAX_RATE", 0 },
                { "PRECIO_CON_DCTO", 0 },
                { "PRECIO_SIN_DCTO", 0 },
                { "MARGEN_IMPUESTO", 0 }
            };

            switch (modifiedProperty)
            {
                case nameof(PriceListDetailDTO.ProfitMargin):
                    CalculateFromProfitMargin(priceListDetail, priceList);
                    break;
                case nameof(PriceListDetailDTO.Price):
                    CalculateFromPrice(priceListDetail, priceList);
                    break;
                case nameof(PriceListDetailDTO.DiscountMargin):
                    CalculateFromDiscountMargin(priceListDetail);
                    break;
                case nameof(PriceListDetailDTO.MinimumPrice):
                    CalculateFromMinimumPrice(priceListDetail);
                    break;
            }
        }

        public void CalculateFromDiscountMargin(PriceListDetailDTO priceListDetail)
        {
            decimal discountValue = priceListDetail.Price * (priceListDetail.DiscountMargin / 100);
            decimal priceWithDiscount = priceListDetail.Price - discountValue;
            priceListDetail.UpdatePropertySilently(nameof(PriceListDetailDTO.MinimumPrice), priceWithDiscount);
        }

        public void CalculateFromMinimumPrice(PriceListDetailDTO priceListDetail)
        {
            decimal discountMargin = (1 - priceListDetail.MinimumPrice / priceListDetail.Price) * 100;
            priceListDetail.UpdatePropertySilently(nameof(PriceListDetailDTO.DiscountMargin), discountMargin);
        }

        public void CalculateFromPrice(PriceListDetailDTO priceListDetail, PriceListGraphQLModel priceList)
        {
            decimal ivaMargin = ExtractIvaMargin(priceListDetail, priceList);
            decimal discountValue = priceListDetail.Price * (priceListDetail.DiscountMargin / 100);
            decimal priceWithDiscount = priceListDetail.Price - discountValue;
            decimal profit = (priceListDetail.Price / (1 + (ivaMargin / 100))) - priceListDetail.Cost;
            decimal profitMargin = (1 - ((priceListDetail.Cost * (1 +(ivaMargin / 100))) / priceListDetail.Price)) * 100;
            priceListDetail.UpdatePropertySilently(nameof(PriceListDetailDTO.ProfitMargin), profitMargin);
            priceListDetail.UpdatePropertySilently(nameof(PriceListDetailDTO.Profit), profit);
            priceListDetail.UpdatePropertySilently(nameof(PriceListDetailDTO.MinimumPrice), priceWithDiscount);
        }

        public void CalculateFromProfitMargin(PriceListDetailDTO priceListDetail, PriceListGraphQLModel priceList)
        {
            decimal ivaMargin = ExtractIvaMargin(priceListDetail, priceList);
            decimal priceWithTax = (priceListDetail.Cost * (1 + (ivaMargin/100))) / (1 - (priceListDetail.ProfitMargin / 100));
            FormulaVariables["PRECIO_SIN_DCTO"] = priceWithTax;
            string pattern = string.Join("|", FormulaVariables.Keys.Select(Regex.Escape));
            decimal ivaValue = priceList.IsTaxable && priceList.PriceListIncludeTax ? CalculateIvaValue(GetIvaTax(priceListDetail), pattern) : 0;
            decimal discountValue = priceWithTax * (priceListDetail.DiscountMargin / 100);
            decimal priceWithDiscount = priceWithTax - discountValue;
            decimal profit = priceWithTax - (priceListDetail.Cost + ivaValue);
            priceListDetail.UpdatePropertySilently(nameof(PriceListDetailDTO.Price), priceWithTax);
            priceListDetail.UpdatePropertySilently(nameof(PriceListDetailDTO.MinimumPrice), priceWithDiscount);
            priceListDetail.UpdatePropertySilently(nameof(PriceListDetailDTO.Profit), profit);
        }

        public decimal ExtractIvaMargin(PriceListDetailDTO priceListDetail, PriceListGraphQLModel priceList)
        {
            if (!priceList.IsTaxable || !priceList.PriceListIncludeTax) return 0;

            TaxGraphQLModel sellTax1 = priceListDetail.CatalogItem.AccountingGroup.SellTax1;
            TaxGraphQLModel sellTax2 = priceListDetail.CatalogItem.AccountingGroup.SellTax2;

            if(sellTax1 != null && sellTax1.TaxCategory != null && sellTax1.TaxCategory.Prefix == "IVA") return sellTax1.Margin;
            if(sellTax2 != null && sellTax2.TaxCategory != null && sellTax2.TaxCategory.Prefix == "IVA") return sellTax2.Margin;

            return 0;
        }

        public TaxGraphQLModel? GetIvaTax(PriceListDetailDTO priceListDetail)
        {
            TaxGraphQLModel? sellTax1 = priceListDetail.CatalogItem.AccountingGroup.SellTax1;
            TaxGraphQLModel? sellTax2 = priceListDetail.CatalogItem.AccountingGroup.SellTax2;

            if(sellTax1 != null && sellTax1.TaxCategory != null && sellTax1.TaxCategory.Prefix == "IVA") return sellTax1;
            if(sellTax2 != null && sellTax2.TaxCategory != null && sellTax2.TaxCategory.Prefix == "IVA") return sellTax2;

            return null;
        }

        public decimal CalculateIvaValue(TaxGraphQLModel? ivaTax, string pattern)
        {
            if (ivaTax is null || string.IsNullOrEmpty(ivaTax.AlternativeFormula)) return 0;

            FormulaVariables["MARGEN_IMPUESTO"] = ivaTax.Margin;

            string formula = Regex.Replace(ivaTax.AlternativeFormula, pattern, m => FormulaVariables[m.Value].ToString(CultureInfo.InvariantCulture));

            return Convert.ToDecimal(new DataTable().Compute(formula, null), CultureInfo.InvariantCulture);
        }

    }
}
