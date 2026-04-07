using System.Collections.Generic;
using FluentAssertions;
using NetErp.Global.CostCenters.Validators;
using Xunit;

namespace NetErp.Tests.Validators;

/// <summary>
/// Tests puros del validador — new CostCenterValidator() + assert. Cero mocks.
/// </summary>
public class CostCenterValidatorTests
{
    private readonly CostCenterValidator _validator = new();

    private static CostCenterValidationContext Context(
        string? name = null,
        string? shortName = null,
        string? primaryPhone = null,
        string? secondaryPhone = null,
        string? primaryCellPhone = null,
        string? secondaryCellPhone = null) => new()
    {
        Name = name,
        ShortName = shortName,
        PrimaryPhone = primaryPhone,
        SecondaryPhone = secondaryPhone,
        PrimaryCellPhone = primaryCellPhone,
        SecondaryCellPhone = secondaryCellPhone
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
        _validator.Validate("Name", "Centro Norte", Context(name: "Centro Norte"))
            .Should().BeEmpty();
    }

    #endregion

    #region Validate — ShortName

    [Fact]
    public void Validate_ShortName_Empty_ReturnsError()
    {
        _validator.Validate("ShortName", "", Context())
            .Should().ContainSingle().Which.Should().Contain("nombre corto");
    }

    [Fact]
    public void Validate_ShortName_Null_ReturnsError()
    {
        _validator.Validate("ShortName", null, Context())
            .Should().ContainSingle();
    }

    [Fact]
    public void Validate_ShortName_Valid_ReturnsEmpty()
    {
        _validator.Validate("ShortName", "CN", Context(shortName: "CN"))
            .Should().BeEmpty();
    }

    #endregion

    #region Validate — PrimaryPhone

    [Fact]
    public void Validate_PrimaryPhone_Not7Digits_ReturnsError()
    {
        _validator.Validate("PrimaryPhone", "12345", Context())
            .Should().ContainSingle().Which.Should().Contain("7 dígitos");
    }

    [Fact]
    public void Validate_PrimaryPhone_7Digits_ReturnsEmpty()
    {
        _validator.Validate("PrimaryPhone", "1234567", Context())
            .Should().BeEmpty();
    }

    [Fact]
    public void Validate_PrimaryPhone_Empty_ReturnsEmpty()
    {
        _validator.Validate("PrimaryPhone", "", Context())
            .Should().BeEmpty();
    }

    [Fact]
    public void Validate_PrimaryPhone_CleansSpaces()
    {
        _validator.Validate("PrimaryPhone", "123 4567", Context())
            .Should().BeEmpty();
    }

    [Fact]
    public void Validate_PrimaryPhone_CleansHyphens()
    {
        _validator.Validate("PrimaryPhone", "123-4567", Context())
            .Should().BeEmpty();
    }

    #endregion

    #region Validate — SecondaryPhone

    [Fact]
    public void Validate_SecondaryPhone_Not7Digits_ReturnsError()
    {
        _validator.Validate("SecondaryPhone", "123", Context())
            .Should().ContainSingle().Which.Should().Contain("7 dígitos");
    }

    [Fact]
    public void Validate_SecondaryPhone_7Digits_ReturnsEmpty()
    {
        _validator.Validate("SecondaryPhone", "7654321", Context())
            .Should().BeEmpty();
    }

    [Fact]
    public void Validate_SecondaryPhone_Empty_ReturnsEmpty()
    {
        _validator.Validate("SecondaryPhone", "", Context())
            .Should().BeEmpty();
    }

    #endregion

    #region Validate — PrimaryCellPhone

    [Fact]
    public void Validate_PrimaryCellPhone_Not10Digits_ReturnsError()
    {
        _validator.Validate("PrimaryCellPhone", "12345", Context())
            .Should().ContainSingle().Which.Should().Contain("10 dígitos");
    }

    [Fact]
    public void Validate_PrimaryCellPhone_10Digits_ReturnsEmpty()
    {
        _validator.Validate("PrimaryCellPhone", "3001234567", Context())
            .Should().BeEmpty();
    }

    [Fact]
    public void Validate_PrimaryCellPhone_Empty_ReturnsEmpty()
    {
        _validator.Validate("PrimaryCellPhone", "", Context())
            .Should().BeEmpty();
    }

    [Fact]
    public void Validate_PrimaryCellPhone_CleansSpacesAndHyphens()
    {
        _validator.Validate("PrimaryCellPhone", "300 123-45 67", Context())
            .Should().BeEmpty();
    }

    #endregion

    #region Validate — SecondaryCellPhone

    [Fact]
    public void Validate_SecondaryCellPhone_Not10Digits_ReturnsError()
    {
        _validator.Validate("SecondaryCellPhone", "123456", Context())
            .Should().ContainSingle().Which.Should().Contain("10 dígitos");
    }

    [Fact]
    public void Validate_SecondaryCellPhone_10Digits_ReturnsEmpty()
    {
        _validator.Validate("SecondaryCellPhone", "3001234567", Context())
            .Should().BeEmpty();
    }

    #endregion

    #region Validate — Unknown property

    [Fact]
    public void Validate_UnknownProperty_ReturnsEmpty()
    {
        _validator.Validate("NonExistent", "anything", Context())
            .Should().BeEmpty();
    }

    #endregion

    #region ValidateAll

    [Fact]
    public void ValidateAll_EmptyNameAndShortName_ReturnsBothErrors()
    {
        Dictionary<string, IReadOnlyList<string>> result = _validator.ValidateAll(Context());

        result.Should().ContainKey("Name");
        result.Should().ContainKey("ShortName");
    }

    [Fact]
    public void ValidateAll_AllValid_ReturnsEmpty()
    {
        CostCenterValidationContext context = Context(
            name: "Centro Norte",
            shortName: "CN",
            primaryPhone: "1234567",
            primaryCellPhone: "3001234567");

        _validator.ValidateAll(context).Should().BeEmpty();
    }

    [Fact]
    public void ValidateAll_InvalidPhone_ReturnsErrorForPhone()
    {
        CostCenterValidationContext context = Context(
            name: "Centro Norte",
            shortName: "CN",
            primaryPhone: "123");

        _validator.ValidateAll(context).Should().ContainKey("PrimaryPhone");
    }

    [Fact]
    public void ValidateAll_InvalidCellPhone_ReturnsErrorForCellPhone()
    {
        CostCenterValidationContext context = Context(
            name: "Centro Norte",
            shortName: "CN",
            primaryCellPhone: "300");

        _validator.ValidateAll(context).Should().ContainKey("PrimaryCellPhone");
    }

    [Fact]
    public void ValidateAll_AllPhonesEmpty_ReturnsEmpty()
    {
        CostCenterValidationContext context = Context(
            name: "Centro Norte",
            shortName: "CN",
            primaryPhone: "",
            secondaryPhone: "",
            primaryCellPhone: "",
            secondaryCellPhone: "");

        _validator.ValidateAll(context).Should().BeEmpty();
    }

    #endregion

    #region CanSave

    [Fact]
    public void CanSave_AllValid_ReturnsTrue()
    {
        CostCenterCanSaveContext context = new()
        {
            IsBusy = false,
            Name = "Centro",
            ShortName = "CN",
            HasChanges = true,
            HasErrors = false
        };

        _validator.CanSave(context).Should().BeTrue();
    }

    [Fact]
    public void CanSave_IsBusy_ReturnsFalse()
    {
        CostCenterCanSaveContext context = new()
        {
            IsBusy = true,
            Name = "Centro",
            ShortName = "CN",
            HasChanges = true,
            HasErrors = false
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_EmptyName_ReturnsFalse()
    {
        CostCenterCanSaveContext context = new()
        {
            IsBusy = false,
            Name = "",
            ShortName = "CN",
            HasChanges = true,
            HasErrors = false
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_NullName_ReturnsFalse()
    {
        CostCenterCanSaveContext context = new()
        {
            IsBusy = false,
            Name = null,
            ShortName = "CN",
            HasChanges = true,
            HasErrors = false
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_EmptyShortName_ReturnsFalse()
    {
        CostCenterCanSaveContext context = new()
        {
            IsBusy = false,
            Name = "Centro",
            ShortName = "",
            HasChanges = true,
            HasErrors = false
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_NullShortName_ReturnsFalse()
    {
        CostCenterCanSaveContext context = new()
        {
            IsBusy = false,
            Name = "Centro",
            ShortName = null,
            HasChanges = true,
            HasErrors = false
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_NoChanges_ReturnsFalse()
    {
        CostCenterCanSaveContext context = new()
        {
            IsBusy = false,
            Name = "Centro",
            ShortName = "CN",
            HasChanges = false,
            HasErrors = false
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_HasErrors_ReturnsFalse()
    {
        CostCenterCanSaveContext context = new()
        {
            IsBusy = false,
            Name = "Centro",
            ShortName = "CN",
            HasChanges = true,
            HasErrors = true
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    #endregion
}
