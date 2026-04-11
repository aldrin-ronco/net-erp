using FluentAssertions;
using NetErp.Inventory.CatalogItems.Validators;
using Xunit;

namespace NetErp.Tests.Validators;

/// <summary>
/// Tests puros del validador — new CatalogValidator() + assert. Cero mocks.
/// </summary>
public class CatalogValidatorTests
{
    private readonly CatalogValidator _validator = new();

    private static CatalogValidationContext Context(string? name = null) => new() { Name = name };

    #region Validate (single property)

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
        _validator.Validate("Name", "Catálogo Principal", Context(name: "Catálogo Principal"))
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
        _validator.ValidateAll(Context(name: "Catálogo Principal"))
            .Should().BeEmpty();
    }

    #endregion

    #region CanSave

    [Fact]
    public void CanSave_AllValid_ReturnsTrue()
    {
        CatalogCanSaveContext context = new()
        {
            Name = "Catálogo Principal",
            HasChanges = true,
            HasErrors = false
        };

        _validator.CanSave(context).Should().BeTrue();
    }

    [Fact]
    public void CanSave_IsBusy_ReturnsFalse()
    {
        CatalogCanSaveContext context = new()
        {
            IsBusy = true,
            Name = "Catálogo Principal",
            HasChanges = true,
            HasErrors = false
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_EmptyName_ReturnsFalse()
    {
        CatalogCanSaveContext context = new()
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
        CatalogCanSaveContext context = new()
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
        CatalogCanSaveContext context = new()
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
        CatalogCanSaveContext context = new()
        {
            Name = "Catálogo Principal",
            HasChanges = false,
            HasErrors = false
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_HasErrors_ReturnsFalse()
    {
        CatalogCanSaveContext context = new()
        {
            Name = "Catálogo Principal",
            HasChanges = true,
            HasErrors = true
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    #endregion
}
