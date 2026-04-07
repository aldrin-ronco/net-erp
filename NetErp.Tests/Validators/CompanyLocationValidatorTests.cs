using FluentAssertions;
using NetErp.Global.CostCenters.Validators;
using Xunit;

namespace NetErp.Tests.Validators;

/// <summary>
/// Tests puros del validador — new CompanyLocationValidator() + assert. Cero mocks.
/// </summary>
public class CompanyLocationValidatorTests
{
    private readonly CompanyLocationValidator _validator = new();

    private static CompanyLocationValidationContext Context(string? name = null) => new()
    {
        Name = name
    };

    #region Validate — Name

    [Fact]
    public void Validate_Name_Empty_ReturnsError()
    {
        _validator.Validate("Name", "", Context())
            .Should().ContainSingle().Which.Should().Contain("nombre");
    }

    [Fact]
    public void Validate_Name_Null_ReturnsError()
    {
        _validator.Validate("Name", null, Context())
            .Should().ContainSingle();
    }

    [Fact]
    public void Validate_Name_Whitespace_ReturnsError()
    {
        _validator.Validate("Name", "   ", Context())
            .Should().ContainSingle();
    }

    [Fact]
    public void Validate_Name_Valid_ReturnsEmpty()
    {
        _validator.Validate("Name", "Sede Norte", Context("Sede Norte"))
            .Should().BeEmpty();
    }

    [Fact]
    public void Validate_UnknownProperty_ReturnsEmpty()
    {
        _validator.Validate("NonExistent", "", Context())
            .Should().BeEmpty();
    }

    #endregion

    #region ValidateAll

    [Fact]
    public void ValidateAll_EmptyName_ReturnsErrorForName()
    {
        _validator.ValidateAll(Context(""))
            .Should().ContainKey("Name");
    }

    [Fact]
    public void ValidateAll_NullName_ReturnsErrorForName()
    {
        _validator.ValidateAll(Context(null))
            .Should().ContainKey("Name");
    }

    [Fact]
    public void ValidateAll_ValidName_ReturnsEmpty()
    {
        _validator.ValidateAll(Context("Sede Centro"))
            .Should().BeEmpty();
    }

    #endregion

    #region CanSave

    [Fact]
    public void CanSave_AllValid_ReturnsTrue()
    {
        CompanyLocationCanSaveContext context = new()
        {
            IsBusy = false,
            Name = "Sede Norte",
            HasChanges = true,
            HasErrors = false
        };

        _validator.CanSave(context).Should().BeTrue();
    }

    [Fact]
    public void CanSave_IsBusy_ReturnsFalse()
    {
        CompanyLocationCanSaveContext context = new()
        {
            IsBusy = true,
            Name = "Sede Norte",
            HasChanges = true,
            HasErrors = false
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_EmptyName_ReturnsFalse()
    {
        CompanyLocationCanSaveContext context = new()
        {
            IsBusy = false,
            Name = "",
            HasChanges = true,
            HasErrors = false
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_NullName_ReturnsFalse()
    {
        CompanyLocationCanSaveContext context = new()
        {
            IsBusy = false,
            Name = null,
            HasChanges = true,
            HasErrors = false
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_WhitespaceName_ReturnsFalse()
    {
        CompanyLocationCanSaveContext context = new()
        {
            IsBusy = false,
            Name = "   ",
            HasChanges = true,
            HasErrors = false
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_NoChanges_ReturnsFalse()
    {
        CompanyLocationCanSaveContext context = new()
        {
            IsBusy = false,
            Name = "Sede Norte",
            HasChanges = false,
            HasErrors = false
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_HasErrors_ReturnsFalse()
    {
        CompanyLocationCanSaveContext context = new()
        {
            IsBusy = false,
            Name = "Sede Norte",
            HasChanges = true,
            HasErrors = true
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    #endregion
}
