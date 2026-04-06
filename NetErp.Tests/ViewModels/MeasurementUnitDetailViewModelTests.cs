using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Caliburn.Micro;
using Common.Interfaces;
using FluentAssertions;
using Microsoft.VisualStudio.Threading;
using Models.Global;
using Models.Inventory;
using NetErp.Helpers.Cache;
using NetErp.Inventory.MeasurementUnits.Validators;
using NetErp.Inventory.MeasurementUnits.ViewModels;
using NSubstitute;
using Xunit;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Tests.ViewModels;

public class MeasurementUnitDetailViewModelTests
{
    private readonly IRepository<MeasurementUnitGraphQLModel> _service;
    private readonly IEventAggregator _eventAggregator;
    private readonly StringLengthCache _stringLengthCache;
    private readonly JoinableTaskFactory _joinableTaskFactory;
    private readonly MeasurementUnitValidator _validator;
    private readonly MeasurementUnitDetailViewModel _vm;

    public MeasurementUnitDetailViewModelTests()
    {
        _service = Substitute.For<IRepository<MeasurementUnitGraphQLModel>>();
        _eventAggregator = Substitute.For<IEventAggregator>();

        var stringLengthRepo = Substitute.For<IRepository<EntityStringLengthsGraphQLModel>>();
        _stringLengthCache = new StringLengthCache(stringLengthRepo);

        var jtc = new JoinableTaskContext();
        _joinableTaskFactory = jtc.Factory;

        _validator = new MeasurementUnitValidator(); // Real validator — no mock needed
        _vm = new MeasurementUnitDetailViewModel(_service, _eventAggregator, _stringLengthCache, _joinableTaskFactory, _validator);
    }

    #region SetForNew

    [Fact]
    public void SetForNew_SetsDefaults()
    {
        _vm.SetForNew();

        _vm.Id.Should().Be(0);
        _vm.Name.Should().BeEmpty();
        _vm.Abbreviation.Should().BeEmpty();
        _vm.Type.Should().BeEmpty();
        _vm.DianCode.Should().BeEmpty();
        _vm.IsNewRecord.Should().BeTrue();
    }

    [Fact]
    public void SetForNew_NoInitialChanges()
    {
        _vm.SetForNew();

        _vm.CanSave.Should().BeFalse();
    }

    #endregion

    #region SetForEdit

    [Fact]
    public void SetForEdit_PopulatesFromModel()
    {
        var entity = new MeasurementUnitGraphQLModel
        {
            Id = 5, Name = "Kilogramo", Abbreviation = "Kg", Type = "Peso", DianCode = "001"
        };

        _vm.SetForEdit(entity);

        _vm.Id.Should().Be(5);
        _vm.Name.Should().Be("Kilogramo");
        _vm.Abbreviation.Should().Be("Kg");
        _vm.Type.Should().Be("Peso");
        _vm.DianCode.Should().Be("001");
        _vm.IsNewRecord.Should().BeFalse();
    }

    [Fact]
    public void SetForEdit_NoInitialChanges()
    {
        var entity = new MeasurementUnitGraphQLModel
        {
            Id = 5, Name = "Kilogramo", Abbreviation = "Kg", Type = "Peso", DianCode = "001"
        };

        _vm.SetForEdit(entity);

        _vm.CanSave.Should().BeFalse();
    }

    #endregion

    #region CanSave

    [Fact]
    public void CanSave_ValidAndChanged_ReturnsTrue()
    {
        var entity = new MeasurementUnitGraphQLModel
        {
            Id = 5, Name = "Kilogramo", Abbreviation = "Kg", Type = "Peso", DianCode = "001"
        };
        _vm.SetForEdit(entity);

        _vm.Name = "Kilogramo Modificado";

        _vm.CanSave.Should().BeTrue();
    }

    [Fact]
    public void CanSave_EmptyName_ReturnsFalse()
    {
        _vm.SetForNew();
        _vm.Name = "Algo";
        _vm.Abbreviation = "A";
        _vm.Type = "T";
        _vm.DianCode = "D";

        _vm.Name = ""; // clear → should invalidate

        _vm.CanSave.Should().BeFalse();
    }

    #endregion

    #region Validation

    [Fact]
    public void Name_SetEmpty_AddsValidationError()
    {
        _vm.SetForNew();
        _vm.Name = "Valid";

        _vm.Name = ""; // trigger error

        _vm.HasErrors.Should().BeTrue();
        var errors = _vm.GetErrors(nameof(MeasurementUnitDetailViewModel.Name)).Cast<string>().ToList();
        errors.Should().NotBeEmpty();
    }

    [Fact]
    public void Name_SetValid_ClearsValidationError()
    {
        _vm.SetForNew();
        _vm.Name = "Valid";
        _vm.Name = ""; // trigger error

        _vm.Name = "Valid Again"; // clear error

        _vm.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void ErrorsChanged_FiredOnValidation()
    {
        _vm.SetForNew();
        _vm.Name = "Valid";
        var firedProperties = new List<string>();
        _vm.ErrorsChanged += (_, e) => firedProperties.Add(e.PropertyName!);

        _vm.Name = ""; // triggers error

        firedProperties.Should().Contain("Name");
    }

    #endregion

    #region ExecuteSaveAsync

    [Fact]
    public async Task ExecuteSaveAsync_NewRecord_CallsCreateAsync()
    {
        var expectedResult = new UpsertResponseType<MeasurementUnitGraphQLModel>
        {
            Entity = new MeasurementUnitGraphQLModel { Id = 1, Name = "Kg", Abbreviation = "Kg", Type = "Peso", DianCode = "001" },
            Success = true,
            Message = "OK"
        };
        _service.CreateAsync<UpsertResponseType<MeasurementUnitGraphQLModel>>(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        _vm.SetForNew();
        _vm.Name = "Kg";
        _vm.Abbreviation = "Kg";
        _vm.Type = "Peso";
        _vm.DianCode = "001";

        var result = await _vm.ExecuteSaveAsync();

        result.Success.Should().BeTrue();
        await _service.Received(1).CreateAsync<UpsertResponseType<MeasurementUnitGraphQLModel>>(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteSaveAsync_ExistingRecord_CallsUpdateAsync()
    {
        var expectedResult = new UpsertResponseType<MeasurementUnitGraphQLModel>
        {
            Entity = new MeasurementUnitGraphQLModel { Id = 5, Name = "Kg Mod", Abbreviation = "Kg", Type = "Peso", DianCode = "001" },
            Success = true,
            Message = "OK"
        };
        _service.UpdateAsync<UpsertResponseType<MeasurementUnitGraphQLModel>>(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        _vm.SetForEdit(new MeasurementUnitGraphQLModel
        {
            Id = 5, Name = "Kg", Abbreviation = "Kg", Type = "Peso", DianCode = "001"
        });
        _vm.Name = "Kg Mod";

        var result = await _vm.ExecuteSaveAsync();

        result.Success.Should().BeTrue();
        await _service.Received(1).UpdateAsync<UpsertResponseType<MeasurementUnitGraphQLModel>>(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    #endregion
}
