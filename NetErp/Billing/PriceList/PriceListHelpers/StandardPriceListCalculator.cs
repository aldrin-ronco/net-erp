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

        public void RecalculateProductValues(PriceListDetailDTO priceListDetail, string modifiedProperty, PriceListGraphQLModel priceList)
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
                case nameof(PriceListDetailDTO.ProfitMargin):
                    // Cálculos específicos cuando se modifica el margen de beneficio
                    CalculateFromProfitMargin(priceListDetail, priceList);
                    break;
                case nameof(PriceListDetailDTO.Price):
                    // Cálculos específicos cuando se modifica el precio
                    CalculateFromPrice(priceListDetail, priceList);
                    break;
                case nameof(PriceListDetailDTO.DiscountMargin):
                    // Cálculos específicos cuando se modifica el margen de descuento
                    CalculateFromDiscountMargin(priceListDetail);
                    break;
                case nameof(PriceListDetailDTO.MinimumPrice):
                    // Cálculos específicos cuando se modifica el precio mínimo
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
            priceListDetail.UpdatePropertySilently(nameof(PriceListDetailDTO.ProfitMargin), profitMargin);
            priceListDetail.UpdatePropertySilently(nameof(PriceListDetailDTO.Profit), profit);
            priceListDetail.UpdatePropertySilently(nameof(PriceListDetailDTO.MinimumPrice), priceWithDiscount);
        }

        public void CalculateFromProfitMargin(PriceListDetailDTO priceListDetail, PriceListGraphQLModel priceList)
        {
            decimal priceWithoutDiscount = (priceListDetail.Cost / (1 - (priceListDetail.ProfitMargin / 100)));
            FormulaVariables["PRECIO_SIN_DCTO"] = priceWithoutDiscount;

            string pattern = string.Join("|", FormulaVariables.Keys.Select(Regex.Escape));
            decimal ivaValue = priceList.IsTaxable && priceList.PriceListIncludeTax ? CalculateIvaValue(GetIvaTax(priceListDetail), pattern) : 0;
            priceWithoutDiscount += ivaValue;

            priceListDetail.UpdatePropertySilently(nameof(PriceListDetailDTO.Price), priceWithoutDiscount);
            decimal discountValue = priceWithoutDiscount * (priceListDetail.DiscountMargin / 100);
            decimal priceWithDiscount = priceWithoutDiscount - discountValue;
            decimal profit = priceWithoutDiscount - priceListDetail.Cost - ivaValue;
            priceListDetail.UpdatePropertySilently(nameof(PriceListDetailDTO.MinimumPrice), priceWithDiscount);
            priceListDetail.UpdatePropertySilently(nameof(PriceListDetailDTO.Profit), profit);
        }

        public decimal ExtractIvaMargin(PriceListDetailDTO priceListDetail, PriceListGraphQLModel priceList)
        {
            if (!priceList.IsTaxable || !priceList.PriceListIncludeTax) return 0;

            TaxGraphQLModel sellTax1 = priceListDetail.CatalogItem.AccountingGroup.SellTax1;
            TaxGraphQLModel sellTax2 = priceListDetail.CatalogItem.AccountingGroup.SellTax2;

            if (sellTax1 != null && sellTax1.TaxCategory != null && sellTax1.TaxCategory.Prefix == "IVA") return sellTax1.Margin;
            if (sellTax2 != null && sellTax2.TaxCategory != null && sellTax2.TaxCategory.Prefix == "IVA") return sellTax2.Margin;

            return 0;
        }

        public TaxGraphQLModel? GetIvaTax(PriceListDetailDTO priceListDetail)
        {
            TaxGraphQLModel? sellTax1 = priceListDetail.CatalogItem.AccountingGroup.SellTax1;
            TaxGraphQLModel? sellTax2 = priceListDetail.CatalogItem.AccountingGroup.SellTax2;

            if (sellTax1 != null && sellTax1.TaxCategory != null && sellTax1.TaxCategory.Prefix == "IVA") return sellTax1;
            if (sellTax2 != null && sellTax2.TaxCategory != null && sellTax2.TaxCategory.Prefix == "IVA") return sellTax2;

            return null;
        }

        public decimal CalculateIvaValue(TaxGraphQLModel? ivaTax, string pattern)
        {
            if (ivaTax is null || string.IsNullOrEmpty(ivaTax.Formula)) return 0;

            FormulaVariables["MARGEN_IMPUESTO"] = ivaTax.Margin;

            string formula = Regex.Replace(ivaTax.Formula, pattern, m => FormulaVariables[m.Value].ToString(CultureInfo.InvariantCulture));

            return Convert.ToDecimal(new DataTable().Compute(formula, null), CultureInfo.InvariantCulture);
        }
    }
}
