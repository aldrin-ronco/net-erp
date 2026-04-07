using FluentAssertions;
using NetErp.Global.CostCenters.Validators;
using Xunit;

namespace NetErp.Tests.Validators;

/// <summary>
/// Tests puros del validador — new StorageValidator() + assert. Cero mocks.
/// </summary>
public class StorageValidatorTests
{
    private readonly StorageValidator _validator = new();

    private static StorageValidationContext Context(string? name = null) => new()
    {
        Name = name
    };

    #region Validate — Name

    [Fact]
    public void Validate_Name_Empty_ReturnsError()
    {
        _validator.Validate("Name", "", Context())
            .Should().ContainSingle().Which.Should().Contain("bodega");
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
        _validator.Validate("Name", "Bodega Principal", Context("Bodega Principal"))
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
    public void ValidateAll_ValidName_ReturnsEmpty()
    {
        _validator.ValidateAll(Context("Bodega Sur"))
            .Should().BeEmpty();
    }

    #endregion

    #region CanSave

    [Fact]
    public void CanSave_AllValid_ReturnsTrue()
    {
        StorageCanSaveContext context = new()
        {
            IsBusy = false,
            Name = "Bodega",
            SelectedCityId = 1,
            HasChanges = true,
            HasErrors = false
        };

        _validator.CanSave(context).Should().BeTrue();
    }

    [Fact]
    public void CanSave_IsBusy_ReturnsFalse()
    {
        StorageCanSaveContext context = new()
        {
            IsBusy = true,
            Name = "Bodega",
            SelectedCityId = 1,
            HasChanges = true,
            HasErrors = false
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_EmptyName_ReturnsFalse()
    {
        StorageCanSaveContext context = new()
        {
            IsBusy = false,
            Name = "",
            SelectedCityId = 1,
            HasChanges = true,
            HasErrors = false
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_NullName_ReturnsFalse()
    {
        StorageCanSaveContext context = new()
        {
            IsBusy = false,
            Name = null,
            SelectedCityId = 1,
            HasChanges = true,
            HasErrors = false
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_NoCity_ReturnsFalse()
    {
        StorageCanSaveContext context = new()
        {
            IsBusy = false,
            Name = "Bodega",
            SelectedCityId = 0,
            HasChanges = true,
            HasErrors = false
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_NegativeCityId_ReturnsFalse()
    {
        StorageCanSaveContext context = new()
        {
            IsBusy = false,
            Name = "Bodega",
            SelectedCityId = -1,
            HasChanges = true,
            HasErrors = false
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_NoChanges_ReturnsFalse()
    {
        StorageCanSaveContext context = new()
        {
            IsBusy = false,
            Name = "Bodega",
            SelectedCityId = 1,
            HasChanges = false,
            HasErrors = false
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_HasErrors_ReturnsFalse()
    {
        StorageCanSaveContext context = new()
        {
            IsBusy = false,
            Name = "Bodega",
            SelectedCityId = 1,
            HasChanges = true,
            HasErrors = true
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    #endregion
}
