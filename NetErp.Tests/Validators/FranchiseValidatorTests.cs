using FluentAssertions;
using NetErp.Treasury.Masters.Validators;
using Xunit;

namespace NetErp.Tests.Validators;

public class FranchiseValidatorTests
{
    private readonly FranchiseValidator _validator = new();

    private static FranchiseValidationContext ValidContext() => new()
    {
        Name = "Visa",
        CommissionAccountingAccountId = 100,
        BankAccountId = 200
    };

    [Fact]
    public void Validate_Name_Empty_ReturnsError()
    {
        _validator.Validate(nameof(FranchiseValidationContext.Name), "", ValidContext())
            .Should().ContainSingle();
    }

    [Fact]
    public void Validate_Name_Whitespace_ReturnsError()
    {
        _validator.Validate(nameof(FranchiseValidationContext.Name), "   ", ValidContext())
            .Should().ContainSingle();
    }

    [Fact]
    public void Validate_Name_Valid_ReturnsEmpty()
    {
        _validator.Validate(nameof(FranchiseValidationContext.Name), "Visa", ValidContext())
            .Should().BeEmpty();
    }

    [Fact]
    public void Validate_CommissionAccount_Zero_ReturnsError()
    {
        FranchiseValidationContext ctx = ValidContext();
        ctx.CommissionAccountingAccountId = 0;
        _validator.Validate(nameof(FranchiseValidationContext.CommissionAccountingAccountId), 0, ctx)
            .Should().ContainSingle();
    }

    [Fact]
    public void Validate_BankAccount_Zero_ReturnsError()
    {
        FranchiseValidationContext ctx = ValidContext();
        ctx.BankAccountId = 0;
        _validator.Validate(nameof(FranchiseValidationContext.BankAccountId), 0, ctx)
            .Should().ContainSingle();
    }

    [Fact]
    public void ValidateAll_ValidContext_Empty()
    {
        _validator.ValidateAll(ValidContext()).Should().BeEmpty();
    }

    [Fact]
    public void ValidateAll_EmptyContext_ReturnsAllErrors()
    {
        var result = _validator.ValidateAll(new FranchiseValidationContext());
        result.Should().ContainKeys(
            nameof(FranchiseValidationContext.Name),
            nameof(FranchiseValidationContext.CommissionAccountingAccountId),
            nameof(FranchiseValidationContext.BankAccountId));
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
    public void CanSave_MissingName_ReturnsFalse()
    {
        FranchiseValidationContext ctx = ValidContext();
        ctx.Name = "";
        _validator.CanSave(ctx, hasChanges: true, hasErrors: false).Should().BeFalse();
    }

    [Fact]
    public void CanSave_MissingCommissionAccount_ReturnsFalse()
    {
        FranchiseValidationContext ctx = ValidContext();
        ctx.CommissionAccountingAccountId = 0;
        _validator.CanSave(ctx, hasChanges: true, hasErrors: false).Should().BeFalse();
    }

    [Fact]
    public void CanSave_MissingBankAccount_ReturnsFalse()
    {
        FranchiseValidationContext ctx = ValidContext();
        ctx.BankAccountId = 0;
        _validator.CanSave(ctx, hasChanges: true, hasErrors: false).Should().BeFalse();
    }
}
