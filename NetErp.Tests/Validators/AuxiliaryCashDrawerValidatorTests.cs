using FluentAssertions;
using NetErp.Treasury.Masters.Validators;
using Xunit;

namespace NetErp.Tests.Validators;

public class AuxiliaryCashDrawerValidatorTests
{
    private readonly AuxiliaryCashDrawerValidator _validator = new();

    private static AuxiliaryCashDrawerValidationContext ValidContext() => new()
    {
        Name = "Caja Auxiliar",
        ComputerName = "PC-01",
        AutoTransfer = false,
        AutoTransferCashDrawerId = 0
    };

    [Fact]
    public void Validate_Name_Empty_ReturnsError()
    {
        _validator.Validate(nameof(AuxiliaryCashDrawerValidationContext.Name), "", ValidContext())
            .Should().ContainSingle();
    }

    [Fact]
    public void Validate_Name_Valid_ReturnsEmpty()
    {
        _validator.Validate(nameof(AuxiliaryCashDrawerValidationContext.Name), "Caja Auxiliar", ValidContext())
            .Should().BeEmpty();
    }

    [Fact]
    public void Validate_ComputerName_Empty_ReturnsError()
    {
        _validator.Validate(nameof(AuxiliaryCashDrawerValidationContext.ComputerName), "", ValidContext())
            .Should().ContainSingle();
    }

    [Fact]
    public void Validate_ComputerName_Valid_ReturnsEmpty()
    {
        _validator.Validate(nameof(AuxiliaryCashDrawerValidationContext.ComputerName), "PC-01", ValidContext())
            .Should().BeEmpty();
    }

    [Fact]
    public void Validate_AutoTransfer_Active_NoTarget_ReturnsError()
    {
        AuxiliaryCashDrawerValidationContext ctx = ValidContext();
        ctx.AutoTransfer = true;
        ctx.AutoTransferCashDrawerId = 0;
        _validator.Validate(nameof(AuxiliaryCashDrawerValidationContext.AutoTransferCashDrawerId), 0, ctx)
            .Should().ContainSingle();
    }

    [Fact]
    public void Validate_AutoTransfer_Inactive_NoTarget_NoError()
    {
        AuxiliaryCashDrawerValidationContext ctx = ValidContext();
        ctx.AutoTransfer = false;
        _validator.Validate(nameof(AuxiliaryCashDrawerValidationContext.AutoTransferCashDrawerId), 0, ctx)
            .Should().BeEmpty();
    }

    [Fact]
    public void ValidateAll_Valid_ReturnsEmpty()
    {
        _validator.ValidateAll(ValidContext()).Should().BeEmpty();
    }

    [Fact]
    public void ValidateAll_Empty_ReturnsErrors()
    {
        var result = _validator.ValidateAll(new AuxiliaryCashDrawerValidationContext { AutoTransfer = true });
        result.Should().ContainKeys(
            nameof(AuxiliaryCashDrawerValidationContext.Name),
            nameof(AuxiliaryCashDrawerValidationContext.ComputerName),
            nameof(AuxiliaryCashDrawerValidationContext.AutoTransferCashDrawerId));
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
        AuxiliaryCashDrawerValidationContext ctx = ValidContext();
        ctx.Name = "";
        _validator.CanSave(ctx, hasChanges: true, hasErrors: false).Should().BeFalse();
    }

    [Fact]
    public void CanSave_EmptyComputerName_ReturnsFalse()
    {
        AuxiliaryCashDrawerValidationContext ctx = ValidContext();
        ctx.ComputerName = "";
        _validator.CanSave(ctx, hasChanges: true, hasErrors: false).Should().BeFalse();
    }

    [Fact]
    public void CanSave_AutoTransferWithoutTarget_ReturnsFalse()
    {
        AuxiliaryCashDrawerValidationContext ctx = ValidContext();
        ctx.AutoTransfer = true;
        _validator.CanSave(ctx, hasChanges: true, hasErrors: false).Should().BeFalse();
    }

    [Fact]
    public void CanSave_AutoTransferWithTarget_ReturnsTrue()
    {
        AuxiliaryCashDrawerValidationContext ctx = ValidContext();
        ctx.AutoTransfer = true;
        ctx.AutoTransferCashDrawerId = 99;
        _validator.CanSave(ctx, hasChanges: true, hasErrors: false).Should().BeTrue();
    }
}
