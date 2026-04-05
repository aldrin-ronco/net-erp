using FluentAssertions;
using NetErp.Billing.Sellers.Validators;
using Xunit;

namespace NetErp.Tests.Validators;

/// <summary>
/// Tests puros del validador — new SellerValidator() + assert. Cero mocks.
/// </summary>
public class SellerValidatorTests
{
    private readonly SellerValidator _validator = new();

    private static SellerValidationContext PNContext(
        int minDocLength = 5,
        string? firstName = null,
        string? firstLastName = null,
        string? identificationNumber = null) => new()
    {
        CaptureInfoAsPN = true,
        MinimumDocumentLength = minDocLength,
        FirstName = firstName,
        FirstLastName = firstLastName,
        IdentificationNumber = identificationNumber
    };

    private static SellerValidationContext PJContext(int minDocLength = 5) => new()
    {
        CaptureInfoAsPN = false,
        MinimumDocumentLength = minDocLength
    };

    #region Validate — IdentificationNumber

    [Fact]
    public void Validate_IdentificationNumber_Empty_ReturnsError()
    {
        _validator.Validate("IdentificationNumber", "", PNContext())
            .Should().ContainSingle();
    }

    [Fact]
    public void Validate_IdentificationNumber_Null_ReturnsError()
    {
        _validator.Validate("IdentificationNumber", null, PNContext())
            .Should().ContainSingle();
    }

    [Fact]
    public void Validate_IdentificationNumber_TooShort_ReturnsError()
    {
        _validator.Validate("IdentificationNumber", "123", PNContext(minDocLength: 5))
            .Should().ContainSingle();
    }

    [Fact]
    public void Validate_IdentificationNumber_Valid_ReturnsEmpty()
    {
        _validator.Validate("IdentificationNumber", "12345678", PNContext(minDocLength: 5, identificationNumber: "12345678"))
            .Should().BeEmpty();
    }

    [Fact]
    public void Validate_IdentificationNumber_ExactMinLength_ReturnsEmpty()
    {
        _validator.Validate("IdentificationNumber", "12345", PNContext(minDocLength: 5, identificationNumber: "12345"))
            .Should().BeEmpty();
    }

    #endregion

    #region Validate — Name fields (PN mode)

    [Fact]
    public void Validate_FirstName_Empty_PN_ReturnsError()
    {
        _validator.Validate("FirstName", "", PNContext())
            .Should().ContainSingle().Which.Should().Contain("primer nombre");
    }

    [Fact]
    public void Validate_FirstName_Whitespace_PN_ReturnsError()
    {
        _validator.Validate("FirstName", "   ", PNContext())
            .Should().ContainSingle();
    }

    [Fact]
    public void Validate_FirstName_Valid_PN_ReturnsEmpty()
    {
        _validator.Validate("FirstName", "Carlos", PNContext(firstName: "Carlos"))
            .Should().BeEmpty();
    }

    [Fact]
    public void Validate_FirstName_Empty_PJ_ReturnsEmpty()
    {
        _validator.Validate("FirstName", "", PJContext())
            .Should().BeEmpty();
    }

    [Fact]
    public void Validate_FirstLastName_Empty_PN_ReturnsError()
    {
        _validator.Validate("FirstLastName", "", PNContext())
            .Should().ContainSingle().Which.Should().Contain("primer apellido");
    }

    [Fact]
    public void Validate_FirstLastName_Valid_PN_ReturnsEmpty()
    {
        _validator.Validate("FirstLastName", "Medrano", PNContext(firstLastName: "Medrano"))
            .Should().BeEmpty();
    }

    [Fact]
    public void Validate_FirstLastName_Empty_PJ_ReturnsEmpty()
    {
        _validator.Validate("FirstLastName", "", PJContext())
            .Should().BeEmpty();
    }

    #endregion

    #region Validate — Phone fields

    [Fact]
    public void Validate_PrimaryPhone_Not7Digits_ReturnsError()
    {
        _validator.Validate("PrimaryPhone", "12345", PNContext())
            .Should().ContainSingle().Which.Should().Contain("7 digitos");
    }

    [Fact]
    public void Validate_PrimaryPhone_7Digits_ReturnsEmpty()
    {
        _validator.Validate("PrimaryPhone", "1234567", PNContext())
            .Should().BeEmpty();
    }

    [Fact]
    public void Validate_PrimaryPhone_Empty_ReturnsEmpty()
    {
        _validator.Validate("PrimaryPhone", "", PNContext())
            .Should().BeEmpty();
    }

    [Fact]
    public void Validate_SecondaryPhone_Not7Digits_ReturnsError()
    {
        _validator.Validate("SecondaryPhone", "123", PNContext())
            .Should().ContainSingle().Which.Should().Contain("7 digitos");
    }

    [Fact]
    public void Validate_SecondaryPhone_7Digits_ReturnsEmpty()
    {
        _validator.Validate("SecondaryPhone", "1234567", PNContext())
            .Should().BeEmpty();
    }

    [Fact]
    public void Validate_PrimaryCellPhone_Not10Digits_ReturnsError()
    {
        _validator.Validate("PrimaryCellPhone", "12345", PNContext())
            .Should().ContainSingle().Which.Should().Contain("10 digitos");
    }

    [Fact]
    public void Validate_PrimaryCellPhone_10Digits_ReturnsEmpty()
    {
        _validator.Validate("PrimaryCellPhone", "1234567890", PNContext())
            .Should().BeEmpty();
    }

    [Fact]
    public void Validate_SecondaryCellPhone_Not10Digits_ReturnsError()
    {
        _validator.Validate("SecondaryCellPhone", "123456", PNContext())
            .Should().ContainSingle().Which.Should().Contain("10 digitos");
    }

    [Fact]
    public void Validate_SecondaryCellPhone_10Digits_ReturnsEmpty()
    {
        _validator.Validate("SecondaryCellPhone", "1234567890", PNContext())
            .Should().BeEmpty();
    }

    [Fact]
    public void Validate_Phone_CleansSpecialChars()
    {
        // Phone with spaces should be cleaned before validation (7 digits remain)
        _validator.Validate("PrimaryPhone", "123 4567", PNContext())
            .Should().BeEmpty("spaces are stripped, leaving 7 digits");
    }

    [Fact]
    public void Validate_Phone_CleansTabsAndHyphens()
    {
        // Tabs, commas, semicolons, hyphens, underscores are stripped
        _validator.Validate("PrimaryPhone", "123-4567", PNContext())
            .Should().BeEmpty("hyphens are stripped, leaving 7 digits");
    }

    [Fact]
    public void Validate_CellPhone_CleansSpaces()
    {
        _validator.Validate("PrimaryCellPhone", "123 456 78 90", PNContext())
            .Should().BeEmpty("spaces are stripped, leaving 10 digits");
    }

    #endregion

    #region Validate — Unknown property

    [Fact]
    public void Validate_UnknownProperty_ReturnsEmpty()
    {
        _validator.Validate("NonExistent", "", PNContext())
            .Should().BeEmpty();
    }

    #endregion

    #region ValidateAll

    [Fact]
    public void ValidateAll_PN_EmptyNames_ReturnsErrors()
    {
        SellerValidationContext context = new()
        {
            CaptureInfoAsPN = true,
            MinimumDocumentLength = 5
        };

        _validator.ValidateAll(context)
            .Should().ContainKey("FirstName")
            .And.ContainKey("FirstLastName")
            .And.ContainKey("IdentificationNumber");
    }

    [Fact]
    public void ValidateAll_PN_ValidData_ReturnsEmpty()
    {
        SellerValidationContext context = new()
        {
            CaptureInfoAsPN = true,
            MinimumDocumentLength = 5,
            FirstName = "Carlos",
            FirstLastName = "Medrano",
            IdentificationNumber = "12345678"
        };

        _validator.ValidateAll(context).Should().BeEmpty();
    }

    [Fact]
    public void ValidateAll_PJ_ReturnsEmpty_RegardlessOfNames()
    {
        SellerValidationContext context = new()
        {
            CaptureInfoAsPN = false,
            MinimumDocumentLength = 5
        };

        _validator.ValidateAll(context).Should().BeEmpty();
    }

    #endregion

    #region CanSave

    [Fact]
    public void CanSave_AllValid_PN_ReturnsTrue()
    {
        SellerCanSaveContext context = new()
        {
            IdentificationNumber = "12345678",
            MinimumDocumentLength = 5,
            CostCenterSelectedCount = 1,
            CaptureInfoAsPN = true,
            FirstName = "Carlos",
            FirstLastName = "Medrano",
            HasChanges = true,
            HasErrors = false
        };

        _validator.CanSave(context).Should().BeTrue();
    }

    [Fact]
    public void CanSave_AllValid_PJ_ReturnsTrue()
    {
        SellerCanSaveContext context = new()
        {
            IdentificationNumber = "900123456",
            MinimumDocumentLength = 5,
            CostCenterSelectedCount = 2,
            CaptureInfoAsPN = false,
            HasChanges = true,
            HasErrors = false
        };

        _validator.CanSave(context).Should().BeTrue();
    }

    [Fact]
    public void CanSave_EmptyIdentificationNumber_ReturnsFalse()
    {
        SellerCanSaveContext context = new()
        {
            IdentificationNumber = "",
            MinimumDocumentLength = 5,
            CostCenterSelectedCount = 1,
            HasChanges = true
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_NullIdentificationNumber_ReturnsFalse()
    {
        SellerCanSaveContext context = new()
        {
            IdentificationNumber = null,
            MinimumDocumentLength = 5,
            CostCenterSelectedCount = 1,
            HasChanges = true
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_IdentificationTooShort_ReturnsFalse()
    {
        SellerCanSaveContext context = new()
        {
            IdentificationNumber = "123",
            MinimumDocumentLength = 5,
            CostCenterSelectedCount = 1,
            HasChanges = true
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_NoCostCentersSelected_ReturnsFalse()
    {
        SellerCanSaveContext context = new()
        {
            IdentificationNumber = "12345678",
            MinimumDocumentLength = 5,
            CostCenterSelectedCount = 0,
            HasChanges = true
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_PN_EmptyFirstName_ReturnsFalse()
    {
        SellerCanSaveContext context = new()
        {
            IdentificationNumber = "12345678",
            MinimumDocumentLength = 5,
            CostCenterSelectedCount = 1,
            CaptureInfoAsPN = true,
            FirstName = "",
            FirstLastName = "Medrano",
            HasChanges = true
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_PN_EmptyFirstLastName_ReturnsFalse()
    {
        SellerCanSaveContext context = new()
        {
            IdentificationNumber = "12345678",
            MinimumDocumentLength = 5,
            CostCenterSelectedCount = 1,
            CaptureInfoAsPN = true,
            FirstName = "Carlos",
            FirstLastName = "",
            HasChanges = true
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_NoChanges_ReturnsFalse()
    {
        SellerCanSaveContext context = new()
        {
            IdentificationNumber = "12345678",
            MinimumDocumentLength = 5,
            CostCenterSelectedCount = 1,
            CaptureInfoAsPN = false,
            HasChanges = false,
            HasErrors = false
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_HasErrors_ReturnsFalse()
    {
        SellerCanSaveContext context = new()
        {
            IdentificationNumber = "12345678",
            MinimumDocumentLength = 5,
            CostCenterSelectedCount = 1,
            CaptureInfoAsPN = false,
            HasChanges = true,
            HasErrors = true
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_PJ_EmptyNames_StillReturnsTrue()
    {
        SellerCanSaveContext context = new()
        {
            IdentificationNumber = "900123456",
            MinimumDocumentLength = 5,
            CostCenterSelectedCount = 1,
            CaptureInfoAsPN = false,
            FirstName = "",
            FirstLastName = "",
            HasChanges = true,
            HasErrors = false
        };

        _validator.CanSave(context).Should().BeTrue("PJ mode does not require names");
    }

    #endregion
}
