using FluentAssertions;
using NetErp.Inventory.MeasurementUnits.Validators;
using Xunit;

namespace NetErp.Tests.Validators;

/// <summary>
/// Tests puros del validador — new MeasurementUnitValidator() + assert. Cero mocks.
/// </summary>
public class MeasurementUnitValidatorTests
{
    private readonly MeasurementUnitValidator _validator = new();

    #region Validate (single property)

    [Fact]
    public void Validate_Name_Empty_ReturnsError()
    {
        var errors = _validator.Validate("Name", "");

        errors.Should().ContainSingle()
              .Which.Should().Contain("nombre");
    }

    [Fact]
    public void Validate_Name_Valid_ReturnsEmpty()
    {
        var errors = _validator.Validate("Name", "Kilogramo");

        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_Abbreviation_Empty_ReturnsError()
    {
        var errors = _validator.Validate("Abbreviation", "");

        errors.Should().ContainSingle()
              .Which.Should().Contain("abreviación");
    }

    [Fact]
    public void Validate_Type_Empty_ReturnsError()
    {
        var errors = _validator.Validate("Type", "");

        errors.Should().ContainSingle()
              .Which.Should().Contain("tipo");
    }

    [Fact]
    public void Validate_DianCode_Empty_ReturnsError()
    {
        var errors = _validator.Validate("DianCode", "");

        errors.Should().ContainSingle()
              .Which.Should().Contain("DIAN");
    }

    [Fact]
    public void Validate_UnknownProperty_ReturnsEmpty()
    {
        var errors = _validator.Validate("NonExistent", "");

        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_NullValue_ReturnsError()
    {
        var errors = _validator.Validate("Name", null);

        errors.Should().ContainSingle();
    }

    #endregion

    #region ValidateAll

    [Fact]
    public void ValidateAll_AllEmpty_Returns4Errors()
    {
        var result = _validator.ValidateAll("", "", "", "");

        result.Should().HaveCount(4);
        result.Should().ContainKey("Name");
        result.Should().ContainKey("Abbreviation");
        result.Should().ContainKey("Type");
        result.Should().ContainKey("DianCode");
    }

    [Fact]
    public void ValidateAll_AllValid_ReturnsEmpty()
    {
        var result = _validator.ValidateAll("Kilogramo", "Kg", "Peso", "001");

        result.Should().BeEmpty();
    }

    [Fact]
    public void ValidateAll_PartiallyValid_ReturnsOnlyInvalid()
    {
        var result = _validator.ValidateAll("Kilogramo", "", "Peso", "");

        result.Should().HaveCount(2);
        result.Should().ContainKey("Abbreviation");
        result.Should().ContainKey("DianCode");
        result.Should().NotContainKey("Name");
        result.Should().NotContainKey("Type");
    }

    #endregion

    #region CanSave

    [Fact]
    public void CanSave_AllValid_HasChanges_NoErrors_ReturnsTrue()
    {
        var result = _validator.CanSave("Kilogramo", "Kg", "Peso", "001",
                                         hasChanges: true, hasErrors: false);

        result.Should().BeTrue();
    }

    [Fact]
    public void CanSave_EmptyName_ReturnsFalse()
    {
        var result = _validator.CanSave("", "Kg", "Peso", "001",
                                         hasChanges: true, hasErrors: false);

        result.Should().BeFalse();
    }

    [Fact]
    public void CanSave_EmptyAbbreviation_ReturnsFalse()
    {
        var result = _validator.CanSave("Kilogramo", "", "Peso", "001",
                                         hasChanges: true, hasErrors: false);

        result.Should().BeFalse();
    }

    [Fact]
    public void CanSave_NoChanges_ReturnsFalse()
    {
        var result = _validator.CanSave("Kilogramo", "Kg", "Peso", "001",
                                         hasChanges: false, hasErrors: false);

        result.Should().BeFalse();
    }

    [Fact]
    public void CanSave_HasErrors_ReturnsFalse()
    {
        var result = _validator.CanSave("Kilogramo", "Kg", "Peso", "001",
                                         hasChanges: true, hasErrors: true);

        result.Should().BeFalse();
    }

    [Fact]
    public void CanSave_NullName_ReturnsFalse()
    {
        var result = _validator.CanSave(null, "Kg", "Peso", "001",
                                         hasChanges: true, hasErrors: false);

        result.Should().BeFalse();
    }

    #endregion
}
