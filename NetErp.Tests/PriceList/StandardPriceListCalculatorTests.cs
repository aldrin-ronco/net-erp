using FluentAssertions;
using Models.Billing;
using Models.Books;
using Models.Inventory;
using NetErp.Billing.PriceList.DTO;
using NetErp.Billing.PriceList.PriceListHelpers;
using Xunit;

namespace NetErp.Tests.PriceList;

/// <summary>
/// Tests del calculador único. Cubren la matriz estrategia (V1/V2) × PriceListIncludeTax × dirección.
///
/// Fórmulas base con IVA ignorado:
///   V1 (margen sobre venta): Price = Cost / (1 - m)
///   V2 (markup):             Price = Cost * (1 + m)
/// Con PriceListIncludeTax=true el resultado se infla por (1 + iva/100).
/// La inversa stripea IVA si corresponde, luego aplica la inversa de V1 o V2.
/// </summary>
public class StandardPriceListCalculatorTests
{
    private const decimal Cost = 100m;
    private const decimal Margin = 30m;
    private const decimal IvaRate = 19m;

    private readonly StandardPriceListCalculator _calculator = new();

    #region ProfitMargin → Price

    [Fact]
    public void ProfitMargin_V1_ExcludeTax_ComputesPreIvaPrice()
    {
        var item = BuildItem(cost: Cost, profitMargin: Margin);
        var pl = BuildPriceList(useAlternative: false, includeTax: false);

        _calculator.RecalculateProductValues(item, nameof(PriceListItemDTO.ProfitMargin), pl);

        // 100 / (1 - 0.30) = 142.857142857...
        item.Price.Should().BeApproximately(142.857m, 0.001m);
    }

    [Fact]
    public void ProfitMargin_V1_IncludeTax_InflatesWithIva()
    {
        var item = BuildItem(cost: Cost, profitMargin: Margin, ivaRate: IvaRate);
        var pl = BuildPriceList(useAlternative: false, includeTax: true);

        _calculator.RecalculateProductValues(item, nameof(PriceListItemDTO.ProfitMargin), pl);

        // (100 / 0.70) * 1.19 = 170.00 (aprox)
        item.Price.Should().BeApproximately(170m, 0.01m);
    }

    [Fact]
    public void ProfitMargin_V2_ExcludeTax_ComputesMarkupPrice()
    {
        var item = BuildItem(cost: Cost, profitMargin: Margin);
        var pl = BuildPriceList(useAlternative: true, includeTax: false);

        _calculator.RecalculateProductValues(item, nameof(PriceListItemDTO.ProfitMargin), pl);

        // 100 * (1 + 0.30) = 130
        item.Price.Should().Be(130m);
    }

    [Fact]
    public void ProfitMargin_V2_IncludeTax_InflatesWithIva()
    {
        var item = BuildItem(cost: Cost, profitMargin: Margin, ivaRate: IvaRate);
        var pl = BuildPriceList(useAlternative: true, includeTax: true);

        _calculator.RecalculateProductValues(item, nameof(PriceListItemDTO.ProfitMargin), pl);

        // 100 * 1.30 * 1.19 = 154.70
        item.Price.Should().BeApproximately(154.7m, 0.01m);
    }

    [Fact]
    public void ProfitMargin_Equals100_LeavesPriceUnchanged()
    {
        var item = BuildItem(cost: 100m, profitMargin: 100m);
        item.Price = 500m;
        var pl = BuildPriceList(useAlternative: false, includeTax: false);

        _calculator.RecalculateProductValues(item, nameof(PriceListItemDTO.ProfitMargin), pl);

        item.Price.Should().Be(500m);
    }

    [Fact]
    public void ProfitMargin_UpdatesMinimumPriceFromDiscount()
    {
        var item = BuildItem(cost: Cost, profitMargin: Margin);
        item.DiscountMargin = 10m;
        var pl = BuildPriceList(useAlternative: false, includeTax: false);

        _calculator.RecalculateProductValues(item, nameof(PriceListItemDTO.ProfitMargin), pl);

        // Price = 142.857, MinimumPrice = 142.857 * 0.9 = 128.571
        item.MinimumPrice.Should().BeApproximately(128.571m, 0.001m);
    }

    [Fact]
    public void ProfitMargin_IsTaxableFalse_IgnoresIvaEvenIfIncludeTaxTrue()
    {
        var item = BuildItem(cost: Cost, profitMargin: Margin, ivaRate: IvaRate);
        var pl = BuildPriceList(useAlternative: false, includeTax: true, isTaxable: false);

        _calculator.RecalculateProductValues(item, nameof(PriceListItemDTO.ProfitMargin), pl);

        // IsTaxable=false → no IVA; V1 sin IVA = 142.857
        item.Price.Should().BeApproximately(142.857m, 0.001m);
    }

    [Fact]
    public void ProfitMargin_NoIvaOnItem_BehavesAsZeroIva()
    {
        var item = BuildItem(cost: Cost, profitMargin: Margin, ivaRate: 0m);
        var pl = BuildPriceList(useAlternative: false, includeTax: true);

        _calculator.RecalculateProductValues(item, nameof(PriceListItemDTO.ProfitMargin), pl);

        // Sin IVA disponible → 142.857
        item.Price.Should().BeApproximately(142.857m, 0.001m);
    }

    [Fact]
    public void ProfitMargin_CostZero_ProducesPriceZero()
    {
        var item = BuildItem(cost: 0m, profitMargin: Margin);
        var pl = BuildPriceList(useAlternative: false, includeTax: false);

        _calculator.RecalculateProductValues(item, nameof(PriceListItemDTO.ProfitMargin), pl);

        item.Price.Should().Be(0m);
    }

    #endregion

    #region Price → ProfitMargin (inversa)

    [Fact]
    public void Price_V1_ExcludeTax_InfersOriginalMargin()
    {
        var item = BuildItem(cost: Cost, profitMargin: 0m);
        item.Price = 142.857m; // valor que V1 sin IVA produciría para margin=30
        var pl = BuildPriceList(useAlternative: false, includeTax: false);

        _calculator.RecalculateProductValues(item, nameof(PriceListItemDTO.Price), pl);

        item.ProfitMargin.Should().BeApproximately(30m, 0.001m);
    }

    [Fact]
    public void Price_V1_IncludeTax_StripesIvaBeforeMarginCalc()
    {
        var item = BuildItem(cost: Cost, profitMargin: 0m, ivaRate: IvaRate);
        item.Price = 170m; // V1 + IVA para cost=100, margin=30
        var pl = BuildPriceList(useAlternative: false, includeTax: true);

        _calculator.RecalculateProductValues(item, nameof(PriceListItemDTO.Price), pl);

        item.ProfitMargin.Should().BeApproximately(30m, 0.01m);
    }

    [Fact]
    public void Price_V2_ExcludeTax_InfersMarkupMargin()
    {
        var item = BuildItem(cost: Cost, profitMargin: 0m);
        item.Price = 130m; // V2 sin IVA para cost=100, margin=30
        var pl = BuildPriceList(useAlternative: true, includeTax: false);

        _calculator.RecalculateProductValues(item, nameof(PriceListItemDTO.Price), pl);

        item.ProfitMargin.Should().BeApproximately(30m, 0.001m);
    }

    [Fact]
    public void Price_V2_IncludeTax_StripesIvaBeforeMarkupCalc()
    {
        var item = BuildItem(cost: Cost, profitMargin: 0m, ivaRate: IvaRate);
        item.Price = 154.7m; // V2 + IVA para cost=100, margin=30
        var pl = BuildPriceList(useAlternative: true, includeTax: true);

        _calculator.RecalculateProductValues(item, nameof(PriceListItemDTO.Price), pl);

        item.ProfitMargin.Should().BeApproximately(30m, 0.01m);
    }

    [Fact]
    public void Price_Zero_NoChange()
    {
        var item = BuildItem(cost: Cost, profitMargin: 0m);
        item.Price = 0m;
        item.ProfitMargin = 25m;
        var pl = BuildPriceList(useAlternative: false, includeTax: false);

        _calculator.RecalculateProductValues(item, nameof(PriceListItemDTO.Price), pl);

        item.ProfitMargin.Should().Be(25m); // no tocó
    }

    [Fact]
    public void Price_V2_CostZero_ProducesMarginZero()
    {
        var item = BuildItem(cost: 0m, profitMargin: 0m);
        item.Price = 100m;
        var pl = BuildPriceList(useAlternative: true, includeTax: false);

        _calculator.RecalculateProductValues(item, nameof(PriceListItemDTO.Price), pl);

        // V2 con cost=0 no puede invertir el markup → margen=0 por convención
        item.ProfitMargin.Should().Be(0m);
    }

    [Fact]
    public void Price_UpdatesMinimumPriceFromDiscount()
    {
        var item = BuildItem(cost: Cost, profitMargin: 0m);
        item.Price = 200m;
        item.DiscountMargin = 15m;
        var pl = BuildPriceList(useAlternative: false, includeTax: false);

        _calculator.RecalculateProductValues(item, nameof(PriceListItemDTO.Price), pl);

        // 200 * (1 - 0.15) = 170
        item.MinimumPrice.Should().Be(170m);
    }

    #endregion

    #region DiscountMargin ↔ MinimumPrice

    [Fact]
    public void DiscountMargin_UpdatesMinimumPrice()
    {
        var item = BuildItem(cost: 0m, profitMargin: 0m);
        item.Price = 100m;
        item.DiscountMargin = 20m;
        var pl = BuildPriceList(useAlternative: false, includeTax: false);

        _calculator.RecalculateProductValues(item, nameof(PriceListItemDTO.DiscountMargin), pl);

        // 100 * (1 - 0.2) = 80
        item.MinimumPrice.Should().Be(80m);
    }

    [Fact]
    public void DiscountMargin_Zero_MinimumEqualsPrice()
    {
        var item = BuildItem(cost: 0m, profitMargin: 0m);
        item.Price = 100m;
        item.DiscountMargin = 0m;
        var pl = BuildPriceList(useAlternative: false, includeTax: false);

        _calculator.RecalculateProductValues(item, nameof(PriceListItemDTO.DiscountMargin), pl);

        item.MinimumPrice.Should().Be(100m);
    }

    [Fact]
    public void MinimumPrice_UpdatesDiscountMargin()
    {
        var item = BuildItem(cost: 0m, profitMargin: 0m);
        item.Price = 100m;
        item.MinimumPrice = 80m;
        var pl = BuildPriceList(useAlternative: false, includeTax: false);

        _calculator.RecalculateProductValues(item, nameof(PriceListItemDTO.MinimumPrice), pl);

        // (1 - 80/100) * 100 = 20
        item.DiscountMargin.Should().Be(20m);
    }

    [Fact]
    public void MinimumPrice_PriceZero_NoChange()
    {
        var item = BuildItem(cost: 0m, profitMargin: 0m);
        item.Price = 0m;
        item.MinimumPrice = 50m;
        item.DiscountMargin = 10m;
        var pl = BuildPriceList(useAlternative: false, includeTax: false);

        _calculator.RecalculateProductValues(item, nameof(PriceListItemDTO.MinimumPrice), pl);

        item.DiscountMargin.Should().Be(10m); // no tocó
    }

    #endregion

    #region Round-trip (consistencia forward/reverse)

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void RoundTrip_MarginThenPrice_RecoversOriginalMargin(bool useAlternative, bool includeTax)
    {
        var item = BuildItem(cost: Cost, profitMargin: Margin, ivaRate: IvaRate);
        var pl = BuildPriceList(useAlternative, includeTax);

        _calculator.RecalculateProductValues(item, nameof(PriceListItemDTO.ProfitMargin), pl);
        decimal computedPrice = item.Price;

        // resetear margen y re-calcular desde el precio
        item.ProfitMargin = 0m;
        item.Price = computedPrice;
        _calculator.RecalculateProductValues(item, nameof(PriceListItemDTO.Price), pl);

        item.ProfitMargin.Should().BeApproximately(Margin, 0.01m);
    }

    #endregion

    #region Helpers

    private static PriceListItemDTO BuildItem(decimal cost, decimal profitMargin, decimal ivaRate = 0m)
    {
        var taxCategory = new TaxCategoryGraphQLModel { Prefix = ivaRate > 0 ? "IVA" : "OTRO" };
        var tax = new TaxGraphQLModel { Rate = ivaRate, TaxCategory = taxCategory };
        var accountingGroup = new AccountingGroupGraphQLModel { SalesPrimaryTax = tax };
        var gqlItem = new ItemGraphQLModel { AccountingGroup = accountingGroup };

        return new PriceListItemDTO
        {
            Item = gqlItem,
            Cost = cost,
            ProfitMargin = profitMargin
        };
    }

    private static PriceListGraphQLModel BuildPriceList(bool useAlternative, bool includeTax, bool isTaxable = true)
        => new()
        {
            UseAlternativeFormula = useAlternative,
            PriceListIncludeTax = includeTax,
            IsTaxable = isTaxable
        };

    #endregion
}
