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

public class MinorCashDrawerDetailViewModelTests
{
    private readonly IRepository<CashDrawerGraphQLModel> _service;
    private readonly IEventAggregator _eventAggregator;
    private readonly AuxiliaryAccountingAccountCache _accountingAccountCache;
    private readonly StringLengthCache _stringLengthCache;
    private readonly JoinableTaskFactory _joinableTaskFactory;
    private readonly MinorCashDrawerValidator _validator;
    private readonly MinorCashDrawerDetailViewModel _vm;

    public MinorCashDrawerDetailViewModelTests()
    {
        _service = Substitute.For<IRepository<CashDrawerGraphQLModel>>();
        _eventAggregator = Substitute.For<IEventAggregator>();

        IRepository<AccountingAccountGraphQLModel> accountRepo = Substitute.For<IRepository<AccountingAccountGraphQLModel>>();
        _accountingAccountCache = new AuxiliaryAccountingAccountCache(accountRepo, _eventAggregator);

        IRepository<EntityStringLengthsGraphQLModel> stringLengthRepo = Substitute.For<IRepository<EntityStringLengthsGraphQLModel>>();
        _stringLengthCache = new StringLengthCache(stringLengthRepo);

        JoinableTaskContext jtc = new();
        _joinableTaskFactory = jtc.Factory;
        _validator = new MinorCashDrawerValidator();

        _vm = new MinorCashDrawerDetailViewModel(
            _service, _eventAggregator, _accountingAccountCache,
            _stringLengthCache, _joinableTaskFactory, _validator);
    }

    private static CostCenterGraphQLModel BuildCostCenter() => new() { Id = 10, Name = "Bogotá" };

    private static CashDrawerGraphQLModel BuildMinor(int id = 5) => new()
    {
        Id = id,
        Name = "Caja Menor",
        CashReviewRequired = false,
        AutoAdjustBalance = false,
        IsPettyCash = true,
        CostCenter = BuildCostCenter(),
        CashAccountingAccount = new AccountingAccountGraphQLModel { Id = 100, Code = "1105", Name = "Caja" }
    };

    #region Constructor null-guards

    [Fact]
    public void Constructor_NullService_Throws()
    {
        System.Action act = () => new MinorCashDrawerDetailViewModel(null!, _eventAggregator, _accountingAccountCache, _stringLengthCache, _joinableTaskFactory, _validator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("cashDrawerService");
    }

    [Fact]
    public void Constructor_NullEventAggregator_Throws()
    {
        System.Action act = () => new MinorCashDrawerDetailViewModel(_service, null!, _accountingAccountCache, _stringLengthCache, _joinableTaskFactory, _validator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("eventAggregator");
    }

    [Fact]
    public void Constructor_NullAccountingCache_Throws()
    {
        System.Action act = () => new MinorCashDrawerDetailViewModel(_service, _eventAggregator, null!, _stringLengthCache, _joinableTaskFactory, _validator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("accountingAccountCache");
    }

    [Fact]
    public void Constructor_NullStringLengthCache_Throws()
    {
        System.Action act = () => new MinorCashDrawerDetailViewModel(_service, _eventAggregator, _accountingAccountCache, null!, _joinableTaskFactory, _validator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("stringLengthCache");
    }

    [Fact]
    public void Constructor_NullValidator_Throws()
    {
        System.Action act = () => new MinorCashDrawerDetailViewModel(_service, _eventAggregator, _accountingAccountCache, _stringLengthCache, _joinableTaskFactory, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("validator");
    }

    #endregion

    #region SetForNew / SetForEdit

    [Fact]
    public void SetForNew_StoresParentCostCenter()
    {
        _vm.SetForNew(BuildCostCenter());

        _vm.Id.Should().Be(0);
        _vm.CostCenterId.Should().Be(10);
        _vm.CostCenterName.Should().Be("Bogotá");
        _vm.IsPettyCash.Should().BeTrue();
        _vm.IsNewRecord.Should().BeTrue();
    }

    [Fact]
    public void SetForNew_NullCostCenter_Throws()
    {
        System.Action act = () => _vm.SetForNew(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void SetForEdit_PopulatesFromModel()
    {
        _accountingAccountCache.Add(new AccountingAccountGraphQLModel { Id = 100, Code = "1105", Name = "Caja" });

        _vm.SetForEdit(BuildMinor());

        _vm.Id.Should().Be(5);
        _vm.Name.Should().Be("Caja Menor");
        _vm.CostCenterId.Should().Be(10);
        _vm.IsPettyCash.Should().BeTrue();
        _vm.SelectedCashAccountingAccount.Should().NotBeNull();
        _vm.IsNewRecord.Should().BeFalse();
    }

    [Fact]
    public void SetForNew_NoChanges_CanSaveFalse()
    {
        _vm.SetForNew(BuildCostCenter());
        _vm.CanSave.Should().BeFalse();
    }

    #endregion

    #region CanSave reactivity

    [Fact]
    public void CanSave_TurnsTrue_WhenNameSet()
    {
        _vm.SetForNew(BuildCostCenter());
        _vm.Name = "Caja Menor";

        _vm.CanSave.Should().BeTrue();
    }

    [Fact]
    public void CanSave_False_WhenNameCleared()
    {
        _vm.SetForNew(BuildCostCenter());
        _vm.Name = "Caja Menor";
        _vm.Name = "";

        _vm.CanSave.Should().BeFalse();
    }

    [Fact]
    public void CanSave_True_WhenFlagChanged()
    {
        _accountingAccountCache.Add(new AccountingAccountGraphQLModel { Id = 100, Code = "1105", Name = "Caja" });
        _vm.SetForEdit(BuildMinor());

        _vm.CashReviewRequired = true;

        _vm.CanSave.Should().BeTrue();
    }

    #endregion

    #region Save flow

    [Fact]
    public async Task SaveAsync_Create_PublishesCashDrawerCreateMessage()
    {
        _vm.SetForNew(BuildCostCenter());
        _vm.Name = "Caja Menor";

        UpsertResponseType<CashDrawerGraphQLModel> response = new() { Success = true, Entity = BuildMinor() };
        _service.CreateAsync<UpsertResponseType<CashDrawerGraphQLModel>>(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(response);

        await _vm.SaveAsync();

        await _eventAggregator.Received(1).PublishOnCurrentThreadAsync(
            Arg.Is<TreasuryCashDrawerCreateMessage>(m => m.CreatedCashDrawer.Success),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveAsync_Update_PublishesCashDrawerUpdateMessage()
    {
        _accountingAccountCache.Add(new AccountingAccountGraphQLModel { Id = 100, Code = "1105", Name = "Caja" });
        _vm.SetForEdit(BuildMinor());
        _vm.Name = "Caja Renombrada";

        UpsertResponseType<CashDrawerGraphQLModel> response = new() { Success = true, Entity = BuildMinor() };
        _service.UpdateAsync<UpsertResponseType<CashDrawerGraphQLModel>>(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(response);

        await _vm.SaveAsync();

        await _eventAggregator.Received(1).PublishOnCurrentThreadAsync(
            Arg.Is<TreasuryCashDrawerUpdateMessage>(m => m.UpdatedCashDrawer.Success),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteSaveAsync_NewRecord_CallsCreateAsync()
    {
        _vm.SetForNew(BuildCostCenter());
        _vm.Name = "Caja Menor";

        UpsertResponseType<CashDrawerGraphQLModel> response = new() { Success = true, Entity = BuildMinor() };
        _service.CreateAsync<UpsertResponseType<CashDrawerGraphQLModel>>(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(response);

        UpsertResponseType<CashDrawerGraphQLModel> result = await _vm.ExecuteSaveAsync();

        result.Success.Should().BeTrue();
        await _service.Received(1).CreateAsync<UpsertResponseType<CashDrawerGraphQLModel>>(
            Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region INotifyDataErrorInfo

    [Fact]
    public void SettingEmptyName_RaisesErrorForName()
    {
        _vm.SetForNew(BuildCostCenter());
        _vm.Name = "x";
        _vm.Name = "";

        _vm.HasErrors.Should().BeTrue();
        _vm.GetErrors(nameof(MinorCashDrawerValidationContext.Name)).Cast<string>().Should().NotBeEmpty();
    }

    [Fact]
    public void SettingValidName_ClearsError()
    {
        _vm.SetForNew(BuildCostCenter());
        _vm.Name = "x";
        _vm.Name = "";
        _vm.HasErrors.Should().BeTrue();

        _vm.Name = "Caja Menor";

        _vm.GetErrors(nameof(MinorCashDrawerValidationContext.Name)).Cast<string>().Should().BeEmpty();
    }

    #endregion
}
