using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Caliburn.Micro;
using Common.Interfaces;
using FluentAssertions;
using Microsoft.VisualStudio.Threading;
using Models.Books;
using Models.Global;
using Models.Treasury;
using NetErp.Helpers.Cache;
using NetErp.Treasury.Masters.Validators;
using NetErp.Treasury.Masters.ViewModels;
using NSubstitute;
using Xunit;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Tests.ViewModels;

public class FranchiseDetailViewModelTests
{
    private readonly IRepository<FranchiseGraphQLModel> _service;
    private readonly IEventAggregator _eventAggregator;
    private readonly BankAccountCache _bankAccountCache;
    private readonly AuxiliaryAccountingAccountCache _accountingAccountCache;
    private readonly StringLengthCache _stringLengthCache;
    private readonly JoinableTaskFactory _joinableTaskFactory;
    private readonly FranchiseValidator _validator;
    private readonly FranchiseDetailViewModel _vm;

    public FranchiseDetailViewModelTests()
    {
        _service = Substitute.For<IRepository<FranchiseGraphQLModel>>();
        _eventAggregator = Substitute.For<IEventAggregator>();

        IRepository<BankAccountGraphQLModel> bankAccountRepo = Substitute.For<IRepository<BankAccountGraphQLModel>>();
        _bankAccountCache = new BankAccountCache(bankAccountRepo, _eventAggregator);

        IRepository<AccountingAccountGraphQLModel> accountRepo = Substitute.For<IRepository<AccountingAccountGraphQLModel>>();
        _accountingAccountCache = new AuxiliaryAccountingAccountCache(accountRepo, _eventAggregator);

        IRepository<EntityStringLengthsGraphQLModel> stringLengthRepo = Substitute.For<IRepository<EntityStringLengthsGraphQLModel>>();
        _stringLengthCache = new StringLengthCache(stringLengthRepo);

        JoinableTaskContext jtc = new();
        _joinableTaskFactory = jtc.Factory;
        _validator = new FranchiseValidator();

        _vm = new FranchiseDetailViewModel(_service, _eventAggregator, _bankAccountCache, _accountingAccountCache, _stringLengthCache, _joinableTaskFactory, _validator);
    }

    private static FranchiseGraphQLModel Build(int id = 1) => new()
    {
        Id = id,
        Name = "Visa",
        Type = "TC",
        CommissionRate = 2.5m,
        ReteivaRate = 15m,
        ReteicaRate = 6.9m,
        RetefteRate = 2.5m,
        TaxRate = 19m,
        FormulaCommission = "X",
        FormulaReteiva = "Y",
        FormulaReteica = "Z",
        FormulaRetefte = "W",
        CommissionAccountingAccount = new AccountingAccountGraphQLModel { Id = 100, Code = "1234", Name = "Comisiones" },
        BankAccount = new BankAccountGraphQLModel { Id = 200, Description = "Cuenta Principal" }
    };

    #region Constructor null-guards

    [Fact]
    public void Constructor_NullService_Throws()
    {
        System.Action act = () => new FranchiseDetailViewModel(null!, _eventAggregator, _bankAccountCache, _accountingAccountCache, _stringLengthCache, _joinableTaskFactory, _validator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("franchiseService");
    }

    [Fact]
    public void Constructor_NullEventAggregator_Throws()
    {
        System.Action act = () => new FranchiseDetailViewModel(_service, null!, _bankAccountCache, _accountingAccountCache, _stringLengthCache, _joinableTaskFactory, _validator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("eventAggregator");
    }

    [Fact]
    public void Constructor_NullBankAccountCache_Throws()
    {
        System.Action act = () => new FranchiseDetailViewModel(_service, _eventAggregator, null!, _accountingAccountCache, _stringLengthCache, _joinableTaskFactory, _validator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("bankAccountCache");
    }

    [Fact]
    public void Constructor_NullAccountingAccountCache_Throws()
    {
        System.Action act = () => new FranchiseDetailViewModel(_service, _eventAggregator, _bankAccountCache, null!, _stringLengthCache, _joinableTaskFactory, _validator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("accountingAccountCache");
    }

    [Fact]
    public void Constructor_NullValidator_Throws()
    {
        System.Action act = () => new FranchiseDetailViewModel(_service, _eventAggregator, _bankAccountCache, _accountingAccountCache, _stringLengthCache, _joinableTaskFactory, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("validator");
    }

    #endregion

    #region SetForNew / SetForEdit

    [Fact]
    public void SetForNew_SetsDefaults()
    {
        _vm.SetForNew();

        _vm.Id.Should().Be(0);
        _vm.Name.Should().BeEmpty();
        _vm.Type.Should().Be("TC");
        _vm.CommissionRate.Should().Be(0);
        _vm.FormulaCommission.Should().Contain("VALOR_TARJETA");
        _vm.SelectedBankAccount.Should().BeNull();
        _vm.SelectedCommissionAccountingAccount.Should().BeNull();
        _vm.IsNewRecord.Should().BeTrue();
    }

    [Fact]
    public void SetForNew_CanSaveFalse()
    {
        _vm.SetForNew();
        _vm.CanSave.Should().BeFalse();
    }

    [Fact]
    public void SetForEdit_WithCachePopulated_LinksSelections()
    {
        _bankAccountCache.Add(new BankAccountGraphQLModel { Id = 200, Description = "Cuenta Principal" });
        _accountingAccountCache.Add(new AccountingAccountGraphQLModel { Id = 100, Code = "1234", Name = "Comisiones" });

        _vm.SetForEdit(Build());

        _vm.Id.Should().Be(1);
        _vm.Name.Should().Be("Visa");
        _vm.Type.Should().Be("TC");
        _vm.SelectedBankAccount.Should().NotBeNull().And.Subject.As<BankAccountGraphQLModel>().Id.Should().Be(200);
        _vm.SelectedCommissionAccountingAccount.Should().NotBeNull().And.Subject.As<AccountingAccountGraphQLModel>().Id.Should().Be(100);
        _vm.IsNewRecord.Should().BeFalse();
    }

    [Fact]
    public void SetForEdit_CanSaveFalseWithoutChanges()
    {
        _bankAccountCache.Add(new BankAccountGraphQLModel { Id = 200, Description = "Cuenta Principal" });
        _accountingAccountCache.Add(new AccountingAccountGraphQLModel { Id = 100, Code = "1234", Name = "Comisiones" });

        _vm.SetForEdit(Build());
        _vm.CanSave.Should().BeFalse();
    }

    #endregion

    #region CanSave reactivity

    [Fact]
    public void CanSave_Edit_TurnsTrue_WhenChangingCommissionRate()
    {
        _bankAccountCache.Add(new BankAccountGraphQLModel { Id = 200, Description = "Cuenta" });
        _accountingAccountCache.Add(new AccountingAccountGraphQLModel { Id = 100, Code = "x", Name = "y" });
        _vm.SetForEdit(Build());

        _vm.CommissionRate = 3m;

        _vm.CanSave.Should().BeTrue();
    }

    [Fact]
    public void CanSave_NewRecord_RequiresAllFields()
    {
        _vm.SetForNew();
        _vm.Name = "Visa";
        _vm.CanSave.Should().BeFalse("faltan cuentas");

        _vm.SelectedBankAccount = new BankAccountGraphQLModel { Id = 200, Description = "x" };
        _vm.SelectedCommissionAccountingAccount = new AccountingAccountGraphQLModel { Id = 100, Code = "a", Name = "b" };
        _vm.CanSave.Should().BeTrue();
    }

    #endregion

    #region Simulator

    [Fact]
    public void Simulator_ValidFormulas_UpdatesSimulatedValues()
    {
        _vm.SetForNew();
        _vm.Name = "Visa";
        _vm.TaxRate = 19m;
        _vm.CommissionRate = 2m;
        _vm.CardValue = 100_000m;

        _vm.SimulatorCommand.Execute(null);

        _vm.SimulatedIvaValue.Should().BeGreaterThan(0);
        _vm.SimulatedCommission.Should().BeGreaterThan(0);
    }

    #endregion

    #region Save flow

    [Fact]
    public async Task SaveAsync_Create_PublishesFranchiseCreateMessage()
    {
        _bankAccountCache.Add(new BankAccountGraphQLModel { Id = 200, Description = "x" });
        _accountingAccountCache.Add(new AccountingAccountGraphQLModel { Id = 100, Code = "a", Name = "b" });

        _vm.SetForNew();
        _vm.Name = "Visa";
        _vm.SelectedBankAccount = _bankAccountCache.Items.First();
        _vm.SelectedCommissionAccountingAccount = _accountingAccountCache.Items.First();

        UpsertResponseType<FranchiseGraphQLModel> response = new() { Success = true, Entity = Build() };
        _service.CreateAsync<UpsertResponseType<FranchiseGraphQLModel>>(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(response);

        await _vm.SaveAsync();

        await _eventAggregator.Received(1).PublishOnCurrentThreadAsync(
            Arg.Is<FranchiseCreateMessage>(m => m.CreatedFranchise.Success),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveAsync_Update_PublishesFranchiseUpdateMessage()
    {
        _bankAccountCache.Add(new BankAccountGraphQLModel { Id = 200, Description = "x" });
        _accountingAccountCache.Add(new AccountingAccountGraphQLModel { Id = 100, Code = "a", Name = "b" });
        _vm.SetForEdit(Build());
        _vm.CommissionRate = 5m;

        UpsertResponseType<FranchiseGraphQLModel> response = new() { Success = true, Entity = Build() };
        _service.UpdateAsync<UpsertResponseType<FranchiseGraphQLModel>>(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(response);

        await _vm.SaveAsync();

        await _eventAggregator.Received(1).PublishOnCurrentThreadAsync(
            Arg.Is<FranchiseUpdateMessage>(m => m.UpdatedFranchise.Success),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteSaveAsync_NewRecord_CallsCreateAsync()
    {
        _bankAccountCache.Add(new BankAccountGraphQLModel { Id = 200, Description = "x" });
        _accountingAccountCache.Add(new AccountingAccountGraphQLModel { Id = 100, Code = "a", Name = "b" });
        _vm.SetForNew();
        _vm.Name = "Visa";
        _vm.SelectedBankAccount = _bankAccountCache.Items.First();
        _vm.SelectedCommissionAccountingAccount = _accountingAccountCache.Items.First();

        UpsertResponseType<FranchiseGraphQLModel> response = new() { Success = true, Entity = Build() };
        _service.CreateAsync<UpsertResponseType<FranchiseGraphQLModel>>(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(response);

        UpsertResponseType<FranchiseGraphQLModel> result = await _vm.ExecuteSaveAsync();

        result.Success.Should().BeTrue();
        await _service.Received(1).CreateAsync<UpsertResponseType<FranchiseGraphQLModel>>(
            Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region INotifyDataErrorInfo

    [Fact]
    public void SettingEmptyName_RaisesErrorForName()
    {
        _bankAccountCache.Add(new BankAccountGraphQLModel { Id = 200, Description = "x" });
        _accountingAccountCache.Add(new AccountingAccountGraphQLModel { Id = 100, Code = "a", Name = "b" });
        _vm.SetForEdit(Build());

        _vm.Name = "";

        _vm.HasErrors.Should().BeTrue();
        _vm.GetErrors(nameof(FranchiseValidationContext.Name)).Cast<string>().Should().NotBeEmpty();
    }

    [Fact]
    public void ClearingBankAccount_RaisesError()
    {
        _bankAccountCache.Add(new BankAccountGraphQLModel { Id = 200, Description = "x" });
        _accountingAccountCache.Add(new AccountingAccountGraphQLModel { Id = 100, Code = "a", Name = "b" });
        _vm.SetForEdit(Build());

        _vm.SelectedBankAccount = null;

        _vm.HasErrors.Should().BeTrue();
        _vm.GetErrors(nameof(FranchiseValidationContext.BankAccountId)).Cast<string>().Should().NotBeEmpty();
    }

    #endregion
}
