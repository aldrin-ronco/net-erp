using FluentAssertions;
using NetErp.Treasury.Masters.Validators;
using Xunit;

namespace NetErp.Tests.Validators;

public class MajorCashDrawerValidatorTests
{
    private readonly MajorCashDrawerValidator _validator = new();

    private static MajorCashDrawerValidationContext ValidContext() => new()
    {
        Name = "Caja Principal",
        AutoTransfer = false,
        AutoTransferCashDrawerId = 0
    };

    [Fact]
    public void Validate_Name_Empty_ReturnsError()
    {
        _validator.Validate(nameof(MajorCashDrawerValidationContext.Name), "", ValidContext())
            .Should().ContainSingle();
    }

    [Fact]
    public void Validate_Name_Whitespace_ReturnsError()
    {
        _validator.Validate(nameof(MajorCashDrawerValidationContext.Name), "   ", ValidContext())
            .Should().ContainSingle();
    }

    [Fact]
    public void Validate_Name_Valid_ReturnsEmpty()
    {
        _validator.Validate(nameof(MajorCashDrawerValidationContext.Name), "Caja Principal", ValidContext())
            .Should().BeEmpty();
    }

    [Fact]
    public void Validate_AutoTransfer_Active_NoTarget_ReturnsError()
    {
        MajorCashDrawerValidationContext ctx = ValidContext();
        ctx.AutoTransfer = true;
        ctx.AutoTransferCashDrawerId = 0;
        _validator.Validate(nameof(MajorCashDrawerValidationContext.AutoTransferCashDrawerId), 0, ctx)
            .Should().ContainSingle();
    }

    [Fact]
    public void Validate_AutoTransfer_Inactive_NoTarget_NoError()
    {
        MajorCashDrawerValidationContext ctx = ValidContext();
        ctx.AutoTransfer = false;
        ctx.AutoTransferCashDrawerId = 0;
        _validator.Validate(nameof(MajorCashDrawerValidationContext.AutoTransferCashDrawerId), 0, ctx)
            .Should().BeEmpty();
    }

    [Fact]
    public void ValidateAll_Valid_ReturnsEmpty()
    {
        _validator.ValidateAll(ValidContext()).Should().BeEmpty();
    }

    [Fact]
    public void ValidateAll_Invalid_ReturnsErrors()
    {
        var result = _validator.ValidateAll(new MajorCashDrawerValidationContext { AutoTransfer = true });
        result.Should().ContainKeys(
            nameof(MajorCashDrawerValidationContext.Name),
            nameof(MajorCashDrawerValidationContext.AutoTransferCashDrawerId));
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
        MajorCashDrawerValidationContext ctx = ValidContext();
        ctx.Name = "";
        _validator.CanSave(ctx, hasChanges: true, hasErrors: false).Should().BeFalse();
    }

    [Fact]
    public void CanSave_AutoTransferWithoutTarget_ReturnsFalse()
    {
        MajorCashDrawerValidationContext ctx = ValidContext();
        ctx.AutoTransfer = true;
        ctx.AutoTransferCashDrawerId = 0;
        _validator.CanSave(ctx, hasChanges: true, hasErrors: false).Should().BeFalse();
    }

    [Fact]
    public void CanSave_AutoTransferWithTarget_ReturnsTrue()
    {
        MajorCashDrawerValidationContext ctx = ValidContext();
        ctx.AutoTransfer = true;
        ctx.AutoTransferCashDrawerId = 99;
        _validator.CanSave(ctx, hasChanges: true, hasErrors: false).Should().BeTrue();
    }
}
