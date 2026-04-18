using FluentAssertions;
using NetErp.Treasury.Masters.Validators;
using Xunit;

namespace NetErp.Tests.Validators;

public class BankValidatorTests
{
    private readonly BankValidator _validator = new();

    private static BankValidationContext ValidContext() => new()
    {
        Code = "001",
        PaymentMethodPrefix = "Z",
        AccountingEntityName = "Bancolombia",
        AccountingEntityId = 42
    };

    #region Code

    [Fact]
    public void Validate_Code_Empty_ReturnsError()
    {
        var errors = _validator.Validate(nameof(BankValidationContext.Code), "", ValidContext());
        errors.Should().ContainSingle().Which.Should().Contain("obligatorio");
    }

    [Fact]
    public void Validate_Code_WrongLength_ReturnsError()
    {
        _validator.Validate(nameof(BankValidationContext.Code), "12", ValidContext()).Should().ContainSingle();
        _validator.Validate(nameof(BankValidationContext.Code), "1234", ValidContext()).Should().ContainSingle();
    }

    [Fact]
    public void Validate_Code_ExactlyThree_NoError()
    {
        _validator.Validate(nameof(BankValidationContext.Code), "007", ValidContext()).Should().BeEmpty();
    }

    #endregion

    #region PaymentMethodPrefix

    [Fact]
    public void Validate_Prefix_Empty_ReturnsError()
    {
        _validator.Validate(nameof(BankValidationContext.PaymentMethodPrefix), "", ValidContext())
            .Should().ContainSingle();
    }

    [Fact]
    public void Validate_Prefix_MoreThanOneChar_ReturnsError()
    {
        _validator.Validate(nameof(BankValidationContext.PaymentMethodPrefix), "ZX", ValidContext())
            .Should().ContainSingle();
    }

    [Fact]
    public void Validate_Prefix_ExactlyOne_NoError()
    {
        _validator.Validate(nameof(BankValidationContext.PaymentMethodPrefix), "Z", ValidContext())
            .Should().BeEmpty();
    }

    #endregion

    #region AccountingEntity

    [Fact]
    public void Validate_AccountingEntity_NameEmpty_ReturnsError()
    {
        BankValidationContext ctx = ValidContext();
        ctx.AccountingEntityName = "";
        _validator.Validate(nameof(BankValidationContext.AccountingEntityName), "", ctx).Should().ContainSingle();
    }

    [Fact]
    public void Validate_AccountingEntity_IdZero_ReturnsError()
    {
        BankValidationContext ctx = ValidContext();
        ctx.AccountingEntityId = 0;
        _validator.Validate(nameof(BankValidationContext.AccountingEntityName), ctx.AccountingEntityName, ctx)
            .Should().ContainSingle();
    }

    #endregion

    #region ValidateAll

    [Fact]
    public void ValidateAll_ValidContext_ReturnsEmptyDict()
    {
        _validator.ValidateAll(ValidContext()).Should().BeEmpty();
    }

    [Fact]
    public void ValidateAll_InvalidContext_ReturnsAllErrors()
    {
        BankValidationContext ctx = new();
        var result = _validator.ValidateAll(ctx);
        result.Should().ContainKeys(
            nameof(BankValidationContext.Code),
            nameof(BankValidationContext.PaymentMethodPrefix),
            nameof(BankValidationContext.AccountingEntityName));
    }

    #endregion

    #region CanSave

    [Fact]
    public void CanSave_ValidContextWithChangesNoErrors_ReturnsTrue()
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
    public void CanSave_MissingAccountingEntity_ReturnsFalse()
    {
        BankValidationContext ctx = ValidContext();
        ctx.AccountingEntityId = 0;
        _validator.CanSave(ctx, hasChanges: true, hasErrors: false).Should().BeFalse();
    }

    [Fact]
    public void CanSave_InvalidCode_ReturnsFalse()
    {
        BankValidationContext ctx = ValidContext();
        ctx.Code = "AB";
        _validator.CanSave(ctx, hasChanges: true, hasErrors: false).Should().BeFalse();
    }

    [Fact]
    public void CanSave_InvalidPrefix_ReturnsFalse()
    {
        BankValidationContext ctx = ValidContext();
        ctx.PaymentMethodPrefix = "";
        _validator.CanSave(ctx, hasChanges: true, hasErrors: false).Should().BeFalse();
    }

    #endregion
}
