using FluentAssertions;
using NetErp.Suppliers.Suppliers.Validators;
using Xunit;

namespace NetErp.Tests.Validators;

/// <summary>
/// Tests puros del validador — new SupplierValidator() + assert. Cero mocks.
/// </summary>
public class SupplierValidatorTests
{
    private readonly SupplierValidator _validator = new();

    private static SupplierValidationContext PNContext(
        int minDocLength = 5,
        string? firstName = null,
        string? firstLastName = null,
        string? identificationNumber = null,
        decimal icaWithholdingRate = 0m) => new()
    {
        CaptureInfoAsPN = true,
        CaptureInfoAsPJ = false,
        MinimumDocumentLength = minDocLength,
        FirstName = firstName,
        FirstLastName = firstLastName,
        IdentificationNumber = identificationNumber,
        IcaWithholdingRate = icaWithholdingRate
    };

    private static SupplierValidationContext PJContext(
        int minDocLength = 5,
        string? businessName = null,
        string? identificationNumber = null,
        decimal icaWithholdingRate = 0m) => new()
    {
        CaptureInfoAsPN = false,
        CaptureInfoAsPJ = true,
        MinimumDocumentLength = minDocLength,
        BusinessName = businessName,
        IdentificationNumber = identificationNumber,
        IcaWithholdingRate = icaWithholdingRate
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
        _validator.Validate("IdentificationNumber", "123", PNContext(minDocLength: 5, identificationNumber: "123"))
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

    #region Validate — BusinessName (PJ mode)

    [Fact]
    public void Validate_BusinessName_Empty_PJ_ReturnsError()
    {
        _validator.Validate("BusinessName", "", PJContext())
            .Should().ContainSingle().Which.Should().Contain("razón social");
    }

    [Fact]
    public void Validate_BusinessName_Whitespace_PJ_ReturnsError()
    {
        _validator.Validate("BusinessName", "   ", PJContext())
            .Should().ContainSingle();
    }

    [Fact]
    public void Validate_BusinessName_Valid_PJ_ReturnsEmpty()
    {
        _validator.Validate("BusinessName", "Acme S.A.", PJContext(businessName: "Acme S.A."))
            .Should().BeEmpty();
    }

    [Fact]
    public void Validate_BusinessName_Empty_PN_ReturnsEmpty()
    {
        _validator.Validate("BusinessName", "", PNContext())
            .Should().BeEmpty();
    }

    #endregion

    #region Validate — Phone fields

    [Fact]
    public void Validate_PrimaryPhone_Not7Digits_ReturnsError()
    {
        _validator.Validate("PrimaryPhone", "12345", PNContext())
            .Should().ContainSingle().Which.Should().Contain("7 dígitos");
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
            .Should().ContainSingle().Which.Should().Contain("7 dígitos");
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
            .Should().ContainSingle().Which.Should().Contain("10 dígitos");
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
            .Should().ContainSingle().Which.Should().Contain("10 dígitos");
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
        _validator.Validate("PrimaryPhone", "123 4567", PNContext())
            .Should().BeEmpty("spaces are stripped, leaving 7 digits");
    }

    [Fact]
    public void Validate_Phone_CleansHyphens()
    {
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

    #region Validate — IcaWithholdingRate (decimal overload)

    [Fact]
    public void Validate_IcaWithholdingRate_Negative_ReturnsError()
    {
        _validator.Validate("IcaWithholdingRate", -0.01m, PNContext())
            .Should().ContainSingle().Which.Should().Contain("0 y 100");
    }

    [Fact]
    public void Validate_IcaWithholdingRate_GreaterThan100_ReturnsError()
    {
        _validator.Validate("IcaWithholdingRate", 100.01m, PNContext())
            .Should().ContainSingle().Which.Should().Contain("0 y 100");
    }

    [Fact]
    public void Validate_IcaWithholdingRate_Zero_ReturnsEmpty()
    {
        _validator.Validate("IcaWithholdingRate", 0m, PNContext())
            .Should().BeEmpty();
    }

    [Fact]
    public void Validate_IcaWithholdingRate_OneHundred_ReturnsEmpty()
    {
        _validator.Validate("IcaWithholdingRate", 100m, PNContext())
            .Should().BeEmpty();
    }

    [Fact]
    public void Validate_IcaWithholdingRate_MidRange_ReturnsEmpty()
    {
        _validator.Validate("IcaWithholdingRate", 5.5m, PNContext())
            .Should().BeEmpty();
    }

    #endregion

    #region Validate — Unknown property

    [Fact]
    public void Validate_UnknownProperty_String_ReturnsEmpty()
    {
        _validator.Validate("NonExistent", "", PNContext())
            .Should().BeEmpty();
    }

    [Fact]
    public void Validate_UnknownProperty_Decimal_ReturnsEmpty()
    {
        _validator.Validate("NonExistent", 1m, PNContext())
            .Should().BeEmpty();
    }

    #endregion

    #region ValidateAll

    [Fact]
    public void ValidateAll_PN_EmptyNames_ReturnsErrors()
    {
        SupplierValidationContext context = new()
        {
            CaptureInfoAsPN = true,
            CaptureInfoAsPJ = false,
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
        SupplierValidationContext context = new()
        {
            CaptureInfoAsPN = true,
            CaptureInfoAsPJ = false,
            MinimumDocumentLength = 5,
            FirstName = "Carlos",
            FirstLastName = "Medrano",
            IdentificationNumber = "12345678"
        };

        _validator.ValidateAll(context).Should().BeEmpty();
    }

    [Fact]
    public void ValidateAll_PJ_EmptyBusinessName_ReturnsError()
    {
        SupplierValidationContext context = new()
        {
            CaptureInfoAsPN = false,
            CaptureInfoAsPJ = true,
            MinimumDocumentLength = 5,
            IdentificationNumber = "900123456"
        };

        _validator.ValidateAll(context)
            .Should().ContainKey("BusinessName");
    }

    [Fact]
    public void ValidateAll_PJ_ValidData_ReturnsEmpty()
    {
        SupplierValidationContext context = new()
        {
            CaptureInfoAsPN = false,
            CaptureInfoAsPJ = true,
            MinimumDocumentLength = 5,
            BusinessName = "Acme S.A.",
            IdentificationNumber = "900123456"
        };

        _validator.ValidateAll(context).Should().BeEmpty();
    }

    [Fact]
    public void ValidateAll_IcaWithholdingRateOutOfRange_ReturnsError()
    {
        SupplierValidationContext context = new()
        {
            CaptureInfoAsPN = false,
            CaptureInfoAsPJ = true,
            MinimumDocumentLength = 5,
            BusinessName = "Acme S.A.",
            IdentificationNumber = "900123456",
            IcaWithholdingRate = 150m
        };

        _validator.ValidateAll(context)
            .Should().ContainKey("IcaWithholdingRate");
    }

    #endregion

    #region CanSave

    [Fact]
    public void CanSave_AllValid_PN_ReturnsTrue()
    {
        SupplierCanSaveContext context = new()
        {
            IdentificationNumber = "12345678",
            MinimumDocumentLength = 5,
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
        SupplierCanSaveContext context = new()
        {
            IdentificationNumber = "900123456",
            MinimumDocumentLength = 5,
            CaptureInfoAsPJ = true,
            BusinessName = "Acme S.A.",
            HasChanges = true,
            HasErrors = false
        };

        _validator.CanSave(context).Should().BeTrue();
    }

    [Fact]
    public void CanSave_IsBusy_ReturnsFalse()
    {
        SupplierCanSaveContext context = new()
        {
            IsBusy = true,
            IdentificationNumber = "12345678",
            MinimumDocumentLength = 5,
            CaptureInfoAsPJ = true,
            BusinessName = "Acme",
            HasChanges = true,
            HasErrors = false
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_MinimumDocumentLengthZero_ReturnsFalse()
    {
        SupplierCanSaveContext context = new()
        {
            IdentificationNumber = "12345678",
            MinimumDocumentLength = 0,
            CaptureInfoAsPJ = true,
            BusinessName = "Acme",
            HasChanges = true
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_EmptyIdentificationNumber_ReturnsFalse()
    {
        SupplierCanSaveContext context = new()
        {
            IdentificationNumber = "",
            MinimumDocumentLength = 5,
            HasChanges = true
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_NullIdentificationNumber_ReturnsFalse()
    {
        SupplierCanSaveContext context = new()
        {
            IdentificationNumber = null,
            MinimumDocumentLength = 5,
            HasChanges = true
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_IdentificationTooShort_ReturnsFalse()
    {
        SupplierCanSaveContext context = new()
        {
            IdentificationNumber = "123",
            MinimumDocumentLength = 5,
            HasChanges = true
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_HasVerificationDigit_Missing_ReturnsFalse()
    {
        SupplierCanSaveContext context = new()
        {
            IdentificationNumber = "900123456",
            MinimumDocumentLength = 5,
            HasVerificationDigit = true,
            VerificationDigit = "",
            CaptureInfoAsPJ = true,
            BusinessName = "Acme",
            HasChanges = true
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_HasVerificationDigit_Provided_ReturnsTrue()
    {
        SupplierCanSaveContext context = new()
        {
            IdentificationNumber = "900123456",
            MinimumDocumentLength = 5,
            HasVerificationDigit = true,
            VerificationDigit = "7",
            CaptureInfoAsPJ = true,
            BusinessName = "Acme",
            HasChanges = true,
            HasErrors = false
        };

        _validator.CanSave(context).Should().BeTrue();
    }

    [Fact]
    public void CanSave_PJ_EmptyBusinessName_ReturnsFalse()
    {
        SupplierCanSaveContext context = new()
        {
            IdentificationNumber = "900123456",
            MinimumDocumentLength = 5,
            CaptureInfoAsPJ = true,
            BusinessName = "",
            HasChanges = true
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_PN_EmptyFirstName_ReturnsFalse()
    {
        SupplierCanSaveContext context = new()
        {
            IdentificationNumber = "12345678",
            MinimumDocumentLength = 5,
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
        SupplierCanSaveContext context = new()
        {
            IdentificationNumber = "12345678",
            MinimumDocumentLength = 5,
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
        SupplierCanSaveContext context = new()
        {
            IdentificationNumber = "900123456",
            MinimumDocumentLength = 5,
            CaptureInfoAsPJ = true,
            BusinessName = "Acme",
            HasChanges = false,
            HasErrors = false
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_HasErrors_ReturnsFalse()
    {
        SupplierCanSaveContext context = new()
        {
            IdentificationNumber = "900123456",
            MinimumDocumentLength = 5,
            CaptureInfoAsPJ = true,
            BusinessName = "Acme",
            HasChanges = true,
            HasErrors = true
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    #endregion
}
