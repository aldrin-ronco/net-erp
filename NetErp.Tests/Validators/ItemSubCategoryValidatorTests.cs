using FluentAssertions;
using NetErp.Inventory.CatalogItems.Validators;
using Xunit;

namespace NetErp.Tests.Validators;

/// <summary>
/// Tests puros del validador — new ItemSubCategoryValidator() + assert. Cero mocks.
/// </summary>
public class ItemSubCategoryValidatorTests
{
    private readonly ItemSubCategoryValidator _validator = new();

    private static ItemSubCategoryValidationContext Context(string? name = null) => new() { Name = name };

    #region Validate (single property)

    [Fact]
    public void Validate_Name_Empty_ReturnsError()
    {
        _validator.Validate("Name", "", Context())
            .Should().ContainSingle().Which.Should().Contain("subcategoría");
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
        _validator.Validate("Name", "Gaseosas", Context(name: "Gaseosas"))
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
    public void ValidateAll_EmptyName_ReturnsError()
    {
        _validator.ValidateAll(Context(name: ""))
            .Should().ContainKey("Name");
    }

    [Fact]
    public void ValidateAll_NullName_ReturnsError()
    {
        _validator.ValidateAll(Context(name: null))
            .Should().ContainKey("Name");
    }

    [Fact]
    public void ValidateAll_ValidName_ReturnsEmpty()
    {
        _validator.ValidateAll(Context(name: "Gaseosas"))
            .Should().BeEmpty();
    }

    #endregion

    #region CanSave

    [Fact]
    public void CanSave_AllValid_ReturnsTrue()
    {
        ItemSubCategoryCanSaveContext context = new()
        {
            Name = "Gaseosas",
            HasChanges = true,
            HasErrors = false
        };

        _validator.CanSave(context).Should().BeTrue();
    }

    [Fact]
    public void CanSave_IsBusy_ReturnsFalse()
    {
        ItemSubCategoryCanSaveContext context = new()
        {
            IsBusy = true,
            Name = "Gaseosas",
            HasChanges = true,
            HasErrors = false
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_EmptyName_ReturnsFalse()
    {
        ItemSubCategoryCanSaveContext context = new()
        {
            Name = "",
            HasChanges = true,
            HasErrors = false
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_NullName_ReturnsFalse()
    {
        ItemSubCategoryCanSaveContext context = new()
        {
            Name = null,
            HasChanges = true,
            HasErrors = false
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_WhitespaceName_ReturnsFalse()
    {
        ItemSubCategoryCanSaveContext context = new()
        {
            Name = "   ",
            HasChanges = true,
            HasErrors = false
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_NoChanges_ReturnsFalse()
    {
        ItemSubCategoryCanSaveContext context = new()
        {
            Name = "Gaseosas",
            HasChanges = false,
            HasErrors = false
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_HasErrors_ReturnsFalse()
    {
        ItemSubCategoryCanSaveContext context = new()
        {
            Name = "Gaseosas",
            HasChanges = true,
            HasErrors = true
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    #endregion
}
