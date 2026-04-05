using FluentAssertions;
using NetErp.Billing.Customers.Validators;
using Xunit;

namespace NetErp.Tests.Validators;

public class CustomerValidatorTests
{
    private readonly CustomerValidator _validator = new();

    private static CustomerValidationContext PNContext(bool isActive = true, int minDocLength = 5) => new()
    {
        CaptureInfoAsPN = true,
        CaptureInfoAsPJ = false,
        IsActive = isActive,
        MinimumDocumentLength = minDocLength
    };

    private static CustomerValidationContext PJContext(bool isActive = true, int minDocLength = 5) => new()
    {
        CaptureInfoAsPN = false,
        CaptureInfoAsPJ = true,
        IsActive = isActive,
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
    public void Validate_IdentificationNumber_TooShort_ReturnsError()
    {
        _validator.Validate("IdentificationNumber", "123", PNContext(minDocLength: 5))
            .Should().ContainSingle();
    }

    [Fact]
    public void Validate_IdentificationNumber_Valid_ReturnsEmpty()
    {
        _validator.Validate("IdentificationNumber", "12345678", PNContext(minDocLength: 5))
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

    #endregion

    #region Validate — BusinessName (PJ mode)

    [Fact]
    public void Validate_BusinessName_Empty_PJ_ReturnsError()
    {
        _validator.Validate("BusinessName", "", PJContext())
            .Should().ContainSingle().Which.Should().Contain("razón social");
    }

    [Fact]
    public void Validate_BusinessName_Empty_PN_ReturnsEmpty()
    {
        _validator.Validate("BusinessName", "", PNContext())
            .Should().BeEmpty();
    }

    #endregion

    #region Validate — BlockingReason

    [Fact]
    public void Validate_BlockingReason_Empty_WhenInactive_ReturnsError()
    {
        _validator.Validate("BlockingReason", "", PNContext(isActive: false))
            .Should().ContainSingle().Which.Should().Contain("motivo de bloqueo");
    }

    [Fact]
    public void Validate_BlockingReason_Empty_WhenActive_ReturnsEmpty()
    {
        _validator.Validate("BlockingReason", "", PNContext(isActive: true))
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
    public void Validate_Phone_CleansSpecialChars()
    {
        // Phone with spaces/tabs/commas/semicolons/hyphens should be cleaned before validation
        _validator.Validate("PrimaryPhone", "123 4567", PNContext())
            .Should().BeEmpty("spaces are stripped, leaving 7 digits");
    }

    #endregion

    #region Validate — UnknownProperty

    [Fact]
    public void Validate_UnknownProperty_ReturnsEmpty()
    {
        _validator.Validate("NonExistent", "", PNContext())
            .Should().BeEmpty();
    }

    #endregion

    #region ValidateSelection

    [Fact]
    public void ValidateSelection_SelectedCountry_Null_ReturnsError()
    {
        _validator.ValidateSelection("SelectedCountry", null)
            .Should().ContainSingle().Which.Should().Contain("país");
    }

    [Fact]
    public void ValidateSelection_SelectedCountry_NotNull_ReturnsEmpty()
    {
        _validator.ValidateSelection("SelectedCountry", new object())
            .Should().BeEmpty();
    }

    [Fact]
    public void ValidateSelection_SelectedDepartment_Null_ReturnsError()
    {
        _validator.ValidateSelection("SelectedDepartment", null)
            .Should().ContainSingle().Which.Should().Contain("departamento");
    }

    [Fact]
    public void ValidateSelection_SelectedCityId_Zero_ReturnsError()
    {
        _validator.ValidateSelection("SelectedCityId", 0)
            .Should().ContainSingle().Which.Should().Contain("municipio");
    }

    [Fact]
    public void ValidateSelection_SelectedCityId_NonZero_ReturnsEmpty()
    {
        _validator.ValidateSelection("SelectedCityId", 42)
            .Should().BeEmpty();
    }

    #endregion

    #region ValidateAll

    [Fact]
    public void ValidateAll_PJ_EmptyBusinessName_ReturnsError()
    {
        CustomerValidationContext context = PJContext();
        var result = _validator.ValidateAll(context);

        result.Should().ContainKey("BusinessName");
    }

    [Fact]
    public void ValidateAll_PN_EmptyNames_ReturnsErrors()
    {
        CustomerValidationContext context = PNContext();
        var result = _validator.ValidateAll(context);

        result.Should().ContainKey("FirstName");
        result.Should().ContainKey("FirstLastName");
        result.Should().ContainKey("IdentificationNumber");
    }

    [Fact]
    public void ValidateAll_PN_ValidData_ReturnsEmpty()
    {
        CustomerValidationContext context = new()
        {
            CaptureInfoAsPN = true,
            CaptureInfoAsPJ = false,
            IsActive = true,
            MinimumDocumentLength = 5,
            FirstName = "Carlos",
            FirstLastName = "Medrano",
            IdentificationNumber = "12345678"
        };

        _validator.ValidateAll(context).Should().BeEmpty();
    }

    #endregion

    #region CanSave

    [Fact]
    public void CanSave_AllValid_ReturnsTrue()
    {
        var context = new CustomerCanSaveContext
        {
            SelectedIdentificationType = new object(),
            IdentificationNumber = "12345678",
            MinimumDocumentLength = 5,
            HasVerificationDigit = false,
            CaptureInfoAsPJ = true,
            CaptureInfoAsPN = false,
            BusinessName = "Mi Empresa",
            SelectedCountry = new object(),
            SelectedDepartment = new object(),
            SelectedCityId = 1,
            HasErrors = false,
            IsNewRecord = true,
            HasChanges = true,
            HasEmailChanges = false
        };

        _validator.CanSave(context).Should().BeTrue();
    }

    [Fact]
    public void CanSave_NoIdentificationType_ReturnsFalse()
    {
        var context = new CustomerCanSaveContext
        {
            SelectedIdentificationType = null,
            IdentificationNumber = "12345678",
            MinimumDocumentLength = 5
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_EmptyIdentificationNumber_ReturnsFalse()
    {
        var context = new CustomerCanSaveContext
        {
            SelectedIdentificationType = new object(),
            IdentificationNumber = "",
            MinimumDocumentLength = 5
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_NoCountry_ReturnsFalse()
    {
        var context = new CustomerCanSaveContext
        {
            SelectedIdentificationType = new object(),
            IdentificationNumber = "12345678",
            MinimumDocumentLength = 5,
            CaptureInfoAsPJ = true,
            BusinessName = "Empresa",
            SelectedCountry = null
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_HasErrors_ReturnsFalse()
    {
        var context = new CustomerCanSaveContext
        {
            SelectedIdentificationType = new object(),
            IdentificationNumber = "12345678",
            MinimumDocumentLength = 5,
            CaptureInfoAsPJ = true,
            BusinessName = "Empresa",
            SelectedCountry = new object(),
            SelectedDepartment = new object(),
            SelectedCityId = 1,
            HasErrors = true
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_ExistingRecord_NoChanges_NoEmailChanges_ReturnsFalse()
    {
        var context = new CustomerCanSaveContext
        {
            SelectedIdentificationType = new object(),
            IdentificationNumber = "12345678",
            MinimumDocumentLength = 5,
            CaptureInfoAsPJ = true,
            BusinessName = "Empresa",
            SelectedCountry = new object(),
            SelectedDepartment = new object(),
            SelectedCityId = 1,
            HasErrors = false,
            IsNewRecord = false,
            HasChanges = false,
            HasEmailChanges = false
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_ExistingRecord_HasEmailChanges_ReturnsTrue()
    {
        var context = new CustomerCanSaveContext
        {
            SelectedIdentificationType = new object(),
            IdentificationNumber = "12345678",
            MinimumDocumentLength = 5,
            CaptureInfoAsPJ = true,
            BusinessName = "Empresa",
            SelectedCountry = new object(),
            SelectedDepartment = new object(),
            SelectedCityId = 1,
            HasErrors = false,
            IsNewRecord = false,
            HasChanges = false,
            HasEmailChanges = true
        };

        _validator.CanSave(context).Should().BeTrue();
    }

    [Fact]
    public void CanSave_PJ_EmptyBusinessName_ReturnsFalse()
    {
        var context = new CustomerCanSaveContext
        {
            SelectedIdentificationType = new object(),
            IdentificationNumber = "12345678",
            MinimumDocumentLength = 5,
            CaptureInfoAsPJ = true,
            BusinessName = "",
            SelectedCountry = new object(),
            SelectedDepartment = new object(),
            SelectedCityId = 1,
            IsNewRecord = true,
            HasChanges = true
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    [Fact]
    public void CanSave_PN_EmptyFirstName_ReturnsFalse()
    {
        var context = new CustomerCanSaveContext
        {
            SelectedIdentificationType = new object(),
            IdentificationNumber = "12345678",
            MinimumDocumentLength = 5,
            CaptureInfoAsPN = true,
            FirstName = "",
            FirstLastName = "Medrano",
            SelectedCountry = new object(),
            SelectedDepartment = new object(),
            SelectedCityId = 1,
            IsNewRecord = true,
            HasChanges = true
        };

        _validator.CanSave(context).Should().BeFalse();
    }

    #endregion
}
