using FluentAssertions;
using NetErp.Books.AccountingEntities.Validators;
using Xunit;

namespace NetErp.Tests.Validators;

/// <summary>
/// Tests puros del validador — new AccountingEntityValidator() + assert. Cero mocks.
/// </summary>
public class AccountingEntityValidatorTests
{
    private readonly AccountingEntityValidator _validator = new();

    #region Context builders

    private static AccountingEntityValidationContext PNContext(
        int minDocLength = 5,
        string? firstName = null,
        string? firstLastName = null,
        string? identificationNumber = null) => new()
        {
            CaptureInfoAsPN = true,
            CaptureInfoAsPJ = false,
            MinimumDocumentLength = minDocLength,
            FirstName = firstName,
            FirstLastName = firstLastName,
            IdentificationNumber = identificationNumber
        };

    private static AccountingEntityValidationContext PJContext(
        int minDocLength = 5,
        string? businessName = null,
        string? identificationNumber = null) => new()
        {
            CaptureInfoAsPN = false,
            CaptureInfoAsPJ = true,
            MinimumDocumentLength = minDocLength,
            BusinessName = businessName,
            IdentificationNumber = identificationNumber
        };

    private static AccountingEntityCanSaveContext PNCanSaveContext(
        bool isBusy = false,
        bool isNewRecord = false,
        int minDocLength = 5,
        bool hasVerificationDigit = false,
        string? identificationNumber = "12345678",
        string? verificationDigit = null,
        string? firstName = "Carlos",
        string? firstLastName = "Medrano",
        bool hasCountry = true,
        bool hasDepartment = true,
        bool hasCity = true,
        bool hasChanges = true,
        bool hasEmailChanges = false,
        bool hasErrors = false) => new()
        {
            IsBusy = isBusy,
            IsNewRecord = isNewRecord,
            MinimumDocumentLength = minDocLength,
            HasVerificationDigit = hasVerificationDigit,
            IdentificationNumber = identificationNumber,
            VerificationDigit = verificationDigit,
            CaptureInfoAsPN = true,
            CaptureInfoAsPJ = false,
            FirstName = firstName,
            FirstLastName = firstLastName,
            HasCountry = hasCountry,
            HasDepartment = hasDepartment,
            HasCity = hasCity,
            HasChanges = hasChanges,
            HasEmailChanges = hasEmailChanges,
            HasErrors = hasErrors
        };

    private static AccountingEntityCanSaveContext PJCanSaveContext(
        bool isBusy = false,
        bool isNewRecord = false,
        int minDocLength = 5,
        bool hasVerificationDigit = false,
        string? identificationNumber = "900123456",
        string? verificationDigit = null,
        string? businessName = "Acme S.A.",
        bool hasCountry = true,
        bool hasDepartment = true,
        bool hasCity = true,
        bool hasChanges = true,
        bool hasEmailChanges = false,
        bool hasErrors = false) => new()
        {
            IsBusy = isBusy,
            IsNewRecord = isNewRecord,
            MinimumDocumentLength = minDocLength,
            HasVerificationDigit = hasVerificationDigit,
            IdentificationNumber = identificationNumber,
            VerificationDigit = verificationDigit,
            CaptureInfoAsPN = false,
            CaptureInfoAsPJ = true,
            BusinessName = businessName,
            HasCountry = hasCountry,
            HasDepartment = hasDepartment,
            HasCity = hasCity,
            HasChanges = hasChanges,
            HasEmailChanges = hasEmailChanges,
            HasErrors = hasErrors
        };

    #endregion

    #region Validate — IdentificationNumber

    [Fact]
    public void Validate_IdentificationNumber_Empty_ReturnsError()
    {
        _validator.Validate("IdentificationNumber", "", PNContext())
            .Should().ContainSingle().Which.Should().Contain("vacío");
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
            .Should().ContainSingle().Which.Should().Contain("al menos");
    }

    [Fact]
    public void Validate_IdentificationNumber_ExactMinLength_ReturnsEmpty()
    {
        _validator.Validate("IdentificationNumber", "12345", PNContext(minDocLength: 5, identificationNumber: "12345"))
            .Should().BeEmpty();
    }

    [Fact]
    public void Validate_IdentificationNumber_LongerThanMin_ReturnsEmpty()
    {
        _validator.Validate("IdentificationNumber", "12345678", PNContext(minDocLength: 5, identificationNumber: "12345678"))
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
    public void Validate_Phone_CleansSpaces()
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
    public void Validate_CellPhone_CleansFormatting()
    {
        _validator.Validate("PrimaryCellPhone", "123 456 78 90", PNContext())
            .Should().BeEmpty("spaces are stripped, leaving 10 digits");
    }

    #endregion

    #region Validate — Unknown property

    [Fact]
    public void Validate_UnknownProperty_ReturnsEmpty()
    {
        _validator.Validate("NonExistent", "anything", PNContext())
            .Should().BeEmpty();
    }

    #endregion

    #region ValidateAll

    [Fact]
    public void ValidateAll_PN_AllEmpty_ReturnsMultipleErrors()
    {
        AccountingEntityValidationContext context = PNContext();

        var result = _validator.ValidateAll(context);

        result.Should().ContainKey("FirstName");
        result.Should().ContainKey("FirstLastName");
        result.Should().ContainKey("IdentificationNumber");
    }

    [Fact]
    public void ValidateAll_PN_Valid_ReturnsEmpty()
    {
        AccountingEntityValidationContext context = PNContext(
            firstName: "Carlos",
            firstLastName: "Medrano",
            identificationNumber: "12345678");

        _validator.ValidateAll(context).Should().BeEmpty();
    }

    [Fact]
    public void ValidateAll_PJ_EmptyBusinessName_ReturnsError()
    {
        AccountingEntityValidationContext context = PJContext(identificationNumber: "900123456");

        _validator.ValidateAll(context).Should().ContainKey("BusinessName");
    }

    [Fact]
    public void ValidateAll_PJ_Valid_ReturnsEmpty()
    {
        AccountingEntityValidationContext context = PJContext(
            businessName: "Acme S.A.",
            identificationNumber: "900123456");

        _validator.ValidateAll(context).Should().BeEmpty();
    }

    [Fact]
    public void ValidateAll_PJ_DoesNotValidateNameFields()
    {
        AccountingEntityValidationContext context = PJContext(
            businessName: "Acme S.A.",
            identificationNumber: "900123456");

        var result = _validator.ValidateAll(context);
        result.Should().NotContainKey("FirstName");
        result.Should().NotContainKey("FirstLastName");
    }

    #endregion

    #region CanSave — PN mode

    [Fact]
    public void CanSave_PN_AllValid_ReturnsTrue()
    {
        _validator.CanSave(PNCanSaveContext()).Should().BeTrue();
    }

    [Fact]
    public void CanSave_PN_IsBusy_ReturnsFalse()
    {
        _validator.CanSave(PNCanSaveContext(isBusy: true)).Should().BeFalse();
    }

    [Fact]
    public void CanSave_PN_NoIdentificationType_ReturnsFalse()
    {
        _validator.CanSave(PNCanSaveContext(minDocLength: 0)).Should().BeFalse();
    }

    [Fact]
    public void CanSave_PN_EmptyIdentification_ReturnsFalse()
    {
        _validator.CanSave(PNCanSaveContext(identificationNumber: "")).Should().BeFalse();
    }

    [Fact]
    public void CanSave_PN_NullIdentification_ReturnsFalse()
    {
        _validator.CanSave(PNCanSaveContext(identificationNumber: null)).Should().BeFalse();
    }

    [Fact]
    public void CanSave_PN_IdentificationTooShort_ReturnsFalse()
    {
        _validator.CanSave(PNCanSaveContext(identificationNumber: "123")).Should().BeFalse();
    }

    [Fact]
    public void CanSave_PN_RequiresVerificationDigit_Missing_ReturnsFalse()
    {
        _validator.CanSave(PNCanSaveContext(hasVerificationDigit: true, verificationDigit: ""))
            .Should().BeFalse();
    }

    [Fact]
    public void CanSave_PN_RequiresVerificationDigit_Provided_ReturnsTrue()
    {
        _validator.CanSave(PNCanSaveContext(hasVerificationDigit: true, verificationDigit: "7"))
            .Should().BeTrue();
    }

    [Fact]
    public void CanSave_PN_EmptyFirstName_ReturnsFalse()
    {
        _validator.CanSave(PNCanSaveContext(firstName: "")).Should().BeFalse();
    }

    [Fact]
    public void CanSave_PN_EmptyFirstLastName_ReturnsFalse()
    {
        _validator.CanSave(PNCanSaveContext(firstLastName: "")).Should().BeFalse();
    }

    [Fact]
    public void CanSave_PN_NoCountry_ReturnsFalse()
    {
        _validator.CanSave(PNCanSaveContext(hasCountry: false)).Should().BeFalse();
    }

    [Fact]
    public void CanSave_PN_NoDepartment_ReturnsFalse()
    {
        _validator.CanSave(PNCanSaveContext(hasDepartment: false)).Should().BeFalse();
    }

    [Fact]
    public void CanSave_PN_NoCity_ReturnsFalse()
    {
        _validator.CanSave(PNCanSaveContext(hasCity: false)).Should().BeFalse();
    }

    [Fact]
    public void CanSave_PN_HasErrors_ReturnsFalse()
    {
        _validator.CanSave(PNCanSaveContext(hasErrors: true)).Should().BeFalse();
    }

    #endregion

    #region CanSave — PJ mode

    [Fact]
    public void CanSave_PJ_AllValid_ReturnsTrue()
    {
        _validator.CanSave(PJCanSaveContext()).Should().BeTrue();
    }

    [Fact]
    public void CanSave_PJ_EmptyBusinessName_ReturnsFalse()
    {
        _validator.CanSave(PJCanSaveContext(businessName: "")).Should().BeFalse();
    }

    [Fact]
    public void CanSave_PJ_NullBusinessName_ReturnsFalse()
    {
        _validator.CanSave(PJCanSaveContext(businessName: null)).Should().BeFalse();
    }

    [Fact]
    public void CanSave_PJ_NoCountry_ReturnsFalse()
    {
        _validator.CanSave(PJCanSaveContext(hasCountry: false)).Should().BeFalse();
    }

    [Fact]
    public void CanSave_PJ_NoDepartment_ReturnsFalse()
    {
        _validator.CanSave(PJCanSaveContext(hasDepartment: false)).Should().BeFalse();
    }

    [Fact]
    public void CanSave_PJ_NoCity_ReturnsFalse()
    {
        _validator.CanSave(PJCanSaveContext(hasCity: false)).Should().BeFalse();
    }

    #endregion

    #region CanSave — NewRecord vs Edit changes/emails

    [Fact]
    public void CanSave_NewRecord_NoChanges_NoEmailChanges_ReturnsTrue()
    {
        // New records don't need HasChanges/HasEmailChanges to be true
        _validator.CanSave(PNCanSaveContext(
            isNewRecord: true, hasChanges: false, hasEmailChanges: false))
            .Should().BeTrue();
    }

    [Fact]
    public void CanSave_EditRecord_NoChangesAndNoEmailChanges_ReturnsFalse()
    {
        _validator.CanSave(PNCanSaveContext(
            isNewRecord: false, hasChanges: false, hasEmailChanges: false))
            .Should().BeFalse();
    }

    [Fact]
    public void CanSave_EditRecord_HasChanges_ReturnsTrue()
    {
        _validator.CanSave(PNCanSaveContext(
            isNewRecord: false, hasChanges: true, hasEmailChanges: false))
            .Should().BeTrue();
    }

    [Fact]
    public void CanSave_EditRecord_HasEmailChangesOnly_ReturnsTrue()
    {
        _validator.CanSave(PNCanSaveContext(
            isNewRecord: false, hasChanges: false, hasEmailChanges: true))
            .Should().BeTrue();
    }

    [Fact]
    public void CanSave_EditRecord_HasBothChangesAndEmailChanges_ReturnsTrue()
    {
        _validator.CanSave(PNCanSaveContext(
            isNewRecord: false, hasChanges: true, hasEmailChanges: true))
            .Should().BeTrue();
    }

    #endregion
}
