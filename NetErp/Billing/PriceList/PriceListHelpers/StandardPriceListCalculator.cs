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
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace NetErp.Billing.PriceList.PriceListHelpers
{
    public class StandardPriceListCalculator: IPriceListCalculator
    {
        public Dictionary<string, decimal> FormulaVariables { get; private set; }

        public StandardPriceListCalculator()
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

        public void RecalculateProductValues(PriceListItemDTO priceListDetail, string modifiedProperty, PriceListGraphQLModel priceList)
        {
            FormulaVariables = new()
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
                case nameof(PriceListItemDTO.ProfitMargin):
                    // Cálculos específicos cuando se modifica el margen de beneficio
                    CalculateFromProfitMargin(priceListDetail, priceList);
                    break;
                case nameof(PriceListItemDTO.Price):
                    // Cálculos específicos cuando se modifica el precio
                    CalculateFromPrice(priceListDetail, priceList);
                    break;
                case nameof(PriceListItemDTO.DiscountMargin):
                    // Cálculos específicos cuando se modifica el margen de descuento
                    CalculateFromDiscountMargin(priceListDetail);
                    break;
                case nameof(PriceListItemDTO.MinimumPrice):
                    // Cálculos específicos cuando se modifica el precio mínimo
                    CalculateFromMinimumPrice(priceListDetail);
                    break;
            }
        }

        public void CalculateFromDiscountMargin(PriceListItemDTO priceListDetail)
        {
            decimal discountValue = priceListDetail.Price * (priceListDetail.DiscountMargin / 100);
            decimal priceWithDiscount = priceListDetail.Price - discountValue;
            priceListDetail.UpdatePropertySilently(nameof(PriceListItemDTO.MinimumPrice), priceWithDiscount);
        }

        public void CalculateFromMinimumPrice(PriceListItemDTO priceListDetail)
        {
            if (priceListDetail.Price == 0) return;
            decimal discountMargin = (1 - priceListDetail.MinimumPrice / priceListDetail.Price) * 100;
            priceListDetail.UpdatePropertySilently(nameof(PriceListItemDTO.DiscountMargin), discountMargin);
        }

        public void CalculateFromPrice(PriceListItemDTO priceListDetail, PriceListGraphQLModel priceList)
        {
            decimal priceWithoutTax = priceListDetail.Price;
            decimal discountValue = (priceWithoutTax * (priceListDetail.DiscountMargin / 100));
            decimal priceWithDiscount = priceWithoutTax - discountValue;
            if (priceList.IsTaxable)
            {
                if (priceList.PriceListIncludeTax)
                {
                    decimal taxMargin = ExtractIvaMargin(priceListDetail, priceList);
                    priceWithoutTax = priceListDetail.Price / (1 + (taxMargin / 100));
                }
            }

            decimal profitMargin = (1 - (priceListDetail.Cost / priceWithoutTax)) * 100;
            decimal profit = priceWithoutTax - priceListDetail.Cost;
            priceListDetail.UpdatePropertySilently(nameof(PriceListItemDTO.ProfitMargin), profitMargin);
            priceListDetail.UpdatePropertySilently(nameof(PriceListItemDTO.MinimumPrice), priceWithDiscount);
        }

        public void CalculateFromProfitMargin(PriceListItemDTO priceListDetail, PriceListGraphQLModel priceList)
        {
            if (priceListDetail.ProfitMargin == 100m) return;
            decimal priceWithoutDiscount = (priceListDetail.Cost / (1 - (priceListDetail.ProfitMargin / 100)));
            FormulaVariables["PRECIO_SIN_DCTO"] = priceWithoutDiscount;

            string pattern = string.Join("|", FormulaVariables.Keys.Select(Regex.Escape));
            decimal ivaValue = priceList.IsTaxable && priceList.PriceListIncludeTax ? CalculateIvaValue(GetIvaTax(priceListDetail), pattern) : 0;
            priceWithoutDiscount += ivaValue;

            priceListDetail.UpdatePropertySilently(nameof(PriceListItemDTO.Price), priceWithoutDiscount);
            decimal discountValue = priceWithoutDiscount * (priceListDetail.DiscountMargin / 100);
            decimal priceWithDiscount = priceWithoutDiscount - discountValue;
            decimal profit = priceWithoutDiscount - priceListDetail.Cost - ivaValue;
            priceListDetail.UpdatePropertySilently(nameof(PriceListItemDTO.MinimumPrice), priceWithDiscount);
        }

        public decimal ExtractIvaMargin(PriceListItemDTO priceListDetail, PriceListGraphQLModel priceList)
        {
            if (!priceList.IsTaxable || !priceList.PriceListIncludeTax) return 0;
            if (priceListDetail.Item?.AccountingGroup is null) return 0;

            TaxGraphQLModel sellTax1 = priceListDetail.Item.AccountingGroup.SalesPrimaryTax;
            TaxGraphQLModel sellTax2 = priceListDetail.Item.AccountingGroup.SalesSecondaryTax;

            if (sellTax1 != null && sellTax1.TaxCategory != null && sellTax1.TaxCategory.Prefix == "IVA") return sellTax1.Rate;
            if (sellTax2 != null && sellTax2.TaxCategory != null && sellTax2.TaxCategory.Prefix == "IVA") return sellTax2.Rate;

            return 0;
        }

        public TaxGraphQLModel? GetIvaTax(PriceListItemDTO priceListDetail)
        {
            if (priceListDetail.Item?.AccountingGroup is null) return null;

            TaxGraphQLModel? sellTax1 = priceListDetail.Item.AccountingGroup.SalesPrimaryTax;
            TaxGraphQLModel? sellTax2 = priceListDetail.Item.AccountingGroup.SalesSecondaryTax;

            if (sellTax1 != null && sellTax1.TaxCategory != null && sellTax1.TaxCategory.Prefix == "IVA") return sellTax1;
            if (sellTax2 != null && sellTax2.TaxCategory != null && sellTax2.TaxCategory.Prefix == "IVA") return sellTax2;

            return null;
        }

        public decimal CalculateIvaValue(TaxGraphQLModel? ivaTax, string pattern)
        {
            if (ivaTax is null || string.IsNullOrEmpty(ivaTax.Formula)) return 0;

            FormulaVariables["MARGEN_IMPUESTO"] = ivaTax.Rate;

            string formula = Regex.Replace(ivaTax.Formula, pattern, m => FormulaVariables[m.Value].ToString(CultureInfo.InvariantCulture));

            using var dt = new DataTable();
            return Convert.ToDecimal(dt.Compute(formula, null), CultureInfo.InvariantCulture);
        }
    }
}
