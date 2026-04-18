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

public class MajorCashDrawerDetailViewModelTests
{
    private readonly IRepository<CashDrawerGraphQLModel> _service;
    private readonly IEventAggregator _eventAggregator;
    private readonly AuxiliaryAccountingAccountCache _accountingAccountCache;
    private readonly MajorCashDrawerCache _majorCashDrawerCache;
    private readonly StringLengthCache _stringLengthCache;
    private readonly JoinableTaskFactory _joinableTaskFactory;
    private readonly MajorCashDrawerValidator _validator;
    private readonly MajorCashDrawerDetailViewModel _vm;

    public MajorCashDrawerDetailViewModelTests()
    {
        _service = Substitute.For<IRepository<CashDrawerGraphQLModel>>();
        _eventAggregator = Substitute.For<IEventAggregator>();

        IRepository<AccountingAccountGraphQLModel> accountRepo = Substitute.For<IRepository<AccountingAccountGraphQLModel>>();
        _accountingAccountCache = new AuxiliaryAccountingAccountCache(accountRepo, _eventAggregator);

        IRepository<CashDrawerGraphQLModel> majorRepo = Substitute.For<IRepository<CashDrawerGraphQLModel>>();
        _majorCashDrawerCache = new MajorCashDrawerCache(majorRepo, _eventAggregator);

        IRepository<EntityStringLengthsGraphQLModel> stringLengthRepo = Substitute.For<IRepository<EntityStringLengthsGraphQLModel>>();
        _stringLengthCache = new StringLengthCache(stringLengthRepo);

        JoinableTaskContext jtc = new();
        _joinableTaskFactory = jtc.Factory;
        _validator = new MajorCashDrawerValidator();

        _vm = new MajorCashDrawerDetailViewModel(
            _service, _eventAggregator, _accountingAccountCache, _majorCashDrawerCache,
            _stringLengthCache, _joinableTaskFactory, _validator);
    }

    private static CostCenterGraphQLModel BuildCostCenter() => new() { Id = 10, Name = "Bogotá" };

    private static CashDrawerGraphQLModel BuildMajor(int id = 5) => new()
    {
        Id = id,
        Name = "Caja Principal",
        CashReviewRequired = false,
        AutoAdjustBalance = false,
        AutoTransfer = false,
        IsPettyCash = false,
        CostCenter = BuildCostCenter(),
        CashAccountingAccount = new AccountingAccountGraphQLModel { Id = 100, Code = "1105", Name = "Caja" },
        CheckAccountingAccount = new AccountingAccountGraphQLModel { Id = 101, Code = "1110", Name = "Cheques" },
        CardAccountingAccount = new AccountingAccountGraphQLModel { Id = 102, Code = "1115", Name = "Tarjetas" }
    };

    #region Constructor null-guards

    [Fact]
    public void Constructor_NullService_Throws()
    {
        System.Action act = () => new MajorCashDrawerDetailViewModel(null!, _eventAggregator, _accountingAccountCache, _majorCashDrawerCache, _stringLengthCache, _joinableTaskFactory, _validator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("cashDrawerService");
    }

    [Fact]
    public void Constructor_NullEventAggregator_Throws()
    {
        System.Action act = () => new MajorCashDrawerDetailViewModel(_service, null!, _accountingAccountCache, _majorCashDrawerCache, _stringLengthCache, _joinableTaskFactory, _validator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("eventAggregator");
    }

    [Fact]
    public void Constructor_NullAccountingCache_Throws()
    {
        System.Action act = () => new MajorCashDrawerDetailViewModel(_service, _eventAggregator, null!, _majorCashDrawerCache, _stringLengthCache, _joinableTaskFactory, _validator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("accountingAccountCache");
    }

    [Fact]
    public void Constructor_NullMajorCache_Throws()
    {
        System.Action act = () => new MajorCashDrawerDetailViewModel(_service, _eventAggregator, _accountingAccountCache, null!, _stringLengthCache, _joinableTaskFactory, _validator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("majorCashDrawerCache");
    }

    [Fact]
    public void Constructor_NullStringLengthCache_Throws()
    {
        System.Action act = () => new MajorCashDrawerDetailViewModel(_service, _eventAggregator, _accountingAccountCache, _majorCashDrawerCache, null!, _joinableTaskFactory, _validator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("stringLengthCache");
    }

    [Fact]
    public void Constructor_NullValidator_Throws()
    {
        System.Action act = () => new MajorCashDrawerDetailViewModel(_service, _eventAggregator, _accountingAccountCache, _majorCashDrawerCache, _stringLengthCache, _joinableTaskFactory, null!);
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
        _vm.IsPettyCash.Should().BeFalse();
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
        _accountingAccountCache.Add(new AccountingAccountGraphQLModel { Id = 101, Code = "1110", Name = "Cheques" });
        _accountingAccountCache.Add(new AccountingAccountGraphQLModel { Id = 102, Code = "1115", Name = "Tarjetas" });

        _vm.SetForEdit(BuildMajor());

        _vm.Id.Should().Be(5);
        _vm.Name.Should().Be("Caja Principal");
        _vm.CostCenterId.Should().Be(10);
        _vm.SelectedCashAccountingAccount.Should().NotBeNull();
        _vm.SelectedCheckAccountingAccount.Should().NotBeNull();
        _vm.SelectedCardAccountingAccount.Should().NotBeNull();
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
        _vm.Name = "Caja Principal";

        _vm.CanSave.Should().BeTrue();
    }

    [Fact]
    public void CanSave_AutoTransferWithoutTarget_ReturnsFalse()
    {
        _vm.SetForNew(BuildCostCenter());
        _vm.Name = "Caja Principal";
        _vm.AutoTransfer = true;

        _vm.CanSave.Should().BeFalse();
    }

    [Fact]
    public void CanSave_AutoTransferWithTarget_ReturnsTrue()
    {
        _majorCashDrawerCache.Add(new CashDrawerGraphQLModel { Id = 99, Name = "Caja destino" });
        _vm.SetForNew(BuildCostCenter());
        _vm.Name = "Caja Principal";
        _vm.AutoTransfer = true;
        _vm.SelectedAutoTransferCashDrawer = _majorCashDrawerCache.Items.First();

        _vm.CanSave.Should().BeTrue();
    }

    [Fact]
    public void AutoTransferCashDrawers_ExcludesSelf()
    {
        _majorCashDrawerCache.Add(new CashDrawerGraphQLModel { Id = 5, Name = "Yo mismo" });
        _majorCashDrawerCache.Add(new CashDrawerGraphQLModel { Id = 99, Name = "Otra" });

        _vm.SetForEdit(BuildMajor(5));

        _vm.AutoTransferCashDrawers.Should().NotContain(c => c.Id == 5);
        _vm.AutoTransferCashDrawers.Should().ContainSingle(c => c.Id == 99);
    }

    #endregion

    #region Save flow

    [Fact]
    public async Task SaveAsync_Create_PublishesCashDrawerCreateMessage()
    {
        _vm.SetForNew(BuildCostCenter());
        _vm.Name = "Caja Principal";

        UpsertResponseType<CashDrawerGraphQLModel> response = new() { Success = true, Entity = BuildMajor() };
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
        _accountingAccountCache.Add(new AccountingAccountGraphQLModel { Id = 101, Code = "1110", Name = "Cheques" });
        _accountingAccountCache.Add(new AccountingAccountGraphQLModel { Id = 102, Code = "1115", Name = "Tarjetas" });

        _vm.SetForEdit(BuildMajor());
        _vm.Name = "Caja Renombrada";

        UpsertResponseType<CashDrawerGraphQLModel> response = new() { Success = true, Entity = BuildMajor() };
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
        _vm.Name = "Caja Principal";

        UpsertResponseType<CashDrawerGraphQLModel> response = new() { Success = true, Entity = BuildMajor() };
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
        _vm.GetErrors(nameof(MajorCashDrawerValidationContext.Name)).Cast<string>().Should().NotBeEmpty();
    }

    [Fact]
    public void EnablingAutoTransferWithoutTarget_RaisesError()
    {
        _vm.SetForNew(BuildCostCenter());
        _vm.Name = "Caja Principal";
        _vm.AutoTransfer = true;

        _vm.HasErrors.Should().BeTrue();
        _vm.GetErrors(nameof(MajorCashDrawerValidationContext.AutoTransferCashDrawerId)).Cast<string>().Should().NotBeEmpty();
    }

    #endregion
}
