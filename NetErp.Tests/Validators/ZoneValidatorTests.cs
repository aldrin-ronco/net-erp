using FluentAssertions;
using NetErp.Billing.Zones.Validators;
using Xunit;

namespace NetErp.Tests.Validators;

/// <summary>
/// Tests puros del validador — new ZoneValidator() + assert. Cero mocks.
/// </summary>
public class ZoneValidatorTests
{
    private readonly ZoneValidator _validator = new();

    #region Validate

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
        var errors = _validator.Validate("Name", "Norte");

        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_Name_Null_ReturnsError()
    {
        var errors = _validator.Validate("Name", null);

        errors.Should().ContainSingle();
    }

    [Fact]
    public void Validate_UnknownProperty_ReturnsEmpty()
    {
        var errors = _validator.Validate("Unknown", "");

        errors.Should().BeEmpty();
    }

    #endregion

    #region ValidateAll

    [Fact]
    public void ValidateAll_EmptyName_ReturnsError()
    {
        var result = _validator.ValidateAll("");

        result.Should().ContainKey("Name");
    }

    [Fact]
    public void ValidateAll_ValidName_ReturnsEmpty()
    {
        var result = _validator.ValidateAll("Norte");

        result.Should().BeEmpty();
    }

    #endregion

    #region CanSave

    [Fact]
    public void CanSave_ValidName_HasChanges_NoErrors_ReturnsTrue()
    {
        _validator.CanSave("Norte", hasChanges: true, hasErrors: false)
            .Should().BeTrue();
    }

    [Fact]
    public void CanSave_EmptyName_ReturnsFalse()
    {
        _validator.CanSave("", hasChanges: true, hasErrors: false)
            .Should().BeFalse();
    }

    [Fact]
    public void CanSave_WhitespaceOnlyName_ReturnsFalse()
    {
        _validator.CanSave("   ", hasChanges: true, hasErrors: false)
            .Should().BeFalse();
    }

    [Fact]
    public void CanSave_NullName_ReturnsFalse()
    {
        _validator.CanSave(null, hasChanges: true, hasErrors: false)
            .Should().BeFalse();
    }

    [Fact]
    public void CanSave_NoChanges_ReturnsFalse()
    {
        _validator.CanSave("Norte", hasChanges: false, hasErrors: false)
            .Should().BeFalse();
    }

    [Fact]
    public void CanSave_HasErrors_ReturnsFalse()
    {
        _validator.CanSave("Norte", hasChanges: true, hasErrors: true)
            .Should().BeFalse();
    }

    #endregion
}
