using Common.Validators;
using FluentAssertions;
using Services.Validators;
using Xunit;

namespace NetErp.Tests.Validators;

/// <summary>
/// Tests puros del validador de límite de crédito — new CreditLimitValidator() + assert. Cero mocks.
/// </summary>
public class CreditLimitValidatorTests
{
    private readonly CreditLimitValidator _validator = new();

    [Fact]
    public void ValidateLimit_WithinBoundsAndUnchanged_ReturnsSuccess()
    {
        ValidationResult result = _validator.ValidateLimit(newLimit: 1_000_000m, currentUsed: 500_000m, originalLimit: 1_000_000m);

        result.IsValid.Should().BeTrue();
        result.Severity.Should().Be(ValidationSeverity.Error.ToString() == "Error" ? default : default); // sanity only
        result.Severity.Should().Be(ValidationSeverity.Error); // enum default value; no warning/info expected on pass
        result.ErrorMessage.Should().BeEmpty();
    }

    [Fact]
    public void ValidateLimit_NewLimitLessThanUsed_ReturnsError()
    {
        ValidationResult result = _validator.ValidateLimit(newLimit: 400_000m, currentUsed: 500_000m, originalLimit: 1_000_000m);

        result.IsValid.Should().BeFalse();
        result.Severity.Should().Be(ValidationSeverity.Error);
        result.ErrorMessage.Should().Contain("utilizado");
    }

    [Fact]
    public void ValidateLimit_NewLimitEqualsUsed_ReturnsSuccess()
    {
        ValidationResult result = _validator.ValidateLimit(newLimit: 500_000m, currentUsed: 500_000m, originalLimit: 500_000m);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateLimit_Negative_ReturnsError()
    {
        // currentUsed en 0 ⇒ no cae en regla 1; cae en regla 2 (negativo)
        ValidationResult result = _validator.ValidateLimit(newLimit: -1m, currentUsed: 0m, originalLimit: 0m);

        result.IsValid.Should().BeFalse();
        result.Severity.Should().Be(ValidationSeverity.Error);
    }

    [Fact]
    public void ValidateLimit_ChangeGreaterThanFiftyPercent_ReturnsWarning()
    {
        // 1M → 2M (aumento 100%)
        ValidationResult result = _validator.ValidateLimit(newLimit: 2_000_000m, currentUsed: 0m, originalLimit: 1_000_000m);

        result.IsValid.Should().BeTrue();
        result.Severity.Should().Be(ValidationSeverity.Warning);
        result.ErrorMessage.Should().Contain("50%");
    }

    [Fact]
    public void ValidateLimit_ChangeExactlyFiftyPercent_ReturnsSuccess()
    {
        // 1M → 1.5M (aumento exactamente 50%) — no activa la regla (>50)
        ValidationResult result = _validator.ValidateLimit(newLimit: 1_500_000m, currentUsed: 0m, originalLimit: 1_000_000m);

        result.IsValid.Should().BeTrue();
        result.Severity.Should().Be(ValidationSeverity.Error); // default enum value
    }

    [Fact]
    public void ValidateLimit_DecreaseMoreThanFiftyPercent_ReturnsWarning()
    {
        // 10M → 1M (caída 90%)
        ValidationResult result = _validator.ValidateLimit(newLimit: 1_000_000m, currentUsed: 0m, originalLimit: 10_000_000m);

        result.IsValid.Should().BeTrue();
        result.Severity.Should().Be(ValidationSeverity.Warning);
    }

    [Fact]
    public void ValidateLimit_OriginalZero_SkipsPercentageRule()
    {
        // originalLimit = 0 → la regla de cambio porcentual no aplica; cualquier valor razonable pasa
        ValidationResult result = _validator.ValidateLimit(newLimit: 500_000m, currentUsed: 0m, originalLimit: 0m);

        result.IsValid.Should().BeTrue();
        result.Severity.Should().NotBe(ValidationSeverity.Warning);
    }

    [Fact]
    public void ValidateLimit_AboveTenMillion_ReturnsWarning()
    {
        // originalLimit en 0 para no activar la regla porcentual; cae sólo en la regla "muy alto"
        ValidationResult result = _validator.ValidateLimit(newLimit: 10_000_001m, currentUsed: 0m, originalLimit: 0m);

        result.IsValid.Should().BeTrue();
        result.Severity.Should().Be(ValidationSeverity.Warning);
        result.ErrorMessage.Should().Contain("muy alto");
    }

    [Fact]
    public void ValidateLimit_AtTenMillion_DoesNotTriggerHighWarning()
    {
        // Boundary exacto: 10_000_000 NO activa (comparación es >)
        ValidationResult result = _validator.ValidateLimit(newLimit: 10_000_000m, currentUsed: 0m, originalLimit: 0m);

        result.IsValid.Should().BeTrue();
        result.Severity.Should().NotBe(ValidationSeverity.Warning);
    }

    [Fact]
    public void ValidateLimit_PercentRulePrecedesHighWarning()
    {
        // 1M → 11M → aumenta >50% (devuelve Warning de porcentaje, no de "muy alto")
        ValidationResult result = _validator.ValidateLimit(newLimit: 11_000_000m, currentUsed: 0m, originalLimit: 1_000_000m);

        result.IsValid.Should().BeTrue();
        result.Severity.Should().Be(ValidationSeverity.Warning);
        result.ErrorMessage.Should().Contain("50%");
        result.ErrorMessage.Should().NotContain("muy alto");
    }
}
