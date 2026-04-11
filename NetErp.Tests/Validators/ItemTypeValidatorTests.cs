using FluentAssertions;
using NetErp.Inventory.CatalogItems.Validators;
using Xunit;

namespace NetErp.Tests.Validators;

/// <summary>
/// Tests puros del validador — new ItemTypeValidator() + assert. Cero mocks.
/// </summary>
public class ItemTypeValidatorTests
{
    private readonly ItemTypeValidator _validator = new();

    private static ItemTypeValidationContext Context(
        string? name = null,
        string? prefixChar = null,
        int defaultMeasurementUnitId = 0,
        int defaultAccountingGroupId = 0) => new()
        {
            Name = name,
            PrefixChar = prefixChar,
            DefaultMeasurementUnitId = defaultMeasurementUnitId,
            DefaultAccountingGroupId = defaultAccountingGroupId
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
        _validator.Validate("Name", "Producto", Context(name: "Producto"))
            .Should().BeEmpty();
    }

    #endregion

    #region Validate — PrefixChar

    [Fact]
    public void Validate_PrefixChar_Empty_ReturnsError()
    {
        _validator.Validate("PrefixChar", "", Context())
            .Should().ContainSingle().Which.Should().Contain("vacío");
    }

    [Fact]
    public void Validate_PrefixChar_Null_ReturnsError()
    {
        _validator.Validate("PrefixChar", null, Context())
            .Should().ContainSingle();
    }

    [Fact]
    public void Validate_PrefixChar_TwoChars_ReturnsError()
    {
        _validator.Validate("PrefixChar", "AB", Context())
            .Should().ContainSingle().Which.Should().Contain("un caracter");
    }

    [Fact]
    public void Validate_PrefixChar_Lowercase_ReturnsError()
    {
        _validator.Validate("PrefixChar", "a", Context())
            .Should().ContainSingle().Which.Should().Contain("mayúscula");
    }

    [Fact]
    public void Validate_PrefixChar_Digit_ReturnsError()
    {
        _validator.Validate("PrefixChar", "1", Context())
            .Should().ContainSingle().Which.Should().Contain("mayúscula");
    }

    [Fact]
    public void Validate_PrefixChar_SpecialChar_ReturnsError()
    {
        _validator.Validate("PrefixChar", "@", Context())
            .Should().ContainSingle().Which.Should().Contain("mayúscula");
    }

    [Fact]
    public void Validate_PrefixChar_UppercaseA_ReturnsEmpty()
    {
        _validator.Validate("PrefixChar", "A", Context())
            .Should().BeEmpty();
    }

    [Fact]
    public void Validate_PrefixChar_UppercaseZ_ReturnsEmpty()
    {
        _validator.Validate("PrefixChar", "Z", Context())
            .Should().BeEmpty();
    }

    #endregion

    #region Validate — DefaultMeasurementUnitId

    [Fact]
    public void Validate_DefaultMeasurementUnitId_Zero_ReturnsError()
    {
        _validator.Validate("DefaultMeasurementUnitId", 0, Context())
            .Should().ContainSingle().Which.Should().Contain("unidad de medida");
    }

    [Fact]
    public void Validate_DefaultMeasurementUnitId_Negative_ReturnsError()
    {
        _validator.Validate("DefaultMeasurementUnitId", -1, Context())
            .Should().ContainSingle();
    }

    [Fact]
    public void Validate_DefaultMeasurementUnitId_Positive_ReturnsEmpty()
    {
        _validator.Validate("DefaultMeasurementUnitId", 5, Context())
            .Should().BeEmpty();
    }

    #endregion

    #region Validate — DefaultAccountingGroupId

    [Fact]
    public void Validate_DefaultAccountingGroupId_Zero_ReturnsError()
    {
        _validator.Validate("DefaultAccountingGroupId", 0, Context())
            .Should().ContainSingle().Which.Should().Contain("grupo contable");
    }

    [Fact]
    public void Validate_DefaultAccountingGroupId_Negative_ReturnsError()
    {
        _validator.Validate("DefaultAccountingGroupId", -1, Context())
            .Should().ContainSingle();
    }

    [Fact]
    public void Validate_DefaultAccountingGroupId_Positive_ReturnsEmpty()
    {
        _validator.Validate("DefaultAccountingGroupId", 5, Context())
            .Should().BeEmpty();
    }

    #endregion

    #region Validate — Unknown property

    [Fact]
    public void Validate_UnknownProperty_ReturnsEmpty()
    {
        _validator.Validate("NonExistent", "foo", Context())
            .Should().BeEmpty();
    }

    #endregion

    #region ValidateAll

    [Fact]
    public void ValidateAll_AllEmpty_ReturnsAllErrors()
    {
        var result = _validator.ValidateAll(Context());

        result.Should().ContainKey("Name");
        result.Should().ContainKey("PrefixChar");
        result.Should().ContainKey("DefaultMeasurementUnitId");
        result.Should().ContainKey("DefaultAccountingGroupId");
    }

    [Fact]
    public void ValidateAll_AllValid_ReturnsEmpty()
    {
        var result = _validator.ValidateAll(Context(
            name: "Producto",
            prefixChar: "P",
            defaultMeasurementUnitId: 1,
            defaultAccountingGroupId: 2));

        result.Should().BeEmpty();
    }

    [Fact]
    public void ValidateAll_InvalidPrefixChar_ReturnsOnlyPrefixError()
    {
        var result = _validator.ValidateAll(Context(
            name: "Producto",
            prefixChar: "ab",
            defaultMeasurementUnitId: 1,
            defaultAccountingGroupId: 2));

        result.Should().HaveCount(1);
        result.Should().ContainKey("PrefixChar");
    }

    #endregion

    #region CanSave

    [Fact]
    public void CanSave_AllValid_ReturnsTrue()
    {
        ItemTypeCanSaveContext context = new()
        {
            Name = "Producto",
            PrefixChar = "P",
            DefaultMeasurementUnitId = 1,
            DefaultAccountingGroupId = 2,
            HasChanges = true,
            HasErrors = false
        };

        _validator.CanSave(context).Should().BeTrue();
    }

    [Fact]
    public void CanSave_IsBusy_ReturnsFalse()
    {
        ItemTypeCanSaveContext context = new()
        {
            IsBusy = true,
            Name = "Producto",
            PrefixChar = "P",
            DefaultMeasurementUnitId = 1,
            DefaultAccountingGroupId = 2,
            HasChanges = true
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_EmptyName_ReturnsFalse()
    {
        ItemTypeCanSaveContext context = new()
        {
            Name = "",
            PrefixChar = "P",
            DefaultMeasurementUnitId = 1,
            DefaultAccountingGroupId = 2,
            HasChanges = true
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_EmptyPrefixChar_ReturnsFalse()
    {
        ItemTypeCanSaveContext context = new()
        {
            Name = "Producto",
            PrefixChar = "",
            DefaultMeasurementUnitId = 1,
            DefaultAccountingGroupId = 2,
            HasChanges = true
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_PrefixCharTwoChars_ReturnsFalse()
    {
        ItemTypeCanSaveContext context = new()
        {
            Name = "Producto",
            PrefixChar = "AB",
            DefaultMeasurementUnitId = 1,
            DefaultAccountingGroupId = 2,
            HasChanges = true
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_PrefixCharLowercase_ReturnsFalse()
    {
        ItemTypeCanSaveContext context = new()
        {
            Name = "Producto",
            PrefixChar = "a",
            DefaultMeasurementUnitId = 1,
            DefaultAccountingGroupId = 2,
            HasChanges = true
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_PrefixCharDigit_ReturnsFalse()
    {
        ItemTypeCanSaveContext context = new()
        {
            Name = "Producto",
            PrefixChar = "1",
            DefaultMeasurementUnitId = 1,
            DefaultAccountingGroupId = 2,
            HasChanges = true
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_MeasurementUnitZero_ReturnsFalse()
    {
        ItemTypeCanSaveContext context = new()
        {
            Name = "Producto",
            PrefixChar = "P",
            DefaultMeasurementUnitId = 0,
            DefaultAccountingGroupId = 2,
            HasChanges = true
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_AccountingGroupZero_ReturnsFalse()
    {
        ItemTypeCanSaveContext context = new()
        {
            Name = "Producto",
            PrefixChar = "P",
            DefaultMeasurementUnitId = 1,
            DefaultAccountingGroupId = 0,
            HasChanges = true
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_NoChanges_ReturnsFalse()
    {
        ItemTypeCanSaveContext context = new()
        {
            Name = "Producto",
            PrefixChar = "P",
            DefaultMeasurementUnitId = 1,
            DefaultAccountingGroupId = 2,
            HasChanges = false,
            HasErrors = false
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_HasErrors_ReturnsFalse()
    {
        ItemTypeCanSaveContext context = new()
        {
            Name = "Producto",
            PrefixChar = "P",
            DefaultMeasurementUnitId = 1,
            DefaultAccountingGroupId = 2,
            HasChanges = true,
            HasErrors = true
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    #endregion
}
