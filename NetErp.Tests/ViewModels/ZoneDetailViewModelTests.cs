using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Caliburn.Micro;
using Common.Interfaces;
using FluentAssertions;
using Microsoft.VisualStudio.Threading;
using Models.Billing;
using Models.Global;
using NetErp.Billing.Zones.Validators;
using NetErp.Billing.Zones.ViewModels;
using NetErp.Helpers.Cache;
using NSubstitute;
using Xunit;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Tests.ViewModels;

public class ZoneDetailViewModelTests
{
    private readonly IRepository<ZoneGraphQLModel> _zoneService;
    private readonly IEventAggregator _eventAggregator;
    private readonly StringLengthCache _stringLengthCache;
    private readonly JoinableTaskFactory _joinableTaskFactory;
    private readonly ZoneDetailViewModel _vm;

    public ZoneDetailViewModelTests()
    {
        _zoneService = Substitute.For<IRepository<ZoneGraphQLModel>>();
        _eventAggregator = Substitute.For<IEventAggregator>();

        // Real StringLengthCache with mocked repository
        var stringLengthRepo = Substitute.For<IRepository<EntityStringLengthsGraphQLModel>>();
        _stringLengthCache = new StringLengthCache(stringLengthRepo);

        var jtc = new JoinableTaskContext();
        _joinableTaskFactory = jtc.Factory;

        _vm = new ZoneDetailViewModel(_zoneService, _eventAggregator, _stringLengthCache, _joinableTaskFactory, new ZoneValidator());
    }

    #region SetForNew

    [Fact]
    public void SetForNew_SetsDefaults()
    {
        _vm.SetForNew();

        _vm.Id.Should().Be(0);
        _vm.Name.Should().BeEmpty();
        _vm.IsActive.Should().BeTrue();
        _vm.IsNewRecord.Should().BeTrue();
    }

    [Fact]
    public void SetForNew_AcceptsChanges_NoInitialChanges()
    {
        _vm.SetForNew();

        _vm.CanSave.Should().BeFalse("no hay cambios después de SetForNew");
    }

    #endregion

    #region SetForEdit

    [Fact]
    public void SetForEdit_PopulatesFromModel()
    {
        var zone = new ZoneGraphQLModel { Id = 5, Name = "Norte", IsActive = false };

        _vm.SetForEdit(zone);

        _vm.Id.Should().Be(5);
        _vm.Name.Should().Be("Norte");
        _vm.IsActive.Should().BeFalse();
        _vm.IsNewRecord.Should().BeFalse();
    }

    [Fact]
    public void SetForEdit_AcceptsChanges_NoInitialChanges()
    {
        var zone = new ZoneGraphQLModel { Id = 5, Name = "Norte", IsActive = true };

        _vm.SetForEdit(zone);

        _vm.CanSave.Should().BeFalse("no hay cambios después de SetForEdit");
    }

    #endregion

    #region CanSave

    [Fact]
    public void CanSave_EmptyName_ReturnsFalse()
    {
        _vm.SetForNew();
        _vm.Name = "";

        _vm.CanSave.Should().BeFalse();
    }

    [Fact]
    public void CanSave_NoChanges_ReturnsFalse()
    {
        var zone = new ZoneGraphQLModel { Id = 5, Name = "Norte", IsActive = true };
        _vm.SetForEdit(zone);

        // No changes made
        _vm.CanSave.Should().BeFalse();
    }

    [Fact]
    public void CanSave_ValidNameAndHasChanges_ReturnsTrue()
    {
        var zone = new ZoneGraphQLModel { Id = 5, Name = "Norte", IsActive = true };
        _vm.SetForEdit(zone);

        _vm.Name = "Norte Actualizado";

        _vm.CanSave.Should().BeTrue();
    }

    [Fact]
    public void CanSave_WhitespaceOnlyName_ReturnsFalse()
    {
        _vm.SetForNew();
        _vm.Name = "   ";

        _vm.CanSave.Should().BeFalse();
    }

    #endregion

    #region Name Property

    [Fact]
    public void Name_Set_RaisesPropertyChanged()
    {
        var changedProperties = new List<string>();
        _vm.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName!);
        _vm.SetForNew();
        changedProperties.Clear();

        _vm.Name = "Test";

        changedProperties.Should().Contain(nameof(ZoneDetailViewModel.Name));
        changedProperties.Should().Contain(nameof(ZoneDetailViewModel.CanSave));
    }

    [Fact]
    public void Name_SetEmpty_AddsValidationError()
    {
        _vm.SetForNew();
        _vm.Name = "Something"; // first set a valid name

        _vm.Name = ""; // now clear it → should trigger validation error

        _vm.HasErrors.Should().BeTrue();
        var errors = _vm.GetErrors(nameof(ZoneDetailViewModel.Name)).Cast<string>().ToList();
        errors.Should().NotBeEmpty();
    }

    [Fact]
    public void Name_SetValid_ClearsValidationError()
    {
        _vm.SetForNew();
        _vm.Name = ""; // trigger error
        _vm.Name = "Valid Name"; // clear error

        _vm.HasErrors.Should().BeFalse();
    }

    #endregion

    #region IsNewRecord

    [Fact]
    public void IsNewRecord_IdZero_ReturnsTrue()
    {
        _vm.Id = 0;

        _vm.IsNewRecord.Should().BeTrue();
    }

    [Fact]
    public void IsNewRecord_IdNonZero_ReturnsFalse()
    {
        _vm.Id = 10;

        _vm.IsNewRecord.Should().BeFalse();
    }

    #endregion

    #region NameMaxLength

    [Fact]
    public void NameMaxLength_ReturnsValueFromCache()
    {
        // StringLengthCache not loaded → returns 0
        _vm.NameMaxLength.Should().Be(0);
    }

    #endregion

    #region ExecuteSaveAsync

    [Fact]
    public async Task ExecuteSaveAsync_NewRecord_CallsCreateAsync()
    {
        var expectedResult = new UpsertResponseType<ZoneGraphQLModel>
        {
            Entity = new ZoneGraphQLModel { Id = 1, Name = "Nueva", IsActive = true },
            Success = true,
            Message = "OK"
        };
        _zoneService.CreateAsync<UpsertResponseType<ZoneGraphQLModel>>(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        _vm.SetForNew();
        _vm.Name = "Nueva";

        var result = await _vm.ExecuteSaveAsync();

        result.Success.Should().BeTrue();
        result.Entity.Name.Should().Be("Nueva");
        await _zoneService.Received(1).CreateAsync<UpsertResponseType<ZoneGraphQLModel>>(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteSaveAsync_ExistingRecord_CallsUpdateAsync()
    {
        var expectedResult = new UpsertResponseType<ZoneGraphQLModel>
        {
            Entity = new ZoneGraphQLModel { Id = 5, Name = "Actualizada", IsActive = true },
            Success = true,
            Message = "OK"
        };
        _zoneService.UpdateAsync<UpsertResponseType<ZoneGraphQLModel>>(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        _vm.SetForEdit(new ZoneGraphQLModel { Id = 5, Name = "Original", IsActive = true });
        _vm.Name = "Actualizada";

        var result = await _vm.ExecuteSaveAsync();

        result.Success.Should().BeTrue();
        result.Entity.Name.Should().Be("Actualizada");
        await _zoneService.Received(1).UpdateAsync<UpsertResponseType<ZoneGraphQLModel>>(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region INotifyDataErrorInfo

    [Fact]
    public void ErrorsChanged_FiredWhenValidationChanges()
    {
        _vm.SetForNew();
        _vm.Name = "Valid"; // set something valid first
        var firedProperties = new List<string>();
        _vm.ErrorsChanged += (_, e) => firedProperties.Add(e.PropertyName!);

        _vm.Name = ""; // now clear → triggers validation error

        firedProperties.Should().Contain(nameof(ZoneDetailViewModel.Name));
    }

    [Fact]
    public void GetErrors_InvalidPropertyName_ReturnsEmpty()
    {
        var errors = _vm.GetErrors("NonExistent").Cast<string>().ToList();

        errors.Should().BeEmpty();
    }

    #endregion
}
