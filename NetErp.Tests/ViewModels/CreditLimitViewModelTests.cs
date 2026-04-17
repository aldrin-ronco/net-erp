using System;
using AutoMapper;
using Caliburn.Micro;
using Common.Interfaces;
using Common.Validators;
using FluentAssertions;
using Microsoft.VisualStudio.Threading;
using Models.Billing;
using NetErp.Billing.CreditLimit.ViewModels;
using NetErp.Helpers;
using NetErp.Helpers.Services;
using NSubstitute;
using Xunit;

namespace NetErp.Tests.ViewModels;

public class CreditLimitViewModelTests
{
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly IEventAggregator _eventAggregator = Substitute.For<IEventAggregator>();
    private readonly INotificationService _notificationService = Substitute.For<INotificationService>();
    private readonly ICreditLimitValidator _validator = Substitute.For<ICreditLimitValidator>();
    private readonly IRepository<CreditLimitGraphQLModel> _service = Substitute.For<IRepository<CreditLimitGraphQLModel>>();
    private readonly IBackgroundQueueService _backgroundQueueService = Substitute.For<IBackgroundQueueService>();
    private readonly JoinableTaskFactory _joinableTaskFactory = new JoinableTaskContext().Factory;
    private readonly DebouncedAction _searchDebounce = new(delayMs: 30);

    private CreditLimitViewModel BuildConductor() => new(
        _mapper, _eventAggregator, _notificationService, _validator, _service, _backgroundQueueService, _joinableTaskFactory, _searchDebounce);

    [Fact]
    public void Constructor_NullAutoMapper_Throws()
    {
        System.Action act = () => new CreditLimitViewModel(
            null!, _eventAggregator, _notificationService, _validator, _service, _backgroundQueueService, _joinableTaskFactory, _searchDebounce);
        act.Should().Throw<ArgumentNullException>().WithParameterName("autoMapper");
    }

    [Fact]
    public void Constructor_NullEventAggregator_Throws()
    {
        System.Action act = () => new CreditLimitViewModel(
            _mapper, null!, _notificationService, _validator, _service, _backgroundQueueService, _joinableTaskFactory, _searchDebounce);
        act.Should().Throw<ArgumentNullException>().WithParameterName("eventAggregator");
    }

    [Fact]
    public void Constructor_NullNotificationService_Throws()
    {
        System.Action act = () => new CreditLimitViewModel(
            _mapper, _eventAggregator, null!, _validator, _service, _backgroundQueueService, _joinableTaskFactory, _searchDebounce);
        act.Should().Throw<ArgumentNullException>().WithParameterName("notificationService");
    }

    [Fact]
    public void Constructor_NullValidator_Throws()
    {
        System.Action act = () => new CreditLimitViewModel(
            _mapper, _eventAggregator, _notificationService, null!, _service, _backgroundQueueService, _joinableTaskFactory, _searchDebounce);
        act.Should().Throw<ArgumentNullException>().WithParameterName("validator");
    }

    [Fact]
    public void Constructor_NullService_Throws()
    {
        System.Action act = () => new CreditLimitViewModel(
            _mapper, _eventAggregator, _notificationService, _validator, null!, _backgroundQueueService, _joinableTaskFactory, _searchDebounce);
        act.Should().Throw<ArgumentNullException>().WithParameterName("creditLimitService");
    }

    [Fact]
    public void Constructor_NullBackgroundQueue_Throws()
    {
        System.Action act = () => new CreditLimitViewModel(
            _mapper, _eventAggregator, _notificationService, _validator, _service, null!, _joinableTaskFactory, _searchDebounce);
        act.Should().Throw<ArgumentNullException>().WithParameterName("backgroundQueueService");
    }

    [Fact]
    public void Constructor_NullSearchDebounce_Throws()
    {
        System.Action act = () => new CreditLimitViewModel(
            _mapper, _eventAggregator, _notificationService, _validator, _service, _backgroundQueueService, _joinableTaskFactory, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("searchDebounce");
    }

    [Fact]
    public void CreditLimitMasterViewModel_LazilyInstantiated_AndCached()
    {
        CreditLimitViewModel conductor = BuildConductor();

        CreditLimitMasterViewModel first = conductor.CreditLimitMasterViewModel;
        CreditLimitMasterViewModel second = conductor.CreditLimitMasterViewModel;

        first.Should().BeSameAs(second);
        first.Context.Should().BeSameAs(conductor);
    }
}
