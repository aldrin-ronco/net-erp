using FluentAssertions;
using NetErp.Global.CostCenters.Validators;
using Xunit;

namespace NetErp.Tests.Validators;

/// <summary>
/// Tests puros del validador — new CompanyValidator() + assert. Cero mocks.
/// </summary>
public class CompanyValidatorTests
{
    private readonly CompanyValidator _validator = new();

    private static CompanyValidationContext Context(int id = 0) => new()
    {
        AccountingEntityCompanyId = id
    };

    #region Validate — AccountingEntityCompanyId

    [Fact]
    public void Validate_AccountingEntityCompanyId_Zero_ReturnsError()
    {
        _validator.Validate("AccountingEntityCompanyId", 0, Context(0))
            .Should().ContainSingle().Which.Should().Contain("entidad contable");
    }

    [Fact]
    public void Validate_AccountingEntityCompanyId_Negative_ReturnsError()
    {
        _validator.Validate("AccountingEntityCompanyId", -5, Context(-5))
            .Should().ContainSingle();
    }

    [Fact]
    public void Validate_AccountingEntityCompanyId_Positive_ReturnsEmpty()
    {
        _validator.Validate("AccountingEntityCompanyId", 42, Context(42))
            .Should().BeEmpty();
    }

    [Fact]
    public void Validate_UnknownProperty_ReturnsEmpty()
    {
        _validator.Validate("NonExistent", 0, Context(0))
            .Should().BeEmpty();
    }

    #endregion

    #region ValidateAll

    [Fact]
    public void ValidateAll_ZeroId_ReturnsErrorForAccountingEntityCompanyId()
    {
        _validator.ValidateAll(Context(0))
            .Should().ContainKey("AccountingEntityCompanyId");
    }

    [Fact]
    public void ValidateAll_PositiveId_ReturnsEmpty()
    {
        _validator.ValidateAll(Context(10))
            .Should().BeEmpty();
    }

    #endregion

    #region CanSave

    [Fact]
    public void CanSave_AllValid_ReturnsTrue()
    {
        CompanyCanSaveContext context = new()
        {
            IsBusy = false,
            AccountingEntityCompanyId = 5,
            HasChanges = true,
            HasErrors = false
        };

        _validator.CanSave(context).Should().BeTrue();
    }

    [Fact]
    public void CanSave_IsBusy_ReturnsFalse()
    {
        CompanyCanSaveContext context = new()
        {
            IsBusy = true,
            AccountingEntityCompanyId = 5,
            HasChanges = true,
            HasErrors = false
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_ZeroId_ReturnsFalse()
    {
        CompanyCanSaveContext context = new()
        {
            IsBusy = false,
            AccountingEntityCompanyId = 0,
            HasChanges = true,
            HasErrors = false
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_NegativeId_ReturnsFalse()
    {
        CompanyCanSaveContext context = new()
        {
            IsBusy = false,
            AccountingEntityCompanyId = -1,
            HasChanges = true,
            HasErrors = false
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_NoChanges_ReturnsFalse()
    {
        CompanyCanSaveContext context = new()
        {
            IsBusy = false,
            AccountingEntityCompanyId = 5,
            HasChanges = false,
            HasErrors = false
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_HasErrors_ReturnsFalse()
    {
        CompanyCanSaveContext context = new()
        {
            IsBusy = false,
            AccountingEntityCompanyId = 5,
            HasChanges = true,
            HasErrors = true
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    #endregion
}
