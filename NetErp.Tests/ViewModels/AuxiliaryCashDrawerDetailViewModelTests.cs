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

public class AuxiliaryCashDrawerDetailViewModelTests
{
    private readonly IRepository<CashDrawerGraphQLModel> _service;
    private readonly IEventAggregator _eventAggregator;
    private readonly AuxiliaryAccountingAccountCache _accountingAccountCache;
    private readonly AuxiliaryCashDrawerCache _auxiliaryCashDrawerCache;
    private readonly StringLengthCache _stringLengthCache;
    private readonly JoinableTaskFactory _joinableTaskFactory;
    private readonly AuxiliaryCashDrawerValidator _validator;
    private readonly AuxiliaryCashDrawerDetailViewModel _vm;

    public AuxiliaryCashDrawerDetailViewModelTests()
    {
        _service = Substitute.For<IRepository<CashDrawerGraphQLModel>>();
        _eventAggregator = Substitute.For<IEventAggregator>();

        IRepository<AccountingAccountGraphQLModel> accountRepo = Substitute.For<IRepository<AccountingAccountGraphQLModel>>();
        _accountingAccountCache = new AuxiliaryAccountingAccountCache(accountRepo, _eventAggregator);

        IRepository<CashDrawerGraphQLModel> auxRepo = Substitute.For<IRepository<CashDrawerGraphQLModel>>();
        _auxiliaryCashDrawerCache = new AuxiliaryCashDrawerCache(auxRepo, _eventAggregator);

        IRepository<EntityStringLengthsGraphQLModel> stringLengthRepo = Substitute.For<IRepository<EntityStringLengthsGraphQLModel>>();
        _stringLengthCache = new StringLengthCache(stringLengthRepo);

        JoinableTaskContext jtc = new();
        _joinableTaskFactory = jtc.Factory;
        _validator = new AuxiliaryCashDrawerValidator();

        _vm = new AuxiliaryCashDrawerDetailViewModel(
            _service, _eventAggregator, _accountingAccountCache, _auxiliaryCashDrawerCache,
            _stringLengthCache, _joinableTaskFactory, _validator);
    }

    private static CashDrawerGraphQLModel BuildMajor(int id = 10) => new()
    {
        Id = id,
        Name = "Caja Mayor",
        IsPettyCash = false,
        Parent = null,
        CostCenter = new CostCenterGraphQLModel { Id = 50, Name = "Bogotá" }
    };

    private static CashDrawerGraphQLModel BuildAuxiliary(int id = 5, int parentId = 10) => new()
    {
        Id = id,
        Name = "Caja Auxiliar",
        CashReviewRequired = false,
        AutoAdjustBalance = false,
        AutoTransfer = false,
        IsPettyCash = false,
        ComputerName = "PC-01",
        Parent = new CashDrawerGraphQLModel { Id = parentId, Name = "Caja Mayor" },
        CostCenter = new CostCenterGraphQLModel { Id = 50, Name = "Bogotá" },
        CashAccountingAccount = new AccountingAccountGraphQLModel { Id = 100, Code = "1105", Name = "Caja" },
        CheckAccountingAccount = new AccountingAccountGraphQLModel { Id = 101, Code = "1110", Name = "Cheques" },
        CardAccountingAccount = new AccountingAccountGraphQLModel { Id = 102, Code = "1115", Name = "Tarjetas" }
    };

    #region Constructor null-guards

    [Fact]
    public void Constructor_NullService_Throws()
    {
        System.Action act = () => new AuxiliaryCashDrawerDetailViewModel(null!, _eventAggregator, _accountingAccountCache, _auxiliaryCashDrawerCache, _stringLengthCache, _joinableTaskFactory, _validator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("cashDrawerService");
    }

    [Fact]
    public void Constructor_NullEventAggregator_Throws()
    {
        System.Action act = () => new AuxiliaryCashDrawerDetailViewModel(_service, null!, _accountingAccountCache, _auxiliaryCashDrawerCache, _stringLengthCache, _joinableTaskFactory, _validator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("eventAggregator");
    }

    [Fact]
    public void Constructor_NullAccountingCache_Throws()
    {
        System.Action act = () => new AuxiliaryCashDrawerDetailViewModel(_service, _eventAggregator, null!, _auxiliaryCashDrawerCache, _stringLengthCache, _joinableTaskFactory, _validator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("accountingAccountCache");
    }

    [Fact]
    public void Constructor_NullAuxiliaryCache_Throws()
    {
        System.Action act = () => new AuxiliaryCashDrawerDetailViewModel(_service, _eventAggregator, _accountingAccountCache, null!, _stringLengthCache, _joinableTaskFactory, _validator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("auxiliaryCashDrawerCache");
    }

    [Fact]
    public void Constructor_NullStringLengthCache_Throws()
    {
        System.Action act = () => new AuxiliaryCashDrawerDetailViewModel(_service, _eventAggregator, _accountingAccountCache, _auxiliaryCashDrawerCache, null!, _joinableTaskFactory, _validator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("stringLengthCache");
    }

    [Fact]
    public void Constructor_NullValidator_Throws()
    {
        System.Action act = () => new AuxiliaryCashDrawerDetailViewModel(_service, _eventAggregator, _accountingAccountCache, _auxiliaryCashDrawerCache, _stringLengthCache, _joinableTaskFactory, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("validator");
    }

    #endregion

    #region SetForNew / SetForEdit

    [Fact]
    public void SetForNew_StoresParentMajor()
    {
        _vm.SetForNew(BuildMajor());

        _vm.Id.Should().Be(0);
        _vm.ParentId.Should().Be(10);
        _vm.ParentName.Should().Be("Caja Mayor");
        _vm.CostCenterId.Should().Be(50);
        _vm.IsPettyCash.Should().BeFalse();
        _vm.IsNewRecord.Should().BeTrue();
    }

    [Fact]
    public void SetForNew_DefaultsName_ComputerNameNotEmpty()
    {
        _vm.SetForNew(BuildMajor());

        _vm.Name.Should().Be("CAJA AUXILIAR");
        _vm.ComputerName.Should().NotBeEmpty();
    }

    [Fact]
    public void SetForNew_NullMajor_Throws()
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

        _vm.SetForEdit(BuildAuxiliary());

        _vm.Id.Should().Be(5);
        _vm.Name.Should().Be("Caja Auxiliar");
        _vm.ParentId.Should().Be(10);
        _vm.ParentName.Should().Be("Caja Mayor");
        _vm.ComputerName.Should().Be("PC-01");
        _vm.SelectedCashAccountingAccount.Should().NotBeNull();
        _vm.SelectedCheckAccountingAccount.Should().NotBeNull();
        _vm.SelectedCardAccountingAccount.Should().NotBeNull();
        _vm.IsNewRecord.Should().BeFalse();
    }

    [Fact]
    public void SetForNew_CanSaveTrue_Because_NameAndComputerNameDefaulted()
    {
        // SetForNew aplica Name="CAJA AUXILIAR" y ComputerName="<current>" después de seed
        // → tracker registra cambios → CanSave=true sin intervención del usuario.
        _vm.SetForNew(BuildMajor());

        _vm.CanSave.Should().BeTrue();
    }

    #endregion

    #region CanSave reactivity

    [Fact]
    public void CanSave_False_WhenNameCleared()
    {
        _vm.SetForNew(BuildMajor());

        _vm.Name = "";

        _vm.CanSave.Should().BeFalse();
    }

    [Fact]
    public void CanSave_False_WhenComputerNameCleared()
    {
        _vm.SetForNew(BuildMajor());

        _vm.ComputerName = "";

        _vm.CanSave.Should().BeFalse();
    }

    [Fact]
    public void CanSave_AutoTransferWithoutTarget_ReturnsFalse()
    {
        _vm.SetForNew(BuildMajor());
        _vm.AutoTransfer = true;

        _vm.CanSave.Should().BeFalse();
    }

    [Fact]
    public void CanSave_AutoTransferWithTarget_ReturnsTrue()
    {
        _auxiliaryCashDrawerCache.Add(new CashDrawerGraphQLModel { Id = 99, Name = "Otra Aux", Parent = new CashDrawerGraphQLModel { Id = 10 } });
        _vm.SetForNew(BuildMajor());
        _vm.AutoTransfer = true;
        _vm.SelectedAutoTransferCashDrawer = _auxiliaryCashDrawerCache.Items.First();

        _vm.CanSave.Should().BeTrue();
    }

    [Fact]
    public void AutoTransferCashDrawers_ExcludesSelf()
    {
        _auxiliaryCashDrawerCache.Add(new CashDrawerGraphQLModel { Id = 5, Name = "Yo mismo", Parent = new CashDrawerGraphQLModel { Id = 10 } });
        _auxiliaryCashDrawerCache.Add(new CashDrawerGraphQLModel { Id = 99, Name = "Otra", Parent = new CashDrawerGraphQLModel { Id = 10 } });

        _vm.SetForEdit(BuildAuxiliary(5));

        _vm.AutoTransferCashDrawers.Should().NotContain(c => c.Id == 5);
        _vm.AutoTransferCashDrawers.Should().ContainSingle(c => c.Id == 99);
    }

    #endregion

    #region Save flow

    [Fact]
    public async Task SaveAsync_Create_PublishesCashDrawerCreateMessage()
    {
        _vm.SetForNew(BuildMajor());

        UpsertResponseType<CashDrawerGraphQLModel> response = new() { Success = true, Entity = BuildAuxiliary() };
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

        _vm.SetForEdit(BuildAuxiliary());
        _vm.Name = "Caja Renombrada";

        UpsertResponseType<CashDrawerGraphQLModel> response = new() { Success = true, Entity = BuildAuxiliary() };
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
        _vm.SetForNew(BuildMajor());

        UpsertResponseType<CashDrawerGraphQLModel> response = new() { Success = true, Entity = BuildAuxiliary() };
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
    public void ClearingName_RaisesErrorForName()
    {
        _vm.SetForNew(BuildMajor());
        _vm.Name = "";

        _vm.HasErrors.Should().BeTrue();
        _vm.GetErrors(nameof(AuxiliaryCashDrawerValidationContext.Name)).Cast<string>().Should().NotBeEmpty();
    }

    [Fact]
    public void ClearingComputerName_RaisesError()
    {
        _vm.SetForNew(BuildMajor());
        _vm.ComputerName = "";

        _vm.HasErrors.Should().BeTrue();
        _vm.GetErrors(nameof(AuxiliaryCashDrawerValidationContext.ComputerName)).Cast<string>().Should().NotBeEmpty();
    }

    [Fact]
    public void EnablingAutoTransferWithoutTarget_RaisesError()
    {
        _vm.SetForNew(BuildMajor());
        _vm.AutoTransfer = true;

        _vm.HasErrors.Should().BeTrue();
        _vm.GetErrors(nameof(AuxiliaryCashDrawerValidationContext.AutoTransferCashDrawerId)).Cast<string>().Should().NotBeEmpty();
    }

    #endregion
}
