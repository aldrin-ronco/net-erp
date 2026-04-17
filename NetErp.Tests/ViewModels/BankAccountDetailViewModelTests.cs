using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Caliburn.Micro;
using Common.Interfaces;
using FluentAssertions;
using Microsoft.VisualStudio.Threading;
using Models.Billing;
using Models.Books;
using Models.Global;
using Models.Treasury;
using NetErp.Helpers.Cache;
using NetErp.Treasury.Masters.Validators;
using NetErp.Treasury.Masters.ViewModels;
using NSubstitute;
using Xunit;
using static Dictionaries.BooksDictionaries;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Tests.ViewModels;

public class BankAccountDetailViewModelTests
{
    private readonly IRepository<BankAccountGraphQLModel> _service;
    private readonly IEventAggregator _eventAggregator;
    private readonly AuxiliaryAccountingAccountCache _accountingAccountCache;
    private readonly StringLengthCache _stringLengthCache;
    private readonly JoinableTaskFactory _joinableTaskFactory;
    private readonly BankAccountValidator _validator;
    private readonly BankAccountDetailViewModel _vm;

    public BankAccountDetailViewModelTests()
    {
        _service = Substitute.For<IRepository<BankAccountGraphQLModel>>();
        _eventAggregator = Substitute.For<IEventAggregator>();

        IRepository<AccountingAccountGraphQLModel> accountRepo = Substitute.For<IRepository<AccountingAccountGraphQLModel>>();
        _accountingAccountCache = new AuxiliaryAccountingAccountCache(accountRepo, _eventAggregator);

        IRepository<EntityStringLengthsGraphQLModel> stringLengthRepo = Substitute.For<IRepository<EntityStringLengthsGraphQLModel>>();
        _stringLengthCache = new StringLengthCache(stringLengthRepo);

        JoinableTaskContext jtc = new();
        _joinableTaskFactory = jtc.Factory;
        _validator = new BankAccountValidator();

        _vm = new BankAccountDetailViewModel(_service, _eventAggregator, _accountingAccountCache, _stringLengthCache, _joinableTaskFactory, _validator);
    }

    private static BankGraphQLModel BuildTraditionalBank() => new()
    {
        Id = 10,
        AccountingEntity = new AccountingEntityGraphQLModel { Id = 1, SearchName = "Bancolombia", CaptureType = "PJ" }
    };

    private static BankGraphQLModel BuildDigitalWalletBank() => new()
    {
        Id = 20,
        AccountingEntity = new AccountingEntityGraphQLModel { Id = 2, SearchName = "NEQUI BANCOLOMBIA", CaptureType = "PN" }
    };

    private static BankAccountGraphQLModel BuildBankAccount() => new()
    {
        Id = 5,
        Type = "A",
        Number = "001-234",
        IsActive = true,
        Reference = "",
        DisplayOrder = 1,
        Provider = "",
        Bank = BuildTraditionalBank(),
        AccountingAccount = new AccountingAccountGraphQLModel { Id = 100, Code = "1110", Name = "Caja general" },
        PaymentMethod = new PaymentMethodGraphQLModel()
    };

    #region Constructor null-guards

    [Fact]
    public void Constructor_NullService_Throws()
    {
        System.Action act = () => new BankAccountDetailViewModel(null!, _eventAggregator, _accountingAccountCache, _stringLengthCache, _joinableTaskFactory, _validator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("bankAccountService");
    }

    [Fact]
    public void Constructor_NullEventAggregator_Throws()
    {
        System.Action act = () => new BankAccountDetailViewModel(_service, null!, _accountingAccountCache, _stringLengthCache, _joinableTaskFactory, _validator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("eventAggregator");
    }

    [Fact]
    public void Constructor_NullAccountingCache_Throws()
    {
        System.Action act = () => new BankAccountDetailViewModel(_service, _eventAggregator, null!, _stringLengthCache, _joinableTaskFactory, _validator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("accountingAccountCache");
    }

    [Fact]
    public void Constructor_NullStringLengthCache_Throws()
    {
        System.Action act = () => new BankAccountDetailViewModel(_service, _eventAggregator, _accountingAccountCache, null!, _joinableTaskFactory, _validator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("stringLengthCache");
    }

    [Fact]
    public void Constructor_NullValidator_Throws()
    {
        System.Action act = () => new BankAccountDetailViewModel(_service, _eventAggregator, _accountingAccountCache, _stringLengthCache, _joinableTaskFactory, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("validator");
    }

    #endregion

    #region SetForNew / SetForEdit

    [Fact]
    public void SetForNew_TraditionalBank_DefaultsToAhorros()
    {
        _vm.SetForNew(BuildTraditionalBank());

        _vm.BankId.Should().Be(10);
        _vm.BankCaptureType.Should().Be(CaptureTypeEnum.PJ);
        _vm.IsTraditionalBank.Should().BeTrue();
        _vm.Type.Should().Be("A");
        _vm.Provider.Should().BeEmpty();
        _vm.AccountingAccountAutoCreate.Should().BeTrue();
        _vm.AccountingAccountSelectExisting.Should().BeFalse();
        _vm.IsNewRecord.Should().BeTrue();
    }

    [Fact]
    public void SetForNew_DigitalWallet_DefaultsToProviderN()
    {
        _vm.SetForNew(BuildDigitalWalletBank());

        _vm.IsDigitalWallet.Should().BeTrue();
        _vm.Type.Should().Be("M");
        _vm.Provider.Should().Be("N");
    }

    [Fact]
    public void SetForNew_NullBank_Throws()
    {
        System.Action act = () => _vm.SetForNew(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void SetForEdit_PopulatesFromModel()
    {
        _accountingAccountCache.Add(new AccountingAccountGraphQLModel { Id = 100, Code = "1110", Name = "Caja general" });

        _vm.SetForEdit(BuildBankAccount());

        _vm.Id.Should().Be(5);
        _vm.Number.Should().Be("001-234");
        _vm.Type.Should().Be("A");
        _vm.BankId.Should().Be(10);
        _vm.BankCaptureType.Should().Be(CaptureTypeEnum.PJ);
        _vm.AccountingAccountAutoCreate.Should().BeFalse();
        _vm.AccountingAccountSelectExisting.Should().BeTrue();
        _vm.SelectedAccountingAccount.Should().NotBeNull();
        _vm.IsNewRecord.Should().BeFalse();
    }

    [Fact]
    public void SetForNew_NoChanges_CanSaveFalse()
    {
        _vm.SetForNew(BuildTraditionalBank());
        _vm.CanSave.Should().BeFalse();
    }

    #endregion

    #region CanSave reactivity

    [Fact]
    public void CanSave_New_TurnsTrue_WhenNumberSet()
    {
        _vm.SetForNew(BuildTraditionalBank());

        _vm.Number = "001-234";

        _vm.CanSave.Should().BeTrue();
    }

    [Fact]
    public void CanSave_New_SelectExisting_RequiresAccount()
    {
        _vm.SetForNew(BuildTraditionalBank());
        _vm.Number = "001-234";
        _vm.AccountingAccountSelectExisting = true;

        _vm.CanSave.Should().BeFalse();

        _vm.SelectedAccountingAccount = new AccountingAccountGraphQLModel { Id = 100 };
        _vm.CanSave.Should().BeTrue();
    }

    [Fact]
    public void CanSave_False_WhenNumberIsEmpty()
    {
        _vm.SetForNew(BuildTraditionalBank());
        _vm.Number = "001";
        _vm.Number = "";

        _vm.CanSave.Should().BeFalse();
    }

    #endregion

    #region Description / PaymentMethodName

    [Fact]
    public void Description_TraditionalBank_FormatsAsSavings()
    {
        _vm.SetForNew(BuildTraditionalBank());
        _vm.Number = "001-234";

        _vm.Description.Should().Contain("Bancolombia");
        _vm.Description.Should().Contain("CTA. DE AHORROS");
        _vm.Description.Should().Contain("001-234");
    }

    [Fact]
    public void Description_TraditionalBank_Current_FormatsAsCurrent()
    {
        _vm.SetForNew(BuildTraditionalBank());
        _vm.Type = "C";
        _vm.Number = "99";

        _vm.Description.Should().Contain("CTA. CORRIENTE");
    }

    [Fact]
    public void Description_DigitalWallet_FormatsWithProviderName()
    {
        _vm.SetForNew(BuildDigitalWalletBank());
        _vm.Number = "3001234567";

        _vm.Description.Should().Contain("NEQUI");
        _vm.Description.Should().Contain("3001234567");

        _vm.Provider = "D";
        _vm.Description.Should().Contain("DAVIPLATA");
    }

    #endregion

    #region Save flow

    [Fact]
    public async Task SaveAsync_Create_PublishesBankAccountCreateMessage()
    {
        _vm.SetForNew(BuildTraditionalBank());
        _vm.Number = "001-234";

        UpsertResponseType<BankAccountGraphQLModel> response = new() { Success = true, Entity = BuildBankAccount() };
        _service.CreateAsync<UpsertResponseType<BankAccountGraphQLModel>>(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(response);

        await _vm.SaveAsync();

        await _eventAggregator.Received(1).PublishOnCurrentThreadAsync(
            Arg.Is<BankAccountCreateMessage>(m => m.CreatedBankAccount.Success),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveAsync_Update_PublishesBankAccountUpdateMessage()
    {
        _accountingAccountCache.Add(new AccountingAccountGraphQLModel { Id = 100, Code = "1110", Name = "Caja" });
        _vm.SetForEdit(BuildBankAccount());
        _vm.Number = "999";

        UpsertResponseType<BankAccountGraphQLModel> response = new() { Success = true, Entity = BuildBankAccount() };
        _service.UpdateAsync<UpsertResponseType<BankAccountGraphQLModel>>(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(response);

        await _vm.SaveAsync();

        await _eventAggregator.Received(1).PublishOnCurrentThreadAsync(
            Arg.Is<BankAccountUpdateMessage>(m => m.UpdatedBankAccount.Success),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteSaveAsync_NewRecord_CallsCreateAsync()
    {
        _vm.SetForNew(BuildTraditionalBank());
        _vm.Number = "001-234";

        UpsertResponseType<BankAccountGraphQLModel> response = new() { Success = true, Entity = BuildBankAccount() };
        _service.CreateAsync<UpsertResponseType<BankAccountGraphQLModel>>(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(response);

        UpsertResponseType<BankAccountGraphQLModel> result = await _vm.ExecuteSaveAsync();

        result.Success.Should().BeTrue();
        await _service.Received(1).CreateAsync<UpsertResponseType<BankAccountGraphQLModel>>(
            Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region INotifyDataErrorInfo

    [Fact]
    public void SettingEmptyNumber_RaisesErrorForNumber()
    {
        _vm.SetForNew(BuildTraditionalBank());
        _vm.Number = "x";
        _vm.Number = "";

        _vm.HasErrors.Should().BeTrue();
        _vm.GetErrors(nameof(BankAccountValidationContext.Number)).Cast<string>().Should().NotBeEmpty();
    }

    [Fact]
    public void SelectExisting_ButNoAccount_RaisesError()
    {
        _vm.SetForNew(BuildTraditionalBank());
        _vm.AccountingAccountSelectExisting = true;

        _vm.HasErrors.Should().BeTrue();
        _vm.GetErrors(nameof(BankAccountValidationContext.AccountingAccountId)).Cast<string>().Should().NotBeEmpty();
    }

    #endregion
}
