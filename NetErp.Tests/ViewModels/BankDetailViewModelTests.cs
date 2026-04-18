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
using IDialogService = NetErp.Helpers.IDialogService;

namespace NetErp.Tests.ViewModels;

public class BankDetailViewModelTests
{
    private readonly IRepository<BankGraphQLModel> _service;
    private readonly IEventAggregator _eventAggregator;
    private readonly IDialogService _dialogService;
    private readonly StringLengthCache _stringLengthCache;
    private readonly JoinableTaskFactory _joinableTaskFactory;
    private readonly BankValidator _validator;
    private readonly BankDetailViewModel _vm;

    public BankDetailViewModelTests()
    {
        _service = Substitute.For<IRepository<BankGraphQLModel>>();
        _eventAggregator = Substitute.For<IEventAggregator>();
        _dialogService = Substitute.For<IDialogService>();

        IRepository<EntityStringLengthsGraphQLModel> stringLengthRepo = Substitute.For<IRepository<EntityStringLengthsGraphQLModel>>();
        _stringLengthCache = new StringLengthCache(stringLengthRepo);

        JoinableTaskContext jtc = new();
        _joinableTaskFactory = jtc.Factory;
        _validator = new BankValidator();

        _vm = new BankDetailViewModel(_service, _eventAggregator, _dialogService, _stringLengthCache, _joinableTaskFactory, _validator);
    }

    private static BankGraphQLModel Build(int id = 1, string code = "001", string prefix = "Z") => new()
    {
        Id = id,
        Code = code,
        PaymentMethodPrefix = prefix,
        AccountingEntity = new AccountingEntityGraphQLModel { Id = 42, SearchName = "Bancolombia" }
    };

    #region Constructor null-guards

    [Fact]
    public void Constructor_NullService_Throws()
    {
        System.Action act = () => new BankDetailViewModel(null!, _eventAggregator, _dialogService, _stringLengthCache, _joinableTaskFactory, _validator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("bankService");
    }

    [Fact]
    public void Constructor_NullEventAggregator_Throws()
    {
        System.Action act = () => new BankDetailViewModel(_service, null!, _dialogService, _stringLengthCache, _joinableTaskFactory, _validator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("eventAggregator");
    }

    [Fact]
    public void Constructor_NullDialogService_Throws()
    {
        System.Action act = () => new BankDetailViewModel(_service, _eventAggregator, null!, _stringLengthCache, _joinableTaskFactory, _validator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("dialogService");
    }

    [Fact]
    public void Constructor_NullStringLengthCache_Throws()
    {
        System.Action act = () => new BankDetailViewModel(_service, _eventAggregator, _dialogService, null!, _joinableTaskFactory, _validator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("stringLengthCache");
    }

    [Fact]
    public void Constructor_NullValidator_Throws()
    {
        System.Action act = () => new BankDetailViewModel(_service, _eventAggregator, _dialogService, _stringLengthCache, _joinableTaskFactory, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("validator");
    }

    #endregion

    #region SetForNew / SetForEdit

    [Fact]
    public void SetForNew_SetsDefaults()
    {
        _vm.SetForNew();

        _vm.Id.Should().Be(0);
        _vm.Code.Should().BeEmpty();
        _vm.AccountingEntityId.Should().Be(0);
        _vm.AccountingEntityName.Should().BeEmpty();
        _vm.PaymentMethodPrefix.Should().Be("Z");
        _vm.IsNewRecord.Should().BeTrue();
    }

    [Fact]
    public void SetForNew_NoChanges_CanSaveFalse()
    {
        _vm.SetForNew();
        _vm.CanSave.Should().BeFalse();
    }

    [Fact]
    public void SetForEdit_PopulatesFromModel()
    {
        _vm.SetForEdit(Build(5, "777", "Y"));

        _vm.Id.Should().Be(5);
        _vm.Code.Should().Be("777");
        _vm.PaymentMethodPrefix.Should().Be("Y");
        _vm.AccountingEntityId.Should().Be(42);
        _vm.AccountingEntityName.Should().Be("Bancolombia");
        _vm.IsNewRecord.Should().BeFalse();
    }

    [Fact]
    public void SetForEdit_NoChanges_CanSaveFalse()
    {
        _vm.SetForEdit(Build());
        _vm.CanSave.Should().BeFalse();
    }

    #endregion

    #region CanSave reactivity

    [Fact]
    public void CanSave_TurnsTrue_WhenEditingModifiesCode()
    {
        _vm.SetForEdit(Build());

        _vm.Code = "999";

        _vm.CanSave.Should().BeTrue();
    }

    [Fact]
    public void CanSave_False_WhenCodeBecomesInvalid()
    {
        _vm.SetForEdit(Build());

        _vm.Code = "AB";

        _vm.CanSave.Should().BeFalse();
    }

    [Fact]
    public void CanSave_NewRecord_RequiresAllFields()
    {
        _vm.SetForNew();
        _vm.Code = "007";
        _vm.CanSave.Should().BeFalse("falta AccountingEntity");

        _vm.AccountingEntityId = 42;
        _vm.AccountingEntityName = "Bancolombia";
        _vm.CanSave.Should().BeTrue();
    }

    #endregion

    #region Save flow

    [Fact]
    public async Task SaveAsync_Create_PublishesBankCreateMessage()
    {
        _vm.SetForNew();
        _vm.Code = "007";
        _vm.PaymentMethodPrefix = "Z";
        _vm.AccountingEntityId = 42;
        _vm.AccountingEntityName = "Bancolombia";

        UpsertResponseType<BankGraphQLModel> response = new()
        {
            Success = true,
            Message = "OK",
            Entity = Build()
        };
        _service.CreateAsync<UpsertResponseType<BankGraphQLModel>>(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(response);

        await _vm.SaveAsync();

        await _eventAggregator.Received(1).PublishOnCurrentThreadAsync(
            Arg.Is<BankCreateMessage>(m => m.CreatedBank.Success),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveAsync_Update_PublishesBankUpdateMessage()
    {
        _vm.SetForEdit(Build());
        _vm.Code = "999"; // dispara change

        UpsertResponseType<BankGraphQLModel> response = new()
        {
            Success = true,
            Message = "OK",
            Entity = Build(code: "999")
        };
        _service.UpdateAsync<UpsertResponseType<BankGraphQLModel>>(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(response);

        await _vm.SaveAsync();

        await _eventAggregator.Received(1).PublishOnCurrentThreadAsync(
            Arg.Is<BankUpdateMessage>(m => m.UpdatedBank.Success),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteSaveAsync_NewRecord_CallsCreateAsync()
    {
        _vm.SetForNew();
        _vm.Code = "007";
        _vm.AccountingEntityId = 42;
        _vm.AccountingEntityName = "Bancolombia";

        UpsertResponseType<BankGraphQLModel> response = new() { Success = true, Entity = Build() };
        _service.CreateAsync<UpsertResponseType<BankGraphQLModel>>(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(response);

        UpsertResponseType<BankGraphQLModel> result = await _vm.ExecuteSaveAsync();

        result.Success.Should().BeTrue();
        await _service.Received(1).CreateAsync<UpsertResponseType<BankGraphQLModel>>(
            Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteSaveAsync_ExistingRecord_CallsUpdateAsync()
    {
        _vm.SetForEdit(Build());
        _vm.Code = "999";

        UpsertResponseType<BankGraphQLModel> response = new() { Success = true, Entity = Build(code: "999") };
        _service.UpdateAsync<UpsertResponseType<BankGraphQLModel>>(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(response);

        UpsertResponseType<BankGraphQLModel> result = await _vm.ExecuteSaveAsync();

        result.Success.Should().BeTrue();
        await _service.Received(1).UpdateAsync<UpsertResponseType<BankGraphQLModel>>(
            Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region INotifyDataErrorInfo

    [Fact]
    public void SettingInvalidCode_RaisesErrorForCode()
    {
        _vm.SetForEdit(Build());

        _vm.Code = "A";

        _vm.HasErrors.Should().BeTrue();
        _vm.GetErrors(nameof(BankValidationContext.Code)).Cast<string>().Should().NotBeEmpty();
    }

    [Fact]
    public void SettingValidCode_ClearsErrorForCode()
    {
        _vm.SetForEdit(Build());
        _vm.Code = "A";
        _vm.HasErrors.Should().BeTrue();

        _vm.Code = "555";

        _vm.GetErrors(nameof(BankValidationContext.Code)).Cast<string>().Should().BeEmpty();
    }

    #endregion
}
