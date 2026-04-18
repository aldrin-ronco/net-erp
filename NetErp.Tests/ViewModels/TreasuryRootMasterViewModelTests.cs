using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using AutoMapper;
using Caliburn.Micro;
using Common.Interfaces;
using FluentAssertions;
using Microsoft.VisualStudio.Threading;
using Models.Books;
using Models.Global;
using Models.Treasury;
using NetErp.Helpers.Cache;
using NetErp.Treasury.Masters.DTO;
using NetErp.Treasury.Masters.Validators;
using NetErp.Treasury.Masters.ViewModels;
using NSubstitute;
using Xunit;
using static Models.Global.GraphQLResponseTypes;
using IDialogService = NetErp.Helpers.IDialogService;
using INotificationService = NetErp.Helpers.Services.INotificationService;

namespace NetErp.Tests.ViewModels;

/// <summary>
/// Fixture que garantiza que exista una <see cref="System.Windows.Application"/>
/// para los handlers del master que usan <c>Application.Current.Dispatcher.Invoke</c>.
///
/// <para>WPF Application requiere un hilo STA con dispatcher. Creamos ese hilo una
/// sola vez por dominio y dejamos al Application pumping su message loop. Todos los
/// <c>Application.Current.Dispatcher.Invoke</c> se marshalan a ese hilo y ejecutan
/// sincronamente bloqueando el hilo de test hasta completar.</para>
/// </summary>
public sealed class WpfApplicationFixture
{
    private static readonly object _lock = new();
    private static System.Threading.Thread? _staThread;

    public WpfApplicationFixture()
    {
        lock (_lock)
        {
            if (Application.Current is not null) return;

            System.Threading.ManualResetEventSlim ready = new(false);
            _staThread = new System.Threading.Thread(() =>
            {
                _ = new Application();
                ready.Set();
                // Bombea el dispatcher para que Invoke desde otros hilos funcione.
                System.Windows.Threading.Dispatcher.Run();
            })
            {
                IsBackground = true,
                Name = "WpfApplicationFixture-STA"
            };
            _staThread.SetApartmentState(System.Threading.ApartmentState.STA);
            _staThread.Start();
            ready.Wait();
        }
    }
}

[CollectionDefinition("WpfApp")]
public class WpfApplicationCollection : ICollectionFixture<WpfApplicationFixture> { }

/// <summary>
/// Integration tests for <see cref="TreasuryRootMasterViewModel"/> — tree insert/update/delete
/// handlers, polymorphic dispatch for create/edit, y null-guards.
/// </summary>
[Collection("WpfApp")]
public class TreasuryRootMasterViewModelTests
{
    private readonly IMapper _mapper;
    private readonly IEventAggregator _eventAggregator;
    private readonly IRepository<CashDrawerGraphQLModel> _cashDrawerService;
    private readonly IRepository<BankGraphQLModel> _bankService;
    private readonly IRepository<BankAccountGraphQLModel> _bankAccountService;
    private readonly IRepository<FranchiseGraphQLModel> _franchiseService;
    private readonly IDialogService _dialogService;
    private readonly INotificationService _notificationService;
    private readonly IGraphQLClient _graphQLClient;
    private readonly AuxiliaryAccountingAccountCache _auxiliaryAccountingAccountCache;
    private readonly CompanyLocationCache _companyLocationCache;
    private readonly CostCenterCache _costCenterCache;
    private readonly BankAccountCache _bankAccountCache;
    private readonly MajorCashDrawerCache _majorCashDrawerCache;
    private readonly MinorCashDrawerCache _minorCashDrawerCache;
    private readonly AuxiliaryCashDrawerCache _auxiliaryCashDrawerCache;
    private readonly BankCache _bankCache;
    private readonly FranchiseCache _franchiseCache;
    private readonly StringLengthCache _stringLengthCache;
    private readonly JoinableTaskFactory _joinableTaskFactory;
    private readonly BankValidator _bankValidator;
    private readonly BankAccountValidator _bankAccountValidator;
    private readonly FranchiseValidator _franchiseValidator;
    private readonly MajorCashDrawerValidator _majorCashDrawerValidator;
    private readonly MinorCashDrawerValidator _minorCashDrawerValidator;
    private readonly AuxiliaryCashDrawerValidator _auxiliaryCashDrawerValidator;
    private readonly TreasuryRootViewModel _context;
    private readonly TreasuryRootMasterViewModel _vm;

    public TreasuryRootMasterViewModelTests()
    {
        _mapper = Substitute.For<IMapper>();
        _eventAggregator = Substitute.For<IEventAggregator>();
        _cashDrawerService = Substitute.For<IRepository<CashDrawerGraphQLModel>>();
        _bankService = Substitute.For<IRepository<BankGraphQLModel>>();
        _bankAccountService = Substitute.For<IRepository<BankAccountGraphQLModel>>();
        _franchiseService = Substitute.For<IRepository<FranchiseGraphQLModel>>();
        _dialogService = Substitute.For<IDialogService>();
        _notificationService = Substitute.For<INotificationService>();
        _graphQLClient = Substitute.For<IGraphQLClient>();

        _auxiliaryAccountingAccountCache = new AuxiliaryAccountingAccountCache(
            Substitute.For<IRepository<AccountingAccountGraphQLModel>>(), _eventAggregator);
        _companyLocationCache = new CompanyLocationCache(
            Substitute.For<IRepository<CompanyLocationGraphQLModel>>(), _eventAggregator);
        _costCenterCache = new CostCenterCache(
            Substitute.For<IRepository<CostCenterGraphQLModel>>(), _eventAggregator);
        _bankAccountCache = new BankAccountCache(_bankAccountService, _eventAggregator);
        _majorCashDrawerCache = new MajorCashDrawerCache(_cashDrawerService, _eventAggregator);
        _minorCashDrawerCache = new MinorCashDrawerCache(_cashDrawerService, _eventAggregator);
        _auxiliaryCashDrawerCache = new AuxiliaryCashDrawerCache(_cashDrawerService, _eventAggregator);
        _bankCache = new BankCache(_bankService, _eventAggregator);
        _franchiseCache = new FranchiseCache(_franchiseService, _eventAggregator);

        IRepository<EntityStringLengthsGraphQLModel> stringLengthRepo =
            Substitute.For<IRepository<EntityStringLengthsGraphQLModel>>();
        _stringLengthCache = new StringLengthCache(stringLengthRepo);

        _joinableTaskFactory = new JoinableTaskContext().Factory;

        _bankValidator = new BankValidator();
        _bankAccountValidator = new BankAccountValidator();
        _franchiseValidator = new FranchiseValidator();
        _majorCashDrawerValidator = new MajorCashDrawerValidator();
        _minorCashDrawerValidator = new MinorCashDrawerValidator();
        _auxiliaryCashDrawerValidator = new AuxiliaryCashDrawerValidator();

        ConfigureMapperDefaults();

        _context = new TreasuryRootViewModel(
            _mapper, _eventAggregator, _cashDrawerService, _bankService,
            _bankAccountService, _franchiseService, _dialogService, _notificationService,
            _auxiliaryAccountingAccountCache, _companyLocationCache, _costCenterCache,
            _bankAccountCache, _majorCashDrawerCache, _minorCashDrawerCache,
            _auxiliaryCashDrawerCache, _bankCache, _franchiseCache, _graphQLClient,
            _stringLengthCache, _joinableTaskFactory,
            _bankValidator, _bankAccountValidator, _franchiseValidator,
            _majorCashDrawerValidator, _minorCashDrawerValidator, _auxiliaryCashDrawerValidator);

        _vm = BuildMaster();
    }

    /// <summary>
    /// Construye el master evitando el path del Conductor (<see cref="TreasuryRootViewModel"/>)
    /// que arranca una <c>Task.Run(ActivateMasterView)</c> — innecesario para tests.
    /// </summary>
    private TreasuryRootMasterViewModel BuildMaster()
    {
        return new TreasuryRootMasterViewModel(
            _context, _cashDrawerService, _bankService, _bankAccountService, _franchiseService,
            _dialogService, _notificationService,
            _auxiliaryAccountingAccountCache, _companyLocationCache, _costCenterCache,
            _bankAccountCache, _majorCashDrawerCache, _minorCashDrawerCache,
            _auxiliaryCashDrawerCache, _bankCache, _franchiseCache, _graphQLClient,
            _stringLengthCache, _joinableTaskFactory,
            _bankValidator, _bankAccountValidator, _franchiseValidator,
            _majorCashDrawerValidator, _minorCashDrawerValidator, _auxiliaryCashDrawerValidator);
    }

    /// <summary>
    /// Configura el mock de <see cref="IMapper"/> para producir DTOs de árbol con los Ids
    /// y campos mínimos que usan los handlers (AccountingEntity, PaymentMethodPrefix, etc).
    /// </summary>
    private void ConfigureMapperDefaults()
    {
        _mapper.Map<TreasuryBankMasterTreeDTO>(Arg.Any<BankGraphQLModel>())
            .Returns(ci =>
            {
                BankGraphQLModel src = ci.Arg<BankGraphQLModel>();
                return new TreasuryBankMasterTreeDTO
                {
                    Id = src.Id,
                    Code = src.Code,
                    PaymentMethodPrefix = src.PaymentMethodPrefix,
                    AccountingEntity = new AccountingEntityDTO { Id = src.AccountingEntity.Id, SearchName = src.AccountingEntity.SearchName }
                };
            });

        _mapper.Map<TreasuryBankAccountMasterTreeDTO>(Arg.Any<BankAccountGraphQLModel>())
            .Returns(ci =>
            {
                BankAccountGraphQLModel src = ci.Arg<BankAccountGraphQLModel>();
                return new TreasuryBankAccountMasterTreeDTO
                {
                    Id = src.Id,
                    Number = src.Number,
                    Bank = new TreasuryBankMasterTreeDTO { Id = src.Bank?.Id ?? 0 }
                };
            });

        _mapper.Map<TreasuryFranchiseMasterTreeDTO>(Arg.Any<FranchiseGraphQLModel>())
            .Returns(ci =>
            {
                FranchiseGraphQLModel src = ci.Arg<FranchiseGraphQLModel>();
                return new TreasuryFranchiseMasterTreeDTO { Id = src.Id, Name = src.Name };
            });

        _mapper.Map<MajorCashDrawerMasterTreeDTO>(Arg.Any<CashDrawerGraphQLModel>())
            .Returns(ci =>
            {
                CashDrawerGraphQLModel src = ci.Arg<CashDrawerGraphQLModel>();
                return new MajorCashDrawerMasterTreeDTO { Id = src.Id, Name = src.Name };
            });

        _mapper.Map<MinorCashDrawerMasterTreeDTO>(Arg.Any<CashDrawerGraphQLModel>())
            .Returns(ci =>
            {
                CashDrawerGraphQLModel src = ci.Arg<CashDrawerGraphQLModel>();
                return new MinorCashDrawerMasterTreeDTO { Id = src.Id, Name = src.Name };
            });

        _mapper.Map<TreasuryAuxiliaryCashDrawerMasterTreeDTO>(Arg.Any<CashDrawerGraphQLModel>())
            .Returns(ci =>
            {
                CashDrawerGraphQLModel src = ci.Arg<CashDrawerGraphQLModel>();
                return new TreasuryAuxiliaryCashDrawerMasterTreeDTO { Id = src.Id, Name = src.Name };
            });
    }

    #region Test data builders

    private static BankGraphQLModel BuildBank(int id = 1, string name = "Bancolombia", string prefix = "BC") =>
        new()
        {
            Id = id,
            Code = id.ToString(),
            PaymentMethodPrefix = prefix,
            AccountingEntity = new AccountingEntityGraphQLModel { Id = id * 10, SearchName = name }
        };

    private static BankAccountGraphQLModel BuildBankAccount(int id, int bankId) =>
        new()
        {
            Id = id,
            Number = $"ACC-{id}",
            Bank = new BankGraphQLModel { Id = bankId }
        };

    private static FranchiseGraphQLModel BuildFranchise(int id = 1, string name = "Visa") =>
        new() { Id = id, Name = name };

    private static CashDrawerGraphQLModel BuildMajorCashDrawer(int id, int locationId, int costCenterId) =>
        new()
        {
            Id = id,
            Name = $"CajaMayor-{id}",
            IsPettyCash = false,
            Parent = null,
            CostCenter = new CostCenterGraphQLModel
            {
                Id = costCenterId,
                CompanyLocation = new CompanyLocationGraphQLModel { Id = locationId }
            }
        };

    private static CashDrawerGraphQLModel BuildMinorCashDrawer(int id, int locationId, int costCenterId) =>
        new()
        {
            Id = id,
            Name = $"CajaMenor-{id}",
            IsPettyCash = true,
            Parent = null,
            CostCenter = new CostCenterGraphQLModel
            {
                Id = costCenterId,
                CompanyLocation = new CompanyLocationGraphQLModel { Id = locationId }
            }
        };

    private static CashDrawerGraphQLModel BuildAuxiliaryCashDrawer(int id, int locationId, int costCenterId, int parentId) =>
        new()
        {
            Id = id,
            Name = $"CajaAux-{id}",
            IsPettyCash = false,
            Parent = new CashDrawerGraphQLModel
            {
                Id = parentId,
                CostCenter = new CostCenterGraphQLModel
                {
                    Id = costCenterId,
                    CompanyLocation = new CompanyLocationGraphQLModel { Id = locationId }
                }
            },
            CostCenter = new CostCenterGraphQLModel
            {
                Id = costCenterId,
                CompanyLocation = new CompanyLocationGraphQLModel { Id = locationId }
            }
        };

    /// <summary>
    /// Pre-popula los nodos del árbol para CashDrawer (Major/Minor) — inserta una location y cost center
    /// para permitir que los HandleAsync de cashDrawer encuentren el padre.
    /// </summary>
    private void SeedLocationAndCostCenterInTree(int locationId, int costCenterId, CashDrawerType type)
    {
        CashDrawerDummyDTO? dummy = _vm.DummyItems.OfType<CashDrawerDummyDTO>().FirstOrDefault(x => x.Type == type);
        if (dummy is null) return;

        CashDrawerCompanyLocationDTO locationDTO = new() { Id = locationId, Type = type, DummyParent = dummy };
        CashDrawerCostCenterDTO costCenterDTO = new() { Id = costCenterId, Type = type, Location = locationDTO };
        locationDTO.CostCenters.Add(costCenterDTO);
        dummy.Locations.Add(locationDTO);
    }

    #endregion

    #region Constructor null-guards

    [Fact]
    public void Constructor_NullContext_Throws()
    {
        System.Action act = () => new TreasuryRootMasterViewModel(
            null!, _cashDrawerService, _bankService, _bankAccountService, _franchiseService,
            _dialogService, _notificationService,
            _auxiliaryAccountingAccountCache, _companyLocationCache, _costCenterCache,
            _bankAccountCache, _majorCashDrawerCache, _minorCashDrawerCache,
            _auxiliaryCashDrawerCache, _bankCache, _franchiseCache, _graphQLClient,
            _stringLengthCache, _joinableTaskFactory,
            _bankValidator, _bankAccountValidator, _franchiseValidator,
            _majorCashDrawerValidator, _minorCashDrawerValidator, _auxiliaryCashDrawerValidator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("context");
    }

    [Fact]
    public void Constructor_NullCashDrawerService_Throws()
    {
        System.Action act = () => new TreasuryRootMasterViewModel(
            _context, null!, _bankService, _bankAccountService, _franchiseService,
            _dialogService, _notificationService,
            _auxiliaryAccountingAccountCache, _companyLocationCache, _costCenterCache,
            _bankAccountCache, _majorCashDrawerCache, _minorCashDrawerCache,
            _auxiliaryCashDrawerCache, _bankCache, _franchiseCache, _graphQLClient,
            _stringLengthCache, _joinableTaskFactory,
            _bankValidator, _bankAccountValidator, _franchiseValidator,
            _majorCashDrawerValidator, _minorCashDrawerValidator, _auxiliaryCashDrawerValidator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("cashDrawerService");
    }

    [Fact]
    public void Constructor_NullBankService_Throws()
    {
        System.Action act = () => new TreasuryRootMasterViewModel(
            _context, _cashDrawerService, null!, _bankAccountService, _franchiseService,
            _dialogService, _notificationService,
            _auxiliaryAccountingAccountCache, _companyLocationCache, _costCenterCache,
            _bankAccountCache, _majorCashDrawerCache, _minorCashDrawerCache,
            _auxiliaryCashDrawerCache, _bankCache, _franchiseCache, _graphQLClient,
            _stringLengthCache, _joinableTaskFactory,
            _bankValidator, _bankAccountValidator, _franchiseValidator,
            _majorCashDrawerValidator, _minorCashDrawerValidator, _auxiliaryCashDrawerValidator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("bankService");
    }

    [Fact]
    public void Constructor_NullBankAccountService_Throws()
    {
        System.Action act = () => new TreasuryRootMasterViewModel(
            _context, _cashDrawerService, _bankService, null!, _franchiseService,
            _dialogService, _notificationService,
            _auxiliaryAccountingAccountCache, _companyLocationCache, _costCenterCache,
            _bankAccountCache, _majorCashDrawerCache, _minorCashDrawerCache,
            _auxiliaryCashDrawerCache, _bankCache, _franchiseCache, _graphQLClient,
            _stringLengthCache, _joinableTaskFactory,
            _bankValidator, _bankAccountValidator, _franchiseValidator,
            _majorCashDrawerValidator, _minorCashDrawerValidator, _auxiliaryCashDrawerValidator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("bankAccountService");
    }

    [Fact]
    public void Constructor_NullFranchiseService_Throws()
    {
        System.Action act = () => new TreasuryRootMasterViewModel(
            _context, _cashDrawerService, _bankService, _bankAccountService, null!,
            _dialogService, _notificationService,
            _auxiliaryAccountingAccountCache, _companyLocationCache, _costCenterCache,
            _bankAccountCache, _majorCashDrawerCache, _minorCashDrawerCache,
            _auxiliaryCashDrawerCache, _bankCache, _franchiseCache, _graphQLClient,
            _stringLengthCache, _joinableTaskFactory,
            _bankValidator, _bankAccountValidator, _franchiseValidator,
            _majorCashDrawerValidator, _minorCashDrawerValidator, _auxiliaryCashDrawerValidator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("franchiseService");
    }

    [Fact]
    public void Constructor_NullDialogService_Throws()
    {
        System.Action act = () => new TreasuryRootMasterViewModel(
            _context, _cashDrawerService, _bankService, _bankAccountService, _franchiseService,
            null!, _notificationService,
            _auxiliaryAccountingAccountCache, _companyLocationCache, _costCenterCache,
            _bankAccountCache, _majorCashDrawerCache, _minorCashDrawerCache,
            _auxiliaryCashDrawerCache, _bankCache, _franchiseCache, _graphQLClient,
            _stringLengthCache, _joinableTaskFactory,
            _bankValidator, _bankAccountValidator, _franchiseValidator,
            _majorCashDrawerValidator, _minorCashDrawerValidator, _auxiliaryCashDrawerValidator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("dialogService");
    }

    [Fact]
    public void Constructor_NullNotificationService_Throws()
    {
        System.Action act = () => new TreasuryRootMasterViewModel(
            _context, _cashDrawerService, _bankService, _bankAccountService, _franchiseService,
            _dialogService, null!,
            _auxiliaryAccountingAccountCache, _companyLocationCache, _costCenterCache,
            _bankAccountCache, _majorCashDrawerCache, _minorCashDrawerCache,
            _auxiliaryCashDrawerCache, _bankCache, _franchiseCache, _graphQLClient,
            _stringLengthCache, _joinableTaskFactory,
            _bankValidator, _bankAccountValidator, _franchiseValidator,
            _majorCashDrawerValidator, _minorCashDrawerValidator, _auxiliaryCashDrawerValidator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("notificationService");
    }

    [Fact]
    public void Constructor_NullAuxiliaryAccountingAccountCache_Throws()
    {
        System.Action act = () => new TreasuryRootMasterViewModel(
            _context, _cashDrawerService, _bankService, _bankAccountService, _franchiseService,
            _dialogService, _notificationService,
            null!, _companyLocationCache, _costCenterCache,
            _bankAccountCache, _majorCashDrawerCache, _minorCashDrawerCache,
            _auxiliaryCashDrawerCache, _bankCache, _franchiseCache, _graphQLClient,
            _stringLengthCache, _joinableTaskFactory,
            _bankValidator, _bankAccountValidator, _franchiseValidator,
            _majorCashDrawerValidator, _minorCashDrawerValidator, _auxiliaryCashDrawerValidator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("auxiliaryAccountingAccountCache");
    }

    [Fact]
    public void Constructor_NullCompanyLocationCache_Throws()
    {
        System.Action act = () => new TreasuryRootMasterViewModel(
            _context, _cashDrawerService, _bankService, _bankAccountService, _franchiseService,
            _dialogService, _notificationService,
            _auxiliaryAccountingAccountCache, null!, _costCenterCache,
            _bankAccountCache, _majorCashDrawerCache, _minorCashDrawerCache,
            _auxiliaryCashDrawerCache, _bankCache, _franchiseCache, _graphQLClient,
            _stringLengthCache, _joinableTaskFactory,
            _bankValidator, _bankAccountValidator, _franchiseValidator,
            _majorCashDrawerValidator, _minorCashDrawerValidator, _auxiliaryCashDrawerValidator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("companyLocationCache");
    }

    [Fact]
    public void Constructor_NullCostCenterCache_Throws()
    {
        System.Action act = () => new TreasuryRootMasterViewModel(
            _context, _cashDrawerService, _bankService, _bankAccountService, _franchiseService,
            _dialogService, _notificationService,
            _auxiliaryAccountingAccountCache, _companyLocationCache, null!,
            _bankAccountCache, _majorCashDrawerCache, _minorCashDrawerCache,
            _auxiliaryCashDrawerCache, _bankCache, _franchiseCache, _graphQLClient,
            _stringLengthCache, _joinableTaskFactory,
            _bankValidator, _bankAccountValidator, _franchiseValidator,
            _majorCashDrawerValidator, _minorCashDrawerValidator, _auxiliaryCashDrawerValidator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("costCenterCache");
    }

    [Fact]
    public void Constructor_NullBankAccountCache_Throws()
    {
        System.Action act = () => new TreasuryRootMasterViewModel(
            _context, _cashDrawerService, _bankService, _bankAccountService, _franchiseService,
            _dialogService, _notificationService,
            _auxiliaryAccountingAccountCache, _companyLocationCache, _costCenterCache,
            null!, _majorCashDrawerCache, _minorCashDrawerCache,
            _auxiliaryCashDrawerCache, _bankCache, _franchiseCache, _graphQLClient,
            _stringLengthCache, _joinableTaskFactory,
            _bankValidator, _bankAccountValidator, _franchiseValidator,
            _majorCashDrawerValidator, _minorCashDrawerValidator, _auxiliaryCashDrawerValidator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("bankAccountCache");
    }

    [Fact]
    public void Constructor_NullMajorCashDrawerCache_Throws()
    {
        System.Action act = () => new TreasuryRootMasterViewModel(
            _context, _cashDrawerService, _bankService, _bankAccountService, _franchiseService,
            _dialogService, _notificationService,
            _auxiliaryAccountingAccountCache, _companyLocationCache, _costCenterCache,
            _bankAccountCache, null!, _minorCashDrawerCache,
            _auxiliaryCashDrawerCache, _bankCache, _franchiseCache, _graphQLClient,
            _stringLengthCache, _joinableTaskFactory,
            _bankValidator, _bankAccountValidator, _franchiseValidator,
            _majorCashDrawerValidator, _minorCashDrawerValidator, _auxiliaryCashDrawerValidator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("majorCashDrawerCache");
    }

    [Fact]
    public void Constructor_NullMinorCashDrawerCache_Throws()
    {
        System.Action act = () => new TreasuryRootMasterViewModel(
            _context, _cashDrawerService, _bankService, _bankAccountService, _franchiseService,
            _dialogService, _notificationService,
            _auxiliaryAccountingAccountCache, _companyLocationCache, _costCenterCache,
            _bankAccountCache, _majorCashDrawerCache, null!,
            _auxiliaryCashDrawerCache, _bankCache, _franchiseCache, _graphQLClient,
            _stringLengthCache, _joinableTaskFactory,
            _bankValidator, _bankAccountValidator, _franchiseValidator,
            _majorCashDrawerValidator, _minorCashDrawerValidator, _auxiliaryCashDrawerValidator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("minorCashDrawerCache");
    }

    [Fact]
    public void Constructor_NullAuxiliaryCashDrawerCache_Throws()
    {
        System.Action act = () => new TreasuryRootMasterViewModel(
            _context, _cashDrawerService, _bankService, _bankAccountService, _franchiseService,
            _dialogService, _notificationService,
            _auxiliaryAccountingAccountCache, _companyLocationCache, _costCenterCache,
            _bankAccountCache, _majorCashDrawerCache, _minorCashDrawerCache,
            null!, _bankCache, _franchiseCache, _graphQLClient,
            _stringLengthCache, _joinableTaskFactory,
            _bankValidator, _bankAccountValidator, _franchiseValidator,
            _majorCashDrawerValidator, _minorCashDrawerValidator, _auxiliaryCashDrawerValidator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("auxiliaryCashDrawerCache");
    }

    [Fact]
    public void Constructor_NullBankCache_Throws()
    {
        System.Action act = () => new TreasuryRootMasterViewModel(
            _context, _cashDrawerService, _bankService, _bankAccountService, _franchiseService,
            _dialogService, _notificationService,
            _auxiliaryAccountingAccountCache, _companyLocationCache, _costCenterCache,
            _bankAccountCache, _majorCashDrawerCache, _minorCashDrawerCache,
            _auxiliaryCashDrawerCache, null!, _franchiseCache, _graphQLClient,
            _stringLengthCache, _joinableTaskFactory,
            _bankValidator, _bankAccountValidator, _franchiseValidator,
            _majorCashDrawerValidator, _minorCashDrawerValidator, _auxiliaryCashDrawerValidator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("bankCache");
    }

    [Fact]
    public void Constructor_NullFranchiseCache_Throws()
    {
        System.Action act = () => new TreasuryRootMasterViewModel(
            _context, _cashDrawerService, _bankService, _bankAccountService, _franchiseService,
            _dialogService, _notificationService,
            _auxiliaryAccountingAccountCache, _companyLocationCache, _costCenterCache,
            _bankAccountCache, _majorCashDrawerCache, _minorCashDrawerCache,
            _auxiliaryCashDrawerCache, _bankCache, null!, _graphQLClient,
            _stringLengthCache, _joinableTaskFactory,
            _bankValidator, _bankAccountValidator, _franchiseValidator,
            _majorCashDrawerValidator, _minorCashDrawerValidator, _auxiliaryCashDrawerValidator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("franchiseCache");
    }

    [Fact]
    public void Constructor_NullGraphQLClient_Throws()
    {
        System.Action act = () => new TreasuryRootMasterViewModel(
            _context, _cashDrawerService, _bankService, _bankAccountService, _franchiseService,
            _dialogService, _notificationService,
            _auxiliaryAccountingAccountCache, _companyLocationCache, _costCenterCache,
            _bankAccountCache, _majorCashDrawerCache, _minorCashDrawerCache,
            _auxiliaryCashDrawerCache, _bankCache, _franchiseCache, null!,
            _stringLengthCache, _joinableTaskFactory,
            _bankValidator, _bankAccountValidator, _franchiseValidator,
            _majorCashDrawerValidator, _minorCashDrawerValidator, _auxiliaryCashDrawerValidator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("graphQLClient");
    }

    [Fact]
    public void Constructor_NullStringLengthCache_Throws()
    {
        System.Action act = () => new TreasuryRootMasterViewModel(
            _context, _cashDrawerService, _bankService, _bankAccountService, _franchiseService,
            _dialogService, _notificationService,
            _auxiliaryAccountingAccountCache, _companyLocationCache, _costCenterCache,
            _bankAccountCache, _majorCashDrawerCache, _minorCashDrawerCache,
            _auxiliaryCashDrawerCache, _bankCache, _franchiseCache, _graphQLClient,
            null!, _joinableTaskFactory,
            _bankValidator, _bankAccountValidator, _franchiseValidator,
            _majorCashDrawerValidator, _minorCashDrawerValidator, _auxiliaryCashDrawerValidator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("stringLengthCache");
    }

    [Fact]
    public void Constructor_NullJoinableTaskFactory_Throws()
    {
        System.Action act = () => new TreasuryRootMasterViewModel(
            _context, _cashDrawerService, _bankService, _bankAccountService, _franchiseService,
            _dialogService, _notificationService,
            _auxiliaryAccountingAccountCache, _companyLocationCache, _costCenterCache,
            _bankAccountCache, _majorCashDrawerCache, _minorCashDrawerCache,
            _auxiliaryCashDrawerCache, _bankCache, _franchiseCache, _graphQLClient,
            _stringLengthCache, null!,
            _bankValidator, _bankAccountValidator, _franchiseValidator,
            _majorCashDrawerValidator, _minorCashDrawerValidator, _auxiliaryCashDrawerValidator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("joinableTaskFactory");
    }

    [Fact]
    public void Constructor_NullBankValidator_Throws()
    {
        System.Action act = () => new TreasuryRootMasterViewModel(
            _context, _cashDrawerService, _bankService, _bankAccountService, _franchiseService,
            _dialogService, _notificationService,
            _auxiliaryAccountingAccountCache, _companyLocationCache, _costCenterCache,
            _bankAccountCache, _majorCashDrawerCache, _minorCashDrawerCache,
            _auxiliaryCashDrawerCache, _bankCache, _franchiseCache, _graphQLClient,
            _stringLengthCache, _joinableTaskFactory,
            null!, _bankAccountValidator, _franchiseValidator,
            _majorCashDrawerValidator, _minorCashDrawerValidator, _auxiliaryCashDrawerValidator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("bankValidator");
    }

    [Fact]
    public void Constructor_NullBankAccountValidator_Throws()
    {
        System.Action act = () => new TreasuryRootMasterViewModel(
            _context, _cashDrawerService, _bankService, _bankAccountService, _franchiseService,
            _dialogService, _notificationService,
            _auxiliaryAccountingAccountCache, _companyLocationCache, _costCenterCache,
            _bankAccountCache, _majorCashDrawerCache, _minorCashDrawerCache,
            _auxiliaryCashDrawerCache, _bankCache, _franchiseCache, _graphQLClient,
            _stringLengthCache, _joinableTaskFactory,
            _bankValidator, null!, _franchiseValidator,
            _majorCashDrawerValidator, _minorCashDrawerValidator, _auxiliaryCashDrawerValidator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("bankAccountValidator");
    }

    [Fact]
    public void Constructor_NullFranchiseValidator_Throws()
    {
        System.Action act = () => new TreasuryRootMasterViewModel(
            _context, _cashDrawerService, _bankService, _bankAccountService, _franchiseService,
            _dialogService, _notificationService,
            _auxiliaryAccountingAccountCache, _companyLocationCache, _costCenterCache,
            _bankAccountCache, _majorCashDrawerCache, _minorCashDrawerCache,
            _auxiliaryCashDrawerCache, _bankCache, _franchiseCache, _graphQLClient,
            _stringLengthCache, _joinableTaskFactory,
            _bankValidator, _bankAccountValidator, null!,
            _majorCashDrawerValidator, _minorCashDrawerValidator, _auxiliaryCashDrawerValidator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("franchiseValidator");
    }

    [Fact]
    public void Constructor_NullMajorCashDrawerValidator_Throws()
    {
        System.Action act = () => new TreasuryRootMasterViewModel(
            _context, _cashDrawerService, _bankService, _bankAccountService, _franchiseService,
            _dialogService, _notificationService,
            _auxiliaryAccountingAccountCache, _companyLocationCache, _costCenterCache,
            _bankAccountCache, _majorCashDrawerCache, _minorCashDrawerCache,
            _auxiliaryCashDrawerCache, _bankCache, _franchiseCache, _graphQLClient,
            _stringLengthCache, _joinableTaskFactory,
            _bankValidator, _bankAccountValidator, _franchiseValidator,
            null!, _minorCashDrawerValidator, _auxiliaryCashDrawerValidator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("majorCashDrawerValidator");
    }

    [Fact]
    public void Constructor_NullMinorCashDrawerValidator_Throws()
    {
        System.Action act = () => new TreasuryRootMasterViewModel(
            _context, _cashDrawerService, _bankService, _bankAccountService, _franchiseService,
            _dialogService, _notificationService,
            _auxiliaryAccountingAccountCache, _companyLocationCache, _costCenterCache,
            _bankAccountCache, _majorCashDrawerCache, _minorCashDrawerCache,
            _auxiliaryCashDrawerCache, _bankCache, _franchiseCache, _graphQLClient,
            _stringLengthCache, _joinableTaskFactory,
            _bankValidator, _bankAccountValidator, _franchiseValidator,
            _majorCashDrawerValidator, null!, _auxiliaryCashDrawerValidator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("minorCashDrawerValidator");
    }

    [Fact]
    public void Constructor_NullAuxiliaryCashDrawerValidator_Throws()
    {
        System.Action act = () => new TreasuryRootMasterViewModel(
            _context, _cashDrawerService, _bankService, _bankAccountService, _franchiseService,
            _dialogService, _notificationService,
            _auxiliaryAccountingAccountCache, _companyLocationCache, _costCenterCache,
            _bankAccountCache, _majorCashDrawerCache, _minorCashDrawerCache,
            _auxiliaryCashDrawerCache, _bankCache, _franchiseCache, _graphQLClient,
            _stringLengthCache, _joinableTaskFactory,
            _bankValidator, _bankAccountValidator, _franchiseValidator,
            _majorCashDrawerValidator, _minorCashDrawerValidator, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("auxiliaryCashDrawerValidator");
    }

    #endregion

    #region BankCreateMessage / BankUpdateMessage / BankDeleteMessage

    [Fact]
    public async Task HandleAsync_BankCreateMessage_InsertsBankIntoTree_AndSelectsIt()
    {
        BankGraphQLModel bank = BuildBank(id: 5, name: "BBVA", prefix: "BB");
        BankCreateMessage message = new()
        {
            CreatedBank = new UpsertResponseType<BankGraphQLModel> { Success = true, Entity = bank, Message = "Banco creado" }
        };

        await _vm.HandleAsync(message, CancellationToken.None);

        BankDummyDTO bankDummy = _vm.DummyItems.OfType<BankDummyDTO>().Single();
        bankDummy.Banks.Should().ContainSingle(b => b.Id == 5);
        _vm.SelectedItem.Should().BeOfType<TreasuryBankMasterTreeDTO>().Which.Id.Should().Be(5);
        _notificationService.Received(1).ShowSuccess("Banco creado");
    }

    [Fact]
    public async Task HandleAsync_BankUpdateMessage_UpdatesExistingBank()
    {
        // Arrange: insert initial bank then publish an update
        BankGraphQLModel originalBank = BuildBank(id: 5, name: "BBVA", prefix: "BB");
        await _vm.HandleAsync(
            new BankCreateMessage { CreatedBank = new UpsertResponseType<BankGraphQLModel> { Success = true, Entity = originalBank, Message = "OK" } },
            CancellationToken.None);

        BankGraphQLModel otherBank = BuildBank(id: 6, name: "Davivienda", prefix: "DA");
        await _vm.HandleAsync(
            new BankCreateMessage { CreatedBank = new UpsertResponseType<BankGraphQLModel> { Success = true, Entity = otherBank, Message = "OK" } },
            CancellationToken.None);

        BankGraphQLModel updatedBank = BuildBank(id: 5, name: "BBVA Colombia", prefix: "BX");

        // Act
        await _vm.HandleAsync(
            new BankUpdateMessage { UpdatedBank = new UpsertResponseType<BankGraphQLModel> { Success = true, Entity = updatedBank, Message = "Banco actualizado" } },
            CancellationToken.None);

        // Assert: bank 5 updated, bank 6 untouched
        BankDummyDTO bankDummy = _vm.DummyItems.OfType<BankDummyDTO>().Single();
        TreasuryBankMasterTreeDTO bank5 = bankDummy.Banks.Single(b => b.Id == 5);
        bank5.PaymentMethodPrefix.Should().Be("BX");
        bank5.AccountingEntity.SearchName.Should().Be("BBVA Colombia");
        TreasuryBankMasterTreeDTO bank6 = bankDummy.Banks.Single(b => b.Id == 6);
        bank6.AccountingEntity.SearchName.Should().Be("Davivienda");
        _notificationService.Received(1).ShowSuccess("Banco actualizado");
    }

    [Fact]
    public async Task HandleAsync_BankDeleteMessage_RemovesBankFromTree()
    {
        // Arrange: insert two banks
        await _vm.HandleAsync(
            new BankCreateMessage { CreatedBank = new UpsertResponseType<BankGraphQLModel> { Success = true, Entity = BuildBank(5), Message = "OK" } },
            CancellationToken.None);
        await _vm.HandleAsync(
            new BankCreateMessage { CreatedBank = new UpsertResponseType<BankGraphQLModel> { Success = true, Entity = BuildBank(6), Message = "OK" } },
            CancellationToken.None);

        // Act
        await _vm.HandleAsync(
            new BankDeleteMessage { DeletedBank = new DeleteResponseType { Success = true, DeletedId = 5, Message = "Banco eliminado" } },
            CancellationToken.None);

        // Assert
        BankDummyDTO bankDummy = _vm.DummyItems.OfType<BankDummyDTO>().Single();
        bankDummy.Banks.Should().ContainSingle().Which.Id.Should().Be(6);
        _notificationService.Received(1).ShowSuccess("Banco eliminado");
    }

    #endregion

    #region FranchiseCreateMessage

    [Fact]
    public async Task HandleAsync_FranchiseCreateMessage_InsertsFranchiseIntoTree_AndSelectsIt()
    {
        FranchiseGraphQLModel franchise = BuildFranchise(id: 11, name: "MasterCard");
        FranchiseCreateMessage message = new()
        {
            CreatedFranchise = new UpsertResponseType<FranchiseGraphQLModel> { Success = true, Entity = franchise, Message = "Franquicia creada" }
        };

        await _vm.HandleAsync(message, CancellationToken.None);

        FranchiseDummyDTO franchiseDummy = _vm.DummyItems.OfType<FranchiseDummyDTO>().Single();
        franchiseDummy.Franchises.Should().ContainSingle(f => f.Id == 11);
        _vm.SelectedItem.Should().BeOfType<TreasuryFranchiseMasterTreeDTO>().Which.Id.Should().Be(11);
        _notificationService.Received(1).ShowSuccess("Franquicia creada");
    }

    #endregion

    #region TreasuryCashDrawerCreateMessage — 3 branches

    [Fact]
    public async Task HandleAsync_MajorCashDrawerCreateMessage_InsertsUnderCorrectLocationAndCostCenter()
    {
        // Arrange: pre-populate tree with location + cost center for Major
        SeedLocationAndCostCenterInTree(locationId: 100, costCenterId: 200, CashDrawerType.Major);
        CashDrawerGraphQLModel major = BuildMajorCashDrawer(id: 50, locationId: 100, costCenterId: 200);

        // Act
        await _vm.HandleAsync(
            new TreasuryCashDrawerCreateMessage { CreatedCashDrawer = new UpsertResponseType<CashDrawerGraphQLModel> { Success = true, Entity = major, Message = "Caja general creada" } },
            CancellationToken.None);

        // Assert
        CashDrawerDummyDTO majorDummy = _vm.DummyItems.OfType<CashDrawerDummyDTO>().Single(x => x.Type == CashDrawerType.Major);
        CashDrawerCompanyLocationDTO location = majorDummy.Locations.Single(l => l.Id == 100);
        CashDrawerCostCenterDTO costCenter = location.CostCenters.Single(c => c.Id == 200);
        costCenter.CashDrawers.Should().ContainSingle()
            .Which.Should().BeOfType<MajorCashDrawerMasterTreeDTO>()
            .Which.Id.Should().Be(50);
        _vm.SelectedItem.Should().BeOfType<MajorCashDrawerMasterTreeDTO>().Which.Id.Should().Be(50);
        _notificationService.Received(1).ShowSuccess("Caja general creada");
    }

    [Fact]
    public async Task HandleAsync_MinorCashDrawerCreateMessage_InsertsUnderMinorLocation()
    {
        SeedLocationAndCostCenterInTree(locationId: 110, costCenterId: 210, CashDrawerType.Minor);
        CashDrawerGraphQLModel minor = BuildMinorCashDrawer(id: 60, locationId: 110, costCenterId: 210);

        await _vm.HandleAsync(
            new TreasuryCashDrawerCreateMessage { CreatedCashDrawer = new UpsertResponseType<CashDrawerGraphQLModel> { Success = true, Entity = minor, Message = "Caja menor creada" } },
            CancellationToken.None);

        CashDrawerDummyDTO minorDummy = _vm.DummyItems.OfType<CashDrawerDummyDTO>().Single(x => x.Type == CashDrawerType.Minor);
        CashDrawerCompanyLocationDTO location = minorDummy.Locations.Single(l => l.Id == 110);
        CashDrawerCostCenterDTO costCenter = location.CostCenters.Single(c => c.Id == 210);
        costCenter.CashDrawers.Should().ContainSingle()
            .Which.Should().BeOfType<MinorCashDrawerMasterTreeDTO>()
            .Which.Id.Should().Be(60);
        _vm.SelectedItem.Should().BeOfType<MinorCashDrawerMasterTreeDTO>().Which.Id.Should().Be(60);
        _notificationService.Received(1).ShowSuccess("Caja menor creada");
    }

    [Fact]
    public async Task HandleAsync_AuxiliaryCashDrawerCreateMessage_InsertsUnderParentMajor()
    {
        // Arrange: seed location + cost center + create parent major first via HandleAsync
        SeedLocationAndCostCenterInTree(locationId: 120, costCenterId: 220, CashDrawerType.Major);
        CashDrawerGraphQLModel major = BuildMajorCashDrawer(id: 70, locationId: 120, costCenterId: 220);
        await _vm.HandleAsync(
            new TreasuryCashDrawerCreateMessage { CreatedCashDrawer = new UpsertResponseType<CashDrawerGraphQLModel> { Success = true, Entity = major, Message = "OK" } },
            CancellationToken.None);

        CashDrawerGraphQLModel aux = BuildAuxiliaryCashDrawer(id: 71, locationId: 120, costCenterId: 220, parentId: 70);

        // Act
        await _vm.HandleAsync(
            new TreasuryCashDrawerCreateMessage { CreatedCashDrawer = new UpsertResponseType<CashDrawerGraphQLModel> { Success = true, Entity = aux, Message = "Caja aux creada" } },
            CancellationToken.None);

        // Assert: aux inserted under major 70
        CashDrawerDummyDTO majorDummy = _vm.DummyItems.OfType<CashDrawerDummyDTO>().Single(x => x.Type == CashDrawerType.Major);
        CashDrawerCostCenterDTO cc = majorDummy.Locations.Single(l => l.Id == 120).CostCenters.Single(c => c.Id == 220);
        MajorCashDrawerMasterTreeDTO parent = cc.CashDrawers.OfType<MajorCashDrawerMasterTreeDTO>().Single(x => x.Id == 70);
        parent.AuxiliaryCashDrawers.Should().ContainSingle().Which.Id.Should().Be(71);
        _vm.SelectedItem.Should().BeOfType<TreasuryAuxiliaryCashDrawerMasterTreeDTO>().Which.Id.Should().Be(71);
    }

    #endregion

    #region BankAccountCreateMessage

    [Fact]
    public async Task HandleAsync_BankAccountCreateMessage_InsertsUnderBank_AndClearsAuxiliaryAccountingCache()
    {
        // Arrange: insert bank into tree via HandleAsync; pre-populate auxiliary accounting cache
        BankGraphQLModel bank = BuildBank(id: 8, name: "Pichincha");
        await _vm.HandleAsync(
            new BankCreateMessage { CreatedBank = new UpsertResponseType<BankGraphQLModel> { Success = true, Entity = bank, Message = "OK" } },
            CancellationToken.None);

        _auxiliaryAccountingAccountCache.Add(new AccountingAccountGraphQLModel { Id = 900, Code = "1110", Name = "Caja" });
        _auxiliaryAccountingAccountCache.Items.Should().HaveCount(1);

        BankAccountGraphQLModel bankAccount = BuildBankAccount(id: 55, bankId: 8);

        // Act
        await _vm.HandleAsync(
            new BankAccountCreateMessage { CreatedBankAccount = new UpsertResponseType<BankAccountGraphQLModel> { Success = true, Entity = bankAccount, Message = "Cuenta creada" } },
            CancellationToken.None);

        // Assert: inserted under bank 8; auxiliary cache cleared (side-effect from PR 8)
        BankDummyDTO bankDummy = _vm.DummyItems.OfType<BankDummyDTO>().Single();
        TreasuryBankMasterTreeDTO bankDTO = bankDummy.Banks.Single(b => b.Id == 8);
        bankDTO.BankAccounts.Should().ContainSingle().Which.Id.Should().Be(55);
        _auxiliaryAccountingAccountCache.Items.Should().BeEmpty();
        _vm.SelectedItem.Should().BeOfType<TreasuryBankAccountMasterTreeDTO>().Which.Id.Should().Be(55);
        _notificationService.Received(1).ShowSuccess("Cuenta creada");
    }

    #endregion

    /// <summary>
    /// Ejecuta una función async en el hilo del dispatcher de WPF. Los métodos
    /// <c>CreateXxxAsync</c>/<c>EditAsync</c> del master usan <c>Dispatcher.Yield</c>
    /// que requiere un message pump activo — el STA thread del fixture lo provee.
    /// </summary>
    private static async Task RunOnDispatcherAsync(Func<Task> action)
    {
        await Application.Current.Dispatcher.InvokeAsync(action).Task.Unwrap();
    }

    #region CreateBankAsync — no SelectedItem required

    [Fact]
    public async Task CreateBankAsync_OpensDialog_Once_WithBankDetailViewModel()
    {
        _vm.SelectedItem = null;

        await RunOnDispatcherAsync(_vm.CreateBankAsync);

        await _dialogService.Received(1).ShowDialogAsync(
            Arg.Any<BankDetailViewModel>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region CreateBankAccountAsync — wrong selection vs valid

    [Fact]
    public async Task CreateBankAccountAsync_WithNullSelectedItem_ShowsInfo_DoesNotOpenDialog()
    {
        _vm.SelectedItem = null;

        await RunOnDispatcherAsync(_vm.CreateBankAccountAsync);

        _notificationService.Received(1).ShowInfo(Arg.Any<string>());
        await _dialogService.DidNotReceive().ShowDialogAsync(
            Arg.Any<Screen>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateBankAccountAsync_WithSelectedBankInCache_OpensDialog()
    {
        // Arrange: populate BankCache and select a matching tree node
        BankGraphQLModel bank = BuildBank(id: 42, name: "Occidente");
        _bankCache.Add(bank);
        _vm.SelectedItem = new TreasuryBankMasterTreeDTO { Id = 42 };

        // Act
        await RunOnDispatcherAsync(_vm.CreateBankAccountAsync);

        // Assert: dialog opened, no info notification
        await _dialogService.Received(1).ShowDialogAsync(
            Arg.Any<BankAccountDetailViewModel>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        _notificationService.DidNotReceive().ShowInfo(Arg.Any<string>());
    }

    #endregion

    #region EditAsync — polymorphic dispatch

    [Fact]
    public async Task EditAsync_WithNullSelectedItem_Returns_NoDialog()
    {
        _vm.SelectedItem = null;

        await RunOnDispatcherAsync(_vm.EditAsync);

        await _dialogService.DidNotReceive().ShowDialogAsync(
            Arg.Any<Screen>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EditAsync_WithSelectedBank_OpensDialog_WithBankDetailViewModel()
    {
        BankGraphQLModel bank = BuildBank(id: 77);
        _bankCache.Add(bank);
        _vm.SelectedItem = new TreasuryBankMasterTreeDTO { Id = 77 };

        await RunOnDispatcherAsync(_vm.EditAsync);

        await _dialogService.Received(1).ShowDialogAsync(
            Arg.Any<BankDetailViewModel>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EditAsync_WithSelectedFranchise_OpensDialog_WithFranchiseDetailViewModel()
    {
        FranchiseGraphQLModel franchise = BuildFranchise(id: 88);
        _franchiseCache.Add(franchise);
        _vm.SelectedItem = new TreasuryFranchiseMasterTreeDTO { Id = 88 };

        await RunOnDispatcherAsync(_vm.EditAsync);

        await _dialogService.Received(1).ShowDialogAsync(
            Arg.Any<FranchiseDetailViewModel>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region OnDeactivateAsync cleanup semantics

    /// <summary>
    /// Llama directamente al <c>OnDeactivateAsync</c> protegido via reflection —
    /// <c>DeactivateAsync</c> en la interfaz IDeactivate solo dispara el hook si
    /// la Screen está activa, y activarla ejerciría <c>OnActivatedAsync</c> con
    /// carga de caches que está fuera del alcance de este test.
    /// </summary>
    private static Task InvokeOnDeactivateAsync(TreasuryRootMasterViewModel vm, bool close)
    {
        System.Reflection.MethodInfo method = typeof(TreasuryRootMasterViewModel)
            .GetMethod("OnDeactivateAsync",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        return (Task)method.Invoke(vm, new object[] { close, CancellationToken.None })!;
    }

    [Fact]
    public async Task OnDeactivateAsync_WithCloseTrue_UnsubscribesFromEventAggregator()
    {
        _eventAggregator.ClearReceivedCalls();

        await InvokeOnDeactivateAsync(_vm, close: true);

        _eventAggregator.Received(1).Unsubscribe(_vm);
    }

    [Fact]
    public async Task OnDeactivateAsync_WithCloseFalse_DoesNotUnsubscribe()
    {
        _eventAggregator.ClearReceivedCalls();

        await InvokeOnDeactivateAsync(_vm, close: false);

        _eventAggregator.DidNotReceive().Unsubscribe(_vm);
    }

    #endregion
}
