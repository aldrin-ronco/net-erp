using FluentAssertions;
using NetErp.Treasury.Masters.Validators;
using Xunit;

namespace NetErp.Tests.Validators;

public class BankAccountValidatorTests
{
    private readonly BankAccountValidator _validator = new();

    private static BankAccountValidationContext ValidContext() => new()
    {
        Number = "1234567890",
        AccountingAccountId = 100,
        AccountingAccountSelectExisting = true
    };

    [Fact]
    public void Validate_Number_Empty_ReturnsError()
    {
        _validator.Validate(nameof(BankAccountValidationContext.Number), "", ValidContext())
            .Should().ContainSingle();
    }

    [Fact]
    public void Validate_Number_Whitespace_ReturnsError()
    {
        _validator.Validate(nameof(BankAccountValidationContext.Number), "   ", ValidContext())
            .Should().ContainSingle();
    }

    [Fact]
    public void Validate_Number_Valid_ReturnsEmpty()
    {
        _validator.Validate(nameof(BankAccountValidationContext.Number), "001-123", ValidContext())
            .Should().BeEmpty();
    }

    [Fact]
    public void Validate_AccountingAccount_SelectExisting_IdZero_ReturnsError()
    {
        BankAccountValidationContext ctx = ValidContext();
        ctx.AccountingAccountId = 0;
        _validator.Validate(nameof(BankAccountValidationContext.AccountingAccountId), 0, ctx)
            .Should().ContainSingle();
    }

    [Fact]
    public void Validate_AccountingAccount_AutoCreate_AllowsZero()
    {
        BankAccountValidationContext ctx = ValidContext();
        ctx.AccountingAccountSelectExisting = false;
        ctx.AccountingAccountId = 0;
        _validator.Validate(nameof(BankAccountValidationContext.AccountingAccountId), 0, ctx)
            .Should().BeEmpty();
    }

    [Fact]
    public void ValidateAll_Valid_ReturnsEmpty()
    {
        _validator.ValidateAll(ValidContext()).Should().BeEmpty();
    }

    [Fact]
    public void ValidateAll_EmptyContext_ReturnsErrorForNumber()
    {
        var result = _validator.ValidateAll(new BankAccountValidationContext());
        result.Should().ContainKey(nameof(BankAccountValidationContext.Number));
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
    public void CanSave_EmptyNumber_ReturnsFalse()
    {
        BankAccountValidationContext ctx = ValidContext();
        ctx.Number = "";
        _validator.CanSave(ctx, hasChanges: true, hasErrors: false).Should().BeFalse();
    }

    [Fact]
    public void CanSave_SelectExisting_NoAccount_ReturnsFalse()
    {
        BankAccountValidationContext ctx = ValidContext();
        ctx.AccountingAccountId = 0;
        _validator.CanSave(ctx, hasChanges: true, hasErrors: false).Should().BeFalse();
    }

    [Fact]
    public void CanSave_AutoCreate_NoAccountRequired()
    {
        BankAccountValidationContext ctx = ValidContext();
        ctx.AccountingAccountId = 0;
        ctx.AccountingAccountSelectExisting = false;
        _validator.CanSave(ctx, hasChanges: true, hasErrors: false).Should().BeTrue();
    }
}
