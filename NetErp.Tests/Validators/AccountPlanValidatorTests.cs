using System.Collections.Generic;
using FluentAssertions;
using NetErp.Books.AccountingAccounts.Validators;
using Xunit;

namespace NetErp.Tests.Validators;

/// <summary>
/// Tests puros del validador — new AccountPlanValidator() + assert. Cero mocks.
/// </summary>
public class AccountPlanValidatorTests
{
    private readonly AccountPlanValidator _validator = new();

    #region Context builders

    private static AccountPlanValidationContext NewContext(
        string lv5Code = "11050501",
        string lv1Name = "ACTIVO",
        string lv2Name = "DISPONIBLE",
        string lv3Name = "CAJA",
        string lv4Name = "CAJA GENERAL",
        string lv5Name = "CAJA PRINCIPAL",
        decimal margin = 0m,
        bool requiresMargin = false,
        HashSet<string>? existingCodes = null) => new()
        {
            IsNewRecord = true,
            CodeLength = lv5Code.Length,
            Lv5Code = lv5Code,
            Lv1Name = lv1Name,
            Lv2Name = lv2Name,
            Lv3Name = lv3Name,
            Lv4Name = lv4Name,
            Lv5Name = lv5Name,
            Margin = margin,
            RequiresMargin = requiresMargin,
            ExistingCodes = existingCodes ?? []
        };

    private static AccountPlanValidationContext EditContext(
        int codeLength,
        string lv1Name = "",
        string lv2Name = "",
        string lv3Name = "",
        string lv4Name = "",
        string lv5Name = "",
        string lv5Code = "",
        decimal margin = 0m,
        bool requiresMargin = false) => new()
        {
            IsNewRecord = false,
            CodeLength = codeLength,
            Lv5Code = lv5Code,
            Lv1Name = lv1Name,
            Lv2Name = lv2Name,
            Lv3Name = lv3Name,
            Lv4Name = lv4Name,
            Lv5Name = lv5Name,
            Margin = margin,
            RequiresMargin = requiresMargin,
            ExistingCodes = []
        };

    private static AccountPlanCanSaveContext NewCanSaveContext(
        bool isBusy = false,
        string lv5Code = "11050501",
        string lv1Name = "ACTIVO",
        string lv2Name = "DISPONIBLE",
        string lv3Name = "CAJA",
        string lv4Name = "CAJA GENERAL",
        string lv5Name = "CAJA PRINCIPAL",
        decimal margin = 0m,
        bool requiresMargin = false,
        HashSet<string>? existingCodes = null) => new()
        {
            IsBusy = isBusy,
            IsNewRecord = true,
            CodeLength = lv5Code.Length,
            Lv5Code = lv5Code,
            Lv1Name = lv1Name,
            Lv2Name = lv2Name,
            Lv3Name = lv3Name,
            Lv4Name = lv4Name,
            Lv5Name = lv5Name,
            Margin = margin,
            RequiresMargin = requiresMargin,
            ExistingCodes = existingCodes ?? []
        };

    private static AccountPlanCanSaveContext EditCanSaveContext(
        int codeLength,
        bool isBusy = false,
        string lv1Name = "",
        string lv2Name = "",
        string lv3Name = "",
        string lv4Name = "",
        string lv5Name = "",
        string lv5Code = "",
        decimal margin = 0m,
        bool requiresMargin = false) => new()
        {
            IsBusy = isBusy,
            IsNewRecord = false,
            CodeLength = codeLength,
            Lv5Code = lv5Code,
            Lv1Name = lv1Name,
            Lv2Name = lv2Name,
            Lv3Name = lv3Name,
            Lv4Name = lv4Name,
            Lv5Name = lv5Name,
            Margin = margin,
            RequiresMargin = requiresMargin,
            ExistingCodes = []
        };

    #endregion

    #region Validate — Lv5Code (new mode)

    [Fact]
    public void Validate_Lv5Code_New_Empty_ReturnsError()
    {
        _validator.Validate("Lv5Code", "", NewContext(lv5Code: ""))
            .Should().ContainSingle().Which.Should().Contain("vacío");
    }

    [Fact]
    public void Validate_Lv5Code_New_Null_ReturnsError()
    {
        _validator.Validate("Lv5Code", null, NewContext(lv5Code: ""))
            .Should().ContainSingle();
    }

    [Fact]
    public void Validate_Lv5Code_New_TooShort_ReturnsError()
    {
        _validator.Validate("Lv5Code", "1105", NewContext(lv5Code: "1105"))
            .Should().ContainSingle().Which.Should().Contain("8 dígitos");
    }

    [Fact]
    public void Validate_Lv5Code_New_Duplicate_ReturnsError()
    {
        HashSet<string> existing = ["11050501"];
        _validator.Validate("Lv5Code", "11050501", NewContext(lv5Code: "11050501", existingCodes: existing))
            .Should().ContainSingle().Which.Should().Contain("existe");
    }

    [Fact]
    public void Validate_Lv5Code_New_Valid_ReturnsEmpty()
    {
        _validator.Validate("Lv5Code", "11050501", NewContext(lv5Code: "11050501"))
            .Should().BeEmpty();
    }

    [Fact]
    public void Validate_Lv5Code_Edit_IgnoredRegardlessOfValue()
    {
        // En edit mode el validador no corre reglas sobre Lv5Code desde Validate(propertyName, ...)
        _validator.Validate("Lv5Code", "", EditContext(codeLength: 8, lv5Code: ""))
            .Should().BeEmpty();
    }

    #endregion

    #region Validate — Lv*Name (new mode)

    [Fact]
    public void Validate_Lv1Name_New_Empty_ReturnsError()
    {
        _validator.Validate("Lv1Name", "", NewContext(lv1Name: ""))
            .Should().ContainSingle().Which.Should().Contain("clase");
    }

    [Fact]
    public void Validate_Lv2Name_New_Empty_ReturnsError()
    {
        _validator.Validate("Lv2Name", "", NewContext(lv2Name: ""))
            .Should().ContainSingle().Which.Should().Contain("grupo");
    }

    [Fact]
    public void Validate_Lv3Name_New_Empty_ReturnsError()
    {
        _validator.Validate("Lv3Name", "", NewContext(lv3Name: ""))
            .Should().ContainSingle().Which.Should().Contain("cuenta");
    }

    [Fact]
    public void Validate_Lv4Name_New_Empty_ReturnsError()
    {
        _validator.Validate("Lv4Name", "", NewContext(lv4Name: ""))
            .Should().ContainSingle().Which.Should().Contain("sub cuenta");
    }

    [Fact]
    public void Validate_Lv5Name_New_Empty_ReturnsError()
    {
        _validator.Validate("Lv5Name", "", NewContext(lv5Name: ""))
            .Should().ContainSingle().Which.Should().Contain("auxiliar");
    }

    [Fact]
    public void Validate_Lv5Name_New_Whitespace_ReturnsError()
    {
        _validator.Validate("Lv5Name", "   ", NewContext(lv5Name: "   "))
            .Should().ContainSingle();
    }

    [Fact]
    public void Validate_Lv5Name_New_Valid_ReturnsEmpty()
    {
        _validator.Validate("Lv5Name", "CAJA PRINCIPAL", NewContext(lv5Name: "CAJA PRINCIPAL"))
            .Should().BeEmpty();
    }

    #endregion

    #region Validate — Lv*Name (edit mode, level-gated)

    [Fact]
    public void Validate_Lv3Name_EditLv3_Empty_ReturnsError()
    {
        _validator.Validate("Lv3Name", "", EditContext(codeLength: 4, lv3Name: ""))
            .Should().ContainSingle();
    }

    [Fact]
    public void Validate_Lv3Name_EditLv5_Empty_ReturnsEmpty()
    {
        // Lv3Name is a shallower level when editing a Lv5 entity → shouldn't be validated.
        _validator.Validate("Lv3Name", "", EditContext(codeLength: 8, lv3Name: ""))
            .Should().BeEmpty();
    }

    [Fact]
    public void Validate_Lv5Name_EditLv5_Empty_ReturnsError()
    {
        _validator.Validate("Lv5Name", "", EditContext(codeLength: 8, lv5Name: ""))
            .Should().ContainSingle();
    }

    [Fact]
    public void Validate_Lv5Name_EditLv1_Empty_ReturnsEmpty()
    {
        _validator.Validate("Lv5Name", "", EditContext(codeLength: 1, lv5Name: ""))
            .Should().BeEmpty();
    }

    [Fact]
    public void Validate_Lv1Name_EditLv1_Empty_ReturnsError()
    {
        _validator.Validate("Lv1Name", "", EditContext(codeLength: 1, lv1Name: ""))
            .Should().ContainSingle();
    }

    [Fact]
    public void Validate_Lv2Name_EditLv2_Empty_ReturnsError()
    {
        _validator.Validate("Lv2Name", "", EditContext(codeLength: 2, lv2Name: ""))
            .Should().ContainSingle();
    }

    [Fact]
    public void Validate_Lv4Name_EditLv4_Empty_ReturnsError()
    {
        _validator.Validate("Lv4Name", "", EditContext(codeLength: 6, lv4Name: ""))
            .Should().ContainSingle();
    }

    #endregion

    #region Validate — Margin

    [Fact]
    public void Validate_Margin_RequiresMargin_Zero_ReturnsError()
    {
        _validator.Validate("Margin", 0m, NewContext(lv5Code: "24080001", requiresMargin: true, margin: 0m))
            .Should().ContainSingle().Which.Should().Contain("margen");
    }

    [Fact]
    public void Validate_Margin_RequiresMargin_Negative_ReturnsError()
    {
        _validator.Validate("Margin", -1m, NewContext(lv5Code: "24080001", requiresMargin: true, margin: -1m))
            .Should().ContainSingle();
    }

    [Fact]
    public void Validate_Margin_RequiresMargin_Positive_ReturnsEmpty()
    {
        _validator.Validate("Margin", 5m, NewContext(lv5Code: "24080001", requiresMargin: true, margin: 5m))
            .Should().BeEmpty();
    }

    [Fact]
    public void Validate_Margin_NotRequired_ZeroAccepted()
    {
        _validator.Validate("Margin", 0m, NewContext(requiresMargin: false, margin: 0m))
            .Should().BeEmpty();
    }

    #endregion

    #region Validate — Unknown property

    [Fact]
    public void Validate_UnknownProperty_ReturnsEmpty()
    {
        _validator.Validate("NonExistent", "", NewContext())
            .Should().BeEmpty();
    }

    #endregion

    #region ValidateAll — New mode

    [Fact]
    public void ValidateAll_New_AllEmpty_ReturnsAllNameErrors()
    {
        AccountPlanValidationContext ctx = NewContext(
            lv5Code: "",
            lv1Name: "", lv2Name: "", lv3Name: "", lv4Name: "", lv5Name: "");

        var result = _validator.ValidateAll(ctx);

        result.Should().ContainKey("Lv5Code");
        result.Should().ContainKey("Lv1Name");
        result.Should().ContainKey("Lv2Name");
        result.Should().ContainKey("Lv3Name");
        result.Should().ContainKey("Lv4Name");
        result.Should().ContainKey("Lv5Name");
    }

    [Fact]
    public void ValidateAll_New_Valid_ReturnsEmpty()
    {
        _validator.ValidateAll(NewContext())
            .Should().BeEmpty();
    }

    [Fact]
    public void ValidateAll_New_RequiresMarginAndMissing_IncludesMarginError()
    {
        AccountPlanValidationContext ctx = NewContext(
            lv5Code: "24080001", requiresMargin: true, margin: 0m);

        _validator.ValidateAll(ctx).Should().ContainKey("Margin");
    }

    [Fact]
    public void ValidateAll_New_DuplicateCode_IncludesLv5CodeError()
    {
        HashSet<string> existing = ["11050501"];
        AccountPlanValidationContext ctx = NewContext(
            lv5Code: "11050501", existingCodes: existing);

        _validator.ValidateAll(ctx).Should().ContainKey("Lv5Code");
    }

    #endregion

    #region ValidateAll — Edit mode

    [Fact]
    public void ValidateAll_EditLv1_OnlyLv1NameValidated()
    {
        var result = _validator.ValidateAll(EditContext(codeLength: 1, lv1Name: ""));

        result.Should().ContainKey("Lv1Name");
        result.Should().NotContainKey("Lv2Name");
        result.Should().NotContainKey("Lv5Name");
    }

    [Fact]
    public void ValidateAll_EditLv2_OnlyLv2NameValidated()
    {
        var result = _validator.ValidateAll(EditContext(codeLength: 2, lv2Name: ""));

        result.Should().ContainKey("Lv2Name");
        result.Should().HaveCount(1);
    }

    [Fact]
    public void ValidateAll_EditLv3_OnlyLv3NameValidated()
    {
        var result = _validator.ValidateAll(EditContext(codeLength: 4, lv3Name: ""));

        result.Should().ContainKey("Lv3Name");
        result.Should().HaveCount(1);
    }

    [Fact]
    public void ValidateAll_EditLv4_OnlyLv4NameValidated()
    {
        var result = _validator.ValidateAll(EditContext(codeLength: 6, lv4Name: ""));

        result.Should().ContainKey("Lv4Name");
        result.Should().HaveCount(1);
    }

    [Fact]
    public void ValidateAll_EditLv5_Lv5NameValidated()
    {
        var result = _validator.ValidateAll(EditContext(codeLength: 8, lv5Name: ""));

        result.Should().ContainKey("Lv5Name");
    }

    [Fact]
    public void ValidateAll_EditLv5_WithValidName_ReturnsEmpty()
    {
        var result = _validator.ValidateAll(EditContext(
            codeLength: 8,
            lv5Code: "11050501",
            lv5Name: "CAJA PRINCIPAL"));

        result.Should().BeEmpty();
    }

    [Fact]
    public void ValidateAll_EditLv5_RequiresMargin_IncludesMarginError()
    {
        var result = _validator.ValidateAll(EditContext(
            codeLength: 8,
            lv5Code: "24080001",
            lv5Name: "RETENCIÓN EN LA FUENTE",
            margin: 0m,
            requiresMargin: true));

        result.Should().ContainKey("Margin");
    }

    #endregion

    #region CanSave — New mode

    [Fact]
    public void CanSave_New_AllValid_ReturnsTrue()
    {
        _validator.CanSave(NewCanSaveContext()).Should().BeTrue();
    }

    [Fact]
    public void CanSave_New_IsBusy_ReturnsFalse()
    {
        _validator.CanSave(NewCanSaveContext(isBusy: true)).Should().BeFalse();
    }

    [Fact]
    public void CanSave_New_EmptyLv5Code_ReturnsFalse()
    {
        _validator.CanSave(NewCanSaveContext(lv5Code: "")).Should().BeFalse();
    }

    [Fact]
    public void CanSave_New_Lv5CodeTooShort_ReturnsFalse()
    {
        _validator.CanSave(NewCanSaveContext(lv5Code: "1105")).Should().BeFalse();
    }

    [Fact]
    public void CanSave_New_DuplicateCode_ReturnsFalse()
    {
        HashSet<string> existing = ["11050501"];
        _validator.CanSave(NewCanSaveContext(lv5Code: "11050501", existingCodes: existing))
            .Should().BeFalse();
    }

    [Fact]
    public void CanSave_New_EmptyLv1Name_ReturnsFalse()
    {
        _validator.CanSave(NewCanSaveContext(lv1Name: "")).Should().BeFalse();
    }

    [Fact]
    public void CanSave_New_EmptyLv2Name_ReturnsFalse()
    {
        _validator.CanSave(NewCanSaveContext(lv2Name: "")).Should().BeFalse();
    }

    [Fact]
    public void CanSave_New_EmptyLv3Name_ReturnsFalse()
    {
        _validator.CanSave(NewCanSaveContext(lv3Name: "")).Should().BeFalse();
    }

    [Fact]
    public void CanSave_New_EmptyLv4Name_ReturnsFalse()
    {
        _validator.CanSave(NewCanSaveContext(lv4Name: "")).Should().BeFalse();
    }

    [Fact]
    public void CanSave_New_EmptyLv5Name_ReturnsFalse()
    {
        _validator.CanSave(NewCanSaveContext(lv5Name: "")).Should().BeFalse();
    }

    [Fact]
    public void CanSave_New_RequiresMargin_ZeroMargin_ReturnsFalse()
    {
        _validator.CanSave(NewCanSaveContext(
            lv5Code: "24080001", requiresMargin: true, margin: 0m))
            .Should().BeFalse();
    }

    [Fact]
    public void CanSave_New_RequiresMargin_PositiveMargin_ReturnsTrue()
    {
        _validator.CanSave(NewCanSaveContext(
            lv5Code: "24080001", requiresMargin: true, margin: 2.5m))
            .Should().BeTrue();
    }

    #endregion

    #region CanSave — Edit mode

    [Fact]
    public void CanSave_EditLv1_ValidName_ReturnsTrue()
    {
        _validator.CanSave(EditCanSaveContext(codeLength: 1, lv1Name: "ACTIVO"))
            .Should().BeTrue();
    }

    [Fact]
    public void CanSave_EditLv1_EmptyName_ReturnsFalse()
    {
        _validator.CanSave(EditCanSaveContext(codeLength: 1, lv1Name: ""))
            .Should().BeFalse();
    }

    [Fact]
    public void CanSave_EditLv2_ValidName_ReturnsTrue()
    {
        _validator.CanSave(EditCanSaveContext(codeLength: 2, lv2Name: "DISPONIBLE"))
            .Should().BeTrue();
    }

    [Fact]
    public void CanSave_EditLv3_ValidName_ReturnsTrue()
    {
        _validator.CanSave(EditCanSaveContext(codeLength: 4, lv3Name: "CAJA"))
            .Should().BeTrue();
    }

    [Fact]
    public void CanSave_EditLv4_ValidName_ReturnsTrue()
    {
        _validator.CanSave(EditCanSaveContext(codeLength: 6, lv4Name: "CAJA GENERAL"))
            .Should().BeTrue();
    }

    [Fact]
    public void CanSave_EditLv5_ValidNameAndCode_ReturnsTrue()
    {
        _validator.CanSave(EditCanSaveContext(
            codeLength: 8, lv5Code: "11050501", lv5Name: "CAJA PRINCIPAL"))
            .Should().BeTrue();
    }

    [Fact]
    public void CanSave_EditLv5_MissingLv5Code_ReturnsFalse()
    {
        _validator.CanSave(EditCanSaveContext(
            codeLength: 8, lv5Code: "", lv5Name: "CAJA PRINCIPAL"))
            .Should().BeFalse();
    }

    [Fact]
    public void CanSave_EditLv5_Lv5CodeTooShort_ReturnsFalse()
    {
        _validator.CanSave(EditCanSaveContext(
            codeLength: 8, lv5Code: "1105", lv5Name: "CAJA PRINCIPAL"))
            .Should().BeFalse();
    }

    [Fact]
    public void CanSave_EditLv5_RequiresMargin_Zero_ReturnsFalse()
    {
        _validator.CanSave(EditCanSaveContext(
            codeLength: 8,
            lv5Code: "24080001",
            lv5Name: "RETENCIÓN",
            requiresMargin: true,
            margin: 0m))
            .Should().BeFalse();
    }

    [Fact]
    public void CanSave_EditLv5_RequiresMargin_Positive_ReturnsTrue()
    {
        _validator.CanSave(EditCanSaveContext(
            codeLength: 8,
            lv5Code: "24080001",
            lv5Name: "RETENCIÓN",
            requiresMargin: true,
            margin: 3.5m))
            .Should().BeTrue();
    }

    #endregion

    #region RequiresMarginFor

    [Theory]
    [InlineData("24080001", true)]
    [InlineData("23650001", true)]
    [InlineData("23670001", true)]
    [InlineData("23680001", true)]
    [InlineData("11050501", false)]  // Not a margin-prefix account
    [InlineData("2408", false)]       // Correct prefix but not full length
    [InlineData("240800", false)]     // Still too short
    [InlineData("", false)]
    public void RequiresMarginFor_Returns_ExpectedResult(string code, bool expected)
    {
        AccountPlanValidator.RequiresMarginFor(code).Should().Be(expected);
    }

    [Fact]
    public void RequiresMarginFor_NullCode_ReturnsFalse()
    {
        AccountPlanValidator.RequiresMarginFor(null!).Should().BeFalse();
    }

    #endregion
}
