using FluentAssertions;
using NetErp.Treasury.Masters.Validators;
using Xunit;

namespace NetErp.Tests.Validators;

public class MinorCashDrawerValidatorTests
{
    private readonly MinorCashDrawerValidator _validator = new();

    private static MinorCashDrawerValidationContext ValidContext() => new()
    {
        Name = "Caja Menor Recepción"
    };

    [Fact]
    public void Validate_Name_Empty_ReturnsError()
    {
        _validator.Validate(nameof(MinorCashDrawerValidationContext.Name), "", ValidContext())
            .Should().ContainSingle();
    }

    [Fact]
    public void Validate_Name_Whitespace_ReturnsError()
    {
        _validator.Validate(nameof(MinorCashDrawerValidationContext.Name), "   ", ValidContext())
            .Should().ContainSingle();
    }

    [Fact]
    public void Validate_Name_Valid_ReturnsEmpty()
    {
        _validator.Validate(nameof(MinorCashDrawerValidationContext.Name), "Caja Menor", ValidContext())
            .Should().BeEmpty();
    }

    [Fact]
    public void Validate_UnknownProperty_ReturnsEmpty()
    {
        _validator.Validate("Unknown", "value", ValidContext()).Should().BeEmpty();
    }

    [Fact]
    public void ValidateAll_Valid_ReturnsEmpty()
    {
        _validator.ValidateAll(ValidContext()).Should().BeEmpty();
    }

    [Fact]
    public void ValidateAll_EmptyContext_ReturnsErrorForName()
    {
        var result = _validator.ValidateAll(new MinorCashDrawerValidationContext());
        result.Should().ContainKey(nameof(MinorCashDrawerValidationContext.Name));
    }

    [Fact]
    public void CanSave_Valid_ReturnsTrue()
    {
        _validator.CanSave(ValidContext(), hasChanges: true, hasErrors: false).Should().BeTrue();
    }

    [Fact]
    public void CanSave_HasErrors_ReturnsFalse()
    {
        _validator.CanSave(ValidContext(), hasChanges: true, hasErrors: true).Should().BeFalse();
    }

    [Fact]
    public void CanSave_NoChanges_ReturnsFalse()
    {
        _validator.CanSave(ValidContext(), hasChanges: false, hasErrors: false).Should().BeFalse();
    }

    [Fact]
    public void CanSave_EmptyName_ReturnsFalse()
    {
        MinorCashDrawerValidationContext ctx = ValidContext();
        ctx.Name = "";
        _validator.CanSave(ctx, hasChanges: true, hasErrors: false).Should().BeFalse();
    }
}
