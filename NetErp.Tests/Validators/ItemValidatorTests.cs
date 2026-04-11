using FluentAssertions;
using NetErp.Inventory.CatalogItems.Validators;
using Xunit;

namespace NetErp.Tests.Validators;

/// <summary>
/// Tests puros del validador — new ItemValidator() + assert. Cero mocks.
/// </summary>
public class ItemValidatorTests
{
    private readonly ItemValidator _validator = new();

    private static ItemValidationContext Context(
        string? name = null,
        string? reference = null,
        bool hasMeasurementUnit = false,
        bool hasAccountingGroup = false) => new()
        {
            Name = name,
            Reference = reference,
            HasMeasurementUnit = hasMeasurementUnit,
            HasAccountingGroup = hasAccountingGroup
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
        _validator.Validate("Name", "Coca Cola 350ml", Context(name: "Coca Cola 350ml"))
            .Should().BeEmpty();
    }

    #endregion

    #region Validate — Reference

    [Fact]
    public void Validate_Reference_Empty_ReturnsError()
    {
        _validator.Validate("Reference", "", Context())
            .Should().ContainSingle().Which.Should().Contain("referencia");
    }

    [Fact]
    public void Validate_Reference_Null_ReturnsError()
    {
        _validator.Validate("Reference", null, Context())
            .Should().ContainSingle();
    }

    [Fact]
    public void Validate_Reference_Whitespace_ReturnsError()
    {
        _validator.Validate("Reference", "   ", Context())
            .Should().ContainSingle();
    }

    [Fact]
    public void Validate_Reference_Valid_ReturnsEmpty()
    {
        _validator.Validate("Reference", "REF-001", Context(reference: "REF-001"))
            .Should().BeEmpty();
    }

    #endregion

    #region Validate — SelectedMeasurementUnit

    [Fact]
    public void Validate_SelectedMeasurementUnit_Null_ReturnsError()
    {
        _validator.Validate("SelectedMeasurementUnit", null, Context())
            .Should().ContainSingle().Which.Should().Contain("unidad de medida");
    }

    [Fact]
    public void Validate_SelectedMeasurementUnit_NonNull_ReturnsEmpty()
    {
        _validator.Validate("SelectedMeasurementUnit", new object(), Context())
            .Should().BeEmpty();
    }

    #endregion

    #region Validate — SelectedAccountingGroup

    [Fact]
    public void Validate_SelectedAccountingGroup_Null_ReturnsError()
    {
        _validator.Validate("SelectedAccountingGroup", null, Context())
            .Should().ContainSingle().Which.Should().Contain("grupo contable");
    }

    [Fact]
    public void Validate_SelectedAccountingGroup_NonNull_ReturnsEmpty()
    {
        _validator.Validate("SelectedAccountingGroup", new object(), Context())
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
        result.Should().ContainKey("Reference");
        result.Should().ContainKey("SelectedMeasurementUnit");
        result.Should().ContainKey("SelectedAccountingGroup");
    }

    [Fact]
    public void ValidateAll_AllValid_ReturnsEmpty()
    {
        var result = _validator.ValidateAll(Context(
            name: "Coca Cola 350ml",
            reference: "REF-001",
            hasMeasurementUnit: true,
            hasAccountingGroup: true));

        result.Should().BeEmpty();
    }

    [Fact]
    public void ValidateAll_MissingReference_ReturnsOnlyReferenceError()
    {
        var result = _validator.ValidateAll(Context(
            name: "Coca Cola 350ml",
            reference: "",
            hasMeasurementUnit: true,
            hasAccountingGroup: true));

        result.Should().HaveCount(1);
        result.Should().ContainKey("Reference");
    }

    [Fact]
    public void ValidateAll_MissingMeasurementUnit_ReturnsOnlyMeasurementUnitError()
    {
        var result = _validator.ValidateAll(Context(
            name: "Coca Cola 350ml",
            reference: "REF-001",
            hasMeasurementUnit: false,
            hasAccountingGroup: true));

        result.Should().HaveCount(1);
        result.Should().ContainKey("SelectedMeasurementUnit");
    }

    [Fact]
    public void ValidateAll_MissingAccountingGroup_ReturnsOnlyAccountingGroupError()
    {
        var result = _validator.ValidateAll(Context(
            name: "Coca Cola 350ml",
            reference: "REF-001",
            hasMeasurementUnit: true,
            hasAccountingGroup: false));

        result.Should().HaveCount(1);
        result.Should().ContainKey("SelectedAccountingGroup");
    }

    #endregion

    #region CanSave

    [Fact]
    public void CanSave_AllValid_ReturnsTrue()
    {
        ItemCanSaveContext context = new()
        {
            Name = "Coca Cola 350ml",
            Reference = "REF-001",
            HasMeasurementUnit = true,
            HasAccountingGroup = true,
            HasChanges = true,
            HasErrors = false
        };

        _validator.CanSave(context).Should().BeTrue();
    }

    [Fact]
    public void CanSave_IsBusy_ReturnsFalse()
    {
        ItemCanSaveContext context = new()
        {
            IsBusy = true,
            Name = "Coca Cola 350ml",
            Reference = "REF-001",
            HasMeasurementUnit = true,
            HasAccountingGroup = true,
            HasChanges = true
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_EmptyName_ReturnsFalse()
    {
        ItemCanSaveContext context = new()
        {
            Name = "",
            Reference = "REF-001",
            HasMeasurementUnit = true,
            HasAccountingGroup = true,
            HasChanges = true
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_NullName_ReturnsFalse()
    {
        ItemCanSaveContext context = new()
        {
            Name = null,
            Reference = "REF-001",
            HasMeasurementUnit = true,
            HasAccountingGroup = true,
            HasChanges = true
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_EmptyReference_ReturnsFalse()
    {
        ItemCanSaveContext context = new()
        {
            Name = "Coca Cola 350ml",
            Reference = "",
            HasMeasurementUnit = true,
            HasAccountingGroup = true,
            HasChanges = true
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_NullReference_ReturnsFalse()
    {
        ItemCanSaveContext context = new()
        {
            Name = "Coca Cola 350ml",
            Reference = null,
            HasMeasurementUnit = true,
            HasAccountingGroup = true,
            HasChanges = true
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_NoMeasurementUnit_ReturnsFalse()
    {
        ItemCanSaveContext context = new()
        {
            Name = "Coca Cola 350ml",
            Reference = "REF-001",
            HasMeasurementUnit = false,
            HasAccountingGroup = true,
            HasChanges = true
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_NoAccountingGroup_ReturnsFalse()
    {
        ItemCanSaveContext context = new()
        {
            Name = "Coca Cola 350ml",
            Reference = "REF-001",
            HasMeasurementUnit = true,
            HasAccountingGroup = false,
            HasChanges = true
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_NoChanges_ReturnsFalse()
    {
        ItemCanSaveContext context = new()
        {
            Name = "Coca Cola 350ml",
            Reference = "REF-001",
            HasMeasurementUnit = true,
            HasAccountingGroup = true,
            HasChanges = false,
            HasErrors = false
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_HasErrors_ReturnsFalse()
    {
        ItemCanSaveContext context = new()
        {
            Name = "Coca Cola 350ml",
            Reference = "REF-001",
            HasMeasurementUnit = true,
            HasAccountingGroup = true,
            HasChanges = true,
            HasErrors = true
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    #endregion
}
