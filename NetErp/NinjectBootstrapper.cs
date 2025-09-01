using AutoMapper;
using Caliburn.Micro;
using Common.Interfaces;
using Common.Validators;
using DevExpress.Data.Utils;
using DevExpress.Entity.Model.Metadata;
using DevExpress.Xpf.Bars.Native;
using DevExpress.Xpf.Editors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using Models.Billing;
using Models.Books;
using Models.Global;
using Models.Inventory;
using Models.Suppliers;
using Models.Treasury;
using NetErp.Billing.PriceList.DTO;
using NetErp.Billing.PriceList.PriceListHelpers;
using NetErp.Books.AccountingAccountGroups.DTO;
using NetErp.Books.AccountingAccounts.DTO;
using NetErp.Books.AccountingEntities.ViewModels;
using NetErp.Books.AccountingEntries.DTO;
using NetErp.Books.IdentificationTypes.DTO;
using NetErp.Global.CostCenters.DTO;
using NetErp.Global.DynamicControl;
using NetErp.Global.MainMenu.ViewModels;
using NetErp.Global.Parameter.ViewModels;
using NetErp.Global.Shell.ViewModels;
using NetErp.Helpers;
using NetErp.Helpers.Services;
using NetErp.Inventory.CatalogItems.DTO;
using NetErp.Inventory.CatalogItems.ViewModels;
using NetErp.Inventory.ItemSizes.DTO;
using NetErp.Treasury.Masters.DTO;
using Ninject;
using Services.Billing.DAL.PostgreSQL;
using Services.Books.DAL.PostgreSQL;
using Services.Global.DAL.PostgreSQL;
using Services.Inventory.DAL.PostgreSQL;
using Services.Validators;
using Services.Suppliers.DAL.PostgreSQL;
using Services.Treasury.DAL.PostgreSQL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using static NetErp.Billing.CreditLimit.ViewModels.CreditLimitMasterViewModel;
using Models.DTO.Billing;
using NetErp.Billing.Zones.DTO;
using Common.Services;

namespace NetErp
{
    class NinjectBootstrapper : BootstrapperBase
        {
            private readonly IKernel kernel = new StandardKernel();

            public NinjectBootstrapper()
            {
                // Convenisiones Caliburn.Micro
                //_ = ConventionManager.AddElementConvention<IntegerUpDown>(IntegerUpDown.ValueProperty, "Value", "DataContextChanged");
                _ = ConventionManager.AddElementConvention<SpinEdit>(SpinEdit.EditValueProperty, "Value", "DataContextChanged");
                _ = ConventionManager.AddElementConvention<TextEdit>(TextEdit.EditValueProperty, "Value", "DataContextChanged");
                _ = ConventionManager.AddElementConvention<MenuItem>(ItemsControl.ItemsSourceProperty, "DataContext", "Click");

                Initialize();
            }

            protected override void OnStartup(object sender, StartupEventArgs e)
            {
                _ = DisplayRootViewForAsync<ShellViewModel>();
            }

        protected override void Configure()
        {
            // Common
            //_ = kernel.Bind(typeof(IGenericDataAccess<>)).To(typeof(GenericDataAccess<>)).InSingletonScope();

            //kernel = new StandardKernel();
            _ = kernel.Bind<IWindowManager>().To<WindowManager>().InSingletonScope();
            _ = kernel.Bind<IEventAggregator>().To<EventAggregator>().InSingletonScope();
            _ = kernel.Bind<IGenericDataAccess<AccountingAccountGraphQLModel>>().To<AccountingAccountService>().InSingletonScope();
            _ = kernel.Bind<IGenericDataAccess<AccountingEntityGraphQLModel>>().To<AccountingEntityService>().InSingletonScope();
            _ = kernel.Bind<IGenericDataAccess<AccountingPresentationGraphQLModel>>().To<AccountingPresentationService>().InSingletonScope();
            _ = kernel.Bind<IGenericDataAccess<IdentificationTypeGraphQLModel>>().To<IdentificationTypeService>().InSingletonScope();
            _ = kernel.Bind<IGenericDataAccess<CountryGraphQLModel>>().To<CountryService>().InSingletonScope();
            _ = kernel.Bind<IGenericDataAccess<SupplierGraphQLModel>>().To<SupplierService>().InSingletonScope();
            _ = kernel.Bind<IGenericDataAccess<SellerGraphQLModel>>().To<SellerService>().InSingletonScope();
            _ = kernel.Bind<IGenericDataAccess<AccountingSourceGraphQLModel>>().To<AccountingSourceService>().InSingletonScope();
            _ = kernel.Bind<IGenericDataAccess<ProcessTypeGraphQLModel>>().To<ProcessTypeService>().InSingletonScope();
            _ = kernel.Bind<IGenericDataAccess<AuxiliaryBookGraphQLModel>>().To<AuxiliaryBookService>().InSingletonScope();
            _ = kernel.Bind<IGenericDataAccess<TestBalanceGraphQLModel>>().To<TestBalanceService>().InSingletonScope();
            _ = kernel.Bind<IGenericDataAccess<DailyBookByEntityGraphQLModel>>().To<DailyBookByEntityService>().InSingletonScope();
            _ = kernel.Bind<IGenericDataAccess<EntityVsAccountGraphQLModel>>().To<EntityVsAccountService>().InSingletonScope();
            _ = kernel.Bind<IGenericDataAccess<TestBalanceByEntityGraphQLModel>>().To<TestBalanceByEntityService>().InSingletonScope();
            _ = kernel.Bind<IGenericDataAccess<AnnualIncomeStatementGraphQLModel>>().To<AnnualIncomeStatementService>().InSingletonScope();
            _ = kernel.Bind<IGenericDataAccess<AccountingEntryMasterGraphQLModel>>().To<AccountingEntryMasterService>().InSingletonScope();
            _ = kernel.Bind<IGenericDataAccess<AccountingEntryDetailGraphQLModel>>().To<AccountingEntryDetailService>().InSingletonScope();
            _ = kernel.Bind<IGenericDataAccess<AccountingEntryDraftMasterGraphQLModel>>().To<AccountingEntryDraftMasterService>().InSingletonScope();
            _ = kernel.Bind<IGenericDataAccess<AccountingEntryDraftDetailGraphQLModel>>().To<AccountingEntryDraftDetailService>().InSingletonScope();
            _ = kernel.Bind<IGenericDataAccess<MeasurementUnitGraphQLModel>>().To<MeasurementUnitService>().InSingletonScope();
            _ = kernel.Bind<IGenericDataAccess<ItemSizeDetailGraphQLModel>>().To<ItemSizeDetailService>().InSingletonScope();
            _ = kernel.Bind<IGenericDataAccess<ItemSizeMasterGraphQLModel>>().To<ItemSizeMasterService>().InSingletonScope();
            _ = kernel.Bind<IGenericDataAccess<CatalogGraphQLModel>>().To<CatalogService>().InSingletonScope();
            _ = kernel.Bind<IGenericDataAccess<ItemTypeGraphQLModel>>().To<ItemTypeService>().InSingletonScope();
            _ = kernel.Bind<IGenericDataAccess<ItemCategoryGraphQLModel>>().To<ItemCategoryService>().InSingletonScope();
            _ = kernel.Bind<IGenericDataAccess<ItemSubCategoryGraphQLModel>>().To<ItemSubCategoryService>().InSingletonScope();
            _ = kernel.Bind<IGenericDataAccess<ItemGraphQLModel>>().To<ItemService>().InSingletonScope();
            _ = kernel.Bind<IGenericDataAccess<EanCodeGraphQLModel>>().To<EanCodeService>().InSingletonScope();
            _ = kernel.Bind<IGenericDataAccess<AwsS3ConfigGraphQLModel>>().To<AwsS3ConfigService>().InSingletonScope();
            _ = kernel.Bind<IGenericDataAccess<CompanyGraphQLModel>>().To<CompanyService>().InSingletonScope();
            _ = kernel.Bind<IGenericDataAccess<CompanyLocationGraphQLModel>>().To<CompanyLocationService>().InSingletonScope();
            _ = kernel.Bind<IGenericDataAccess<CostCenterGraphQLModel>>().To<CostCenterService>().InSingletonScope();
            _ = kernel.Bind<IGenericDataAccess<StorageGraphQLModel>>().To<StorageService>().InSingletonScope();
            _ = kernel.Bind<IGenericDataAccess<CashDrawerGraphQLModel>>().To<CashDrawerService>().InSingletonScope();
            _ = kernel.Bind<IGenericDataAccess<BankGraphQLModel>>().To<BankService>().InSingletonScope();
            _ = kernel.Bind<IGenericDataAccess<BankAccountGraphQLModel>>().To<BankAccountService>().InSingletonScope();
            _ = kernel.Bind<IGenericDataAccess<FranchiseGraphQLModel>>().To<FranchiseService>().InSingletonScope();
            _ = kernel.Bind<IGenericDataAccess<SmtpGraphQLModel>>().To<SmtpService>().InSingletonScope();
            _ = kernel.Bind<IGenericDataAccess<ZoneGraphQLModel>>().To<ZoneService>().InSingletonScope();
            _ = kernel.Bind<IGenericDataAccess<EmailGraphQLModel>>().To<EmailService>().InSingletonScope();
            _ = kernel.Bind<IGenericDataAccess<AccountingBookGraphQLModel>>().To<AccountingBookService>().InSingletonScope();
            _ = kernel.Bind<IGenericDataAccess<ConceptGraphQLModel>>().To<ConceptService>().InSingletonScope();
            _ = kernel.Bind<IGenericDataAccess<PriceListGraphQLModel>>().To<PriceListService>().InSingletonScope();
            _ = kernel.Bind<IGenericDataAccess<PriceListDetailGraphQLModel>>().To<PriceListDetailService>().InSingletonScope();
            _ = kernel.Bind<IGenericDataAccess<AuthorizationSequenceGraphQLModel>>().To<AuthorizationSequenceService>().InSingletonScope();
            _ = kernel.Bind<IGenericDataAccess<AuthorizationSequenceTypeGraphQLModel>>().To<AuthorizationSequenceTypeService>().InSingletonScope();
          
            _ = kernel.Bind<IGenericDataAccess<ParameterGraphQLModel>>().To<ParameterService>().InSingletonScope();
            
            // New GraphQL Infrastructure
            // Nueva estructura de servicios e inyección de dependencias
            // IRepository reemplaza IGenericDataAccess
            _ = kernel.Bind<IGraphQLClient>().To<GraphQLClient>().InSingletonScope();

            _ = kernel.Bind<IRepository<MeasurementUnitGraphQLModel>>().To<GraphQLRepository<MeasurementUnitGraphQLModel>>().InSingletonScope();
            _ = kernel.Bind<IRepository<CreditLimitGraphQLModel>>().To<GraphQLRepository<CreditLimitGraphQLModel>>().InSingletonScope();
            _ = kernel.Bind<IRepository<CustomerGraphQLModel>>().To<GraphQLRepository<CustomerGraphQLModel>>().InSingletonScope();
            _ = kernel.Bind<IRepository<PriceListDetailGraphQLModel>>().To<GraphQLRepository<PriceListDetailGraphQLModel>>().InSingletonScope();
            _ = kernel.Bind<IRepository<PriceListGraphQLModel>>().To<GraphQLRepository<PriceListGraphQLModel>>().InSingletonScope();
            _ = kernel.Bind<IRepository<StorageGraphQLModel>>().To<GraphQLRepository<StorageGraphQLModel>>().InSingletonScope();
            _ = kernel.Bind<IRepository<ItemGraphQLModel>>().To<GraphQLRepository<ItemGraphQLModel>>().InSingletonScope();
            _ = kernel.Bind<IRepository<TempRecordGraphQLModel>>().To<GraphQLRepository<TempRecordGraphQLModel>>().InSingletonScope();
            
            // Inventory module repositories
            _ = kernel.Bind<IRepository<CatalogGraphQLModel>>().To<GraphQLRepository<CatalogGraphQLModel>>().InSingletonScope();
            _ = kernel.Bind<IRepository<ItemTypeGraphQLModel>>().To<GraphQLRepository<ItemTypeGraphQLModel>>().InSingletonScope();
            _ = kernel.Bind<IRepository<ItemCategoryGraphQLModel>>().To<GraphQLRepository<ItemCategoryGraphQLModel>>().InSingletonScope();
            _ = kernel.Bind<IRepository<ItemSubCategoryGraphQLModel>>().To<GraphQLRepository<ItemSubCategoryGraphQLModel>>().InSingletonScope();
            _ = kernel.Bind<IRepository<ItemSizeMasterGraphQLModel>>().To<GraphQLRepository<ItemSizeMasterGraphQLModel>>().InSingletonScope();
            _ = kernel.Bind<IRepository<ItemSizeDetailGraphQLModel>>().To<GraphQLRepository<ItemSizeDetailGraphQLModel>>().InSingletonScope();
            _ = kernel.Bind<IRepository<AwsS3ConfigGraphQLModel>>().To<GraphQLRepository<AwsS3ConfigGraphQLModel>>().InSingletonScope();

            // Books
            _ = kernel.Bind<IRepository<TaxGraphQLModel>>().To<GraphQLRepository<TaxGraphQLModel>>().InSingletonScope();
            _ = kernel.Bind<IRepository<TaxTypeGraphQLModel>>().To<GraphQLRepository<TaxTypeGraphQLModel>>().InSingletonScope();
            _ = kernel.Bind<IRepository<WithholdingCertificateConfigGraphQLModel>>().To<GraphQLRepository<WithholdingCertificateConfigGraphQLModel>>().InSingletonScope();
            _ = kernel.Bind<IRepository<AccountingAccountGroupGraphQLModel>>().To<GraphQLRepository<AccountingAccountGroupGraphQLModel>>().InSingletonScope();


            // Global
            _ = kernel.Bind<IRepository<AuthorizationSequenceGraphQLModel>>().To<GraphQLRepository<AuthorizationSequenceGraphQLModel>>().InSingletonScope();
            
            // Treasury module repositories
            _ = kernel.Bind<IRepository<CompanyLocationGraphQLModel>>().To<GraphQLRepository<CompanyLocationGraphQLModel>>().InSingletonScope();
            _ = kernel.Bind<IRepository<CostCenterGraphQLModel>>().To<GraphQLRepository<CostCenterGraphQLModel>>().InSingletonScope();
            _ = kernel.Bind<IRepository<ConceptGraphQLModel>>().To<GraphQLRepository<ConceptGraphQLModel>>().InSingletonScope();
            
            // Global module repositories
            _ = kernel.Bind<IRepository<CompanyGraphQLModel>>().To<GraphQLRepository<CompanyGraphQLModel>>().InSingletonScope();
            _ = kernel.Bind<IRepository<CountryGraphQLModel>>().To<GraphQLRepository<CountryGraphQLModel>>().InSingletonScope();
            _ = kernel.Bind<IRepository<CashDrawerGraphQLModel>>().To<GraphQLRepository<CashDrawerGraphQLModel>>().InSingletonScope();
            _ = kernel.Bind<IRepository<BankGraphQLModel>>().To<GraphQLRepository<BankGraphQLModel>>().InSingletonScope();
            _ = kernel.Bind<IRepository<BankAccountGraphQLModel>>().To<GraphQLRepository<BankAccountGraphQLModel>>().InSingletonScope();
            _ = kernel.Bind<IRepository<FranchiseGraphQLModel>>().To<GraphQLRepository<FranchiseGraphQLModel>>().InSingletonScope();
            
            // Billing module repositories
            _ = kernel.Bind<IRepository<SellerGraphQLModel>>().To<GraphQLRepository<SellerGraphQLModel>>().InSingletonScope();
            _ = kernel.Bind<IRepository<ZoneGraphQLModel>>().To<GraphQLRepository<ZoneGraphQLModel>>().InSingletonScope();
            
            // Suppliers module repositories
            _ = kernel.Bind<IRepository<SupplierGraphQLModel>>().To<GraphQLRepository<SupplierGraphQLModel>>().InSingletonScope();
            
            // Email module repositories
            _ = kernel.Bind<IRepository<EmailGraphQLModel>>().To<GraphQLRepository<EmailGraphQLModel>>().InSingletonScope();
            _ = kernel.Bind<IRepository<SmtpGraphQLModel>>().To<GraphQLRepository<SmtpGraphQLModel>>().InSingletonScope();
            
            // Login service
            _ = kernel.Bind<ILoginService>().To<LoginService>().InSingletonScope();
            //Servicio SQLite usado para almacenar los correos electrónicos guardados localmente para autocompletar en el inicio de sesión
            _ = kernel.Bind<ISQLiteEmailStorageService>().To<SQLiteEmailStorageService>().InSingletonScope();
            
            _ = kernel.Bind<IBackgroundQueueService>().To<BackgroundQueueService>().InSingletonScope();
            _ = kernel.Bind<INetworkConnectivityService>().To<NetworkConnectivityService>().InSingletonScope();
            _ = kernel.Bind<INotificationService>().To<NotificationService>().InSingletonScope();
            _ = kernel.Bind<ICreditLimitValidator>().To<CreditLimitValidator>().InSingletonScope();
            _ = kernel.Bind<IServiceProvider>().ToMethod(ctx => ctx.Kernel).InSingletonScope();

           

            _ = kernel.Bind<ILoggerFactory>().ToConstant(LoggerFactory.Create(builder =>
            {
                builder.AddDebug();
            })).InSingletonScope();
            _ = kernel.Bind(typeof(ILogger<>)).To(typeof(Logger<>)).InSingletonScope();
            _ = kernel.Bind<IDialogService>().To<DialogService>().InSingletonScope();
            _ = kernel.Bind<IPriceListCalculator>().To<StandardPriceListCalculator>().InSingletonScope().Named("Standard");
            _ = kernel.Bind<IPriceListCalculator>().To<AlternativePriceListCalculator>().InSingletonScope().Named("Alternative");
            _ = kernel.Bind<IPriceListCalculatorFactory>().To<PriceListCalculatorFactory>().InSingletonScope();
            _ = kernel.Bind<IGenericDataAccess<TempRecordGraphQLModel>>().To<TempRecordService>().InSingletonScope();
            _ = kernel.Bind<IParallelBatchProcessor>().To<ParallelBatchProcessor>().InSingletonScope();
            // Setup application clases
            // Books
            //_ = kernel.Bind<IBooksAccountingAccount>().To<BooksAccountingAccount>().InSingletonScope();
            //_ = kernel.Bind<IBooksAccountingSource>().To<BooksAccountingSource>().InSingletonScope();
            //_ = kernel.Bind<IBooksIdentificationType>().To<BooksIdentificationType>().InSingletonScope();
            //_ = kernel.Bind<IBooksAccountingEntity>().To<BooksAccountingEntity>().InSingletonScope();
            //_ = kernel.Bind<IAccountingEntries>().To<AccountingEntries>().InSingletonScope();
            //_ = kernel.Bind<IAccountingSources>().To<AccountingSoures>().InSingletonScope();
            //_ = kernel.Bind<IAuxiliaryBook>().To<AuxiliaryBook>().InSingletonScope();
            //_ = kernel.Bind<IEntityVsAccount>().To<EntityVsAccount>().InSingletonScope();
            //_ = kernel.Bind<ITestBalance>().To<TestBalance>().InSingletonScope();
            //_ = kernel.Bind<IAnnualIncomeStatement>().To<AnnualIncomeStatement>().InSingletonScope();
            //_ = kernel.Bind<IDailyBook>().To<DailyBook>().InSingletonScope();
            //_ = kernel.Bind<IBooksAccountingBook>().To<BooksAccountingBook>().InSingletonScope();
            //_ = kernel.Bind<ITestBalanceByEntity>().To<TestBalanceByEntity>().InSingletonScope();
            //_ = kernel.Bind<IBooksAccountingPresentation>().To<BooksAccountingPresentation>().InSingletonScope();
            //_ = kernel.Bind<IBooksAccountingEntryDraftMaster>().To<BooksAccountingEntryDraftMaster>().InSingletonScope();
            //_ = kernel.Bind<IBooksAccountingEntryDraftDetail>().To<BooksAccountingEntryDraftDetail>().InSingletonScope();
            //_ = kernel.Bind<IBooksAccountingEntryMaster>().To<BooksAccountingEntryMaster>().InSingletonScope();
            //_ = kernel.Bind<IBooksAccountingEntryDetail>().To<BooksAccountingEntryDetail>().InSingletonScope();

            // Billing
            //_ = kernel.Bind<IBillingCustomer>().To<BillingCustomer>().InSingletonScope();
            //_ = kernel.Bind<ICustomers>().To<Customers>().InSingletonScope();
            //_ = kernel.Bind<IBillingSeller>().To<BillingSeller>().InSingletonScope();
            //_ = kernel.Bind<ISellers>().To<Sellers>().InSingletonScope();
            //_ = kernel.Bind<IBillingDocumentSequence>().To<BillingDocumentSequence>().InSingletonScope();
            //_ = kernel.Bind<IDocumentSequence>().To<DocumentSequence>().InSingletonScope();

            // Account
            //_ = kernel.Bind<IAccount>().To<Account>().InSingletonScope();

            // Global
            //_ = kernel.Bind<IGlobalProcessType>().To<GlobalProcessType>().InSingletonScope();
            //_ = kernel.Bind<IGlobalAwsSes>().To<GlobalAwsSes>().InSingletonScope();
            //_ = kernel.Bind<IGlobalModule>().To<GlobalModule>().InSingletonScope();
            //_ = kernel.Bind<IGlobalCountry>().To<GlobalCountry>().InSingletonScope();
            //_ = kernel.Bind<IGlobalDepartment>().To<GlobalDepartment>().InSingletonScope();
            //_ = kernel.Bind<IGlobalSmtp>().To<GlobalSmtp>().InSingletonScope();
            //_ = kernel.Bind<IGlobalEmail>().To<GlobalEmail>().InSingletonScope();
            //_ = kernel.Bind<IGlobalCompany>().To<GlobalCompany>().InSingletonScope();
            //_ = kernel.Bind<IGlobalCompanyLocation>().To<GlobalCompanyLocation>().InSingletonScope();
            //_ = kernel.Bind<IGlobalCostCenter>().To<GlobalCostCenter>().InSingletonScope();

            // Inventory
            //_ = kernel.Bind<IInventoryItemType>().To<InventoryItemType>().InSingletonScope();

            // Treasury
            //_ = kernel.Bind<ICashDrawer>().To<CashDrawer>().InSingletonScope();
            //_ = kernel.Bind<ITreasuryCashDrawer>().To<TreasuryCashDrawer>().InSingletonScope();

            // Suppliers
            //_ = kernel.Bind<ISuppliersSupplier>().To<SuppliersSuplier>().InSingletonScope();
            //_ = kernel.Bind<ISupplier>().To<Supplier>().InSingletonScope();

            // Register ALL the ViewModels by Reflection
            GetType().Assembly.GetTypes()
            .Where(type => type.IsClass)
            .Where(type => type.Name.EndsWith("ViewModel"))
            .ToList()
            .ForEach(viewModelType => kernel.Bind(viewModelType).ToSelf().InTransientScope());

            var config = new MapperConfiguration(cfg =>
            {
                _ = cfg.CreateMap<AccountingEntityGraphQLModel, AccountingEntityDTO>();
                _ = cfg.CreateMap<IdentificationTypeGraphQLModel, IdentificationTypeDTO>();
                _ = cfg.CreateMap<RetentionTypeGraphQLModel, RetentionTypeDTO>();
                _ = cfg.CreateMap<SupplierGraphQLModel, SupplierDTO>();
                _ = cfg.CreateMap<CostCenterGraphQLModel, CostCenterDTO>();
                _ = cfg.CreateMap<SellerGraphQLModel, SellerDTO>();
                _ = cfg.CreateMap<AccountingSourceGraphQLModel, AccountingSourceDTO>();
                _ = cfg.CreateMap<AccountingAccountGraphQLModel, AccountingAccountDTO>();
                _ = cfg.CreateMap<AccountingEntryMasterGraphQLModel, AccountingEntryMasterDTO>();
                _ = cfg.CreateMap<AccountingEntryDraftMasterGraphQLModel, AccountingEntryDraftMasterDTO>();
                _ = cfg.CreateMap<AccountingEntryDraftDetailGraphQLModel, AccountingEntryDraftDetailDTO>();
                _ = cfg.CreateMap<MeasurementUnitGraphQLModel, MeasurementUnitDTO>();
                _ = cfg.CreateMap<ItemSizeMasterGraphQLModel, ItemSizeMasterDTO>();
                _ = cfg.CreateMap<ItemSizeDetailGraphQLModel, ItemSizeDetailDTO>();
                _ = cfg.CreateMap<CatalogGraphQLModel, CatalogDTO>();
                _ = cfg.CreateMap<ItemTypeGraphQLModel, ItemTypeDTO>();
                _ = cfg.CreateMap<ItemCategoryGraphQLModel, ItemCategoryDTO>();
                _ = cfg.CreateMap<ItemSubCategoryGraphQLModel, ItemSubCategoryDTO>();
                _ = cfg.CreateMap<ItemGraphQLModel, ItemDTO>();
                _ = cfg.CreateMap<BrandGraphQLModel, BrandDTO>();
                _ = cfg.CreateMap<AccountingGroupGraphQLModel, AccountingGroupDTO>();
                _ = cfg.CreateMap<EanCodeGraphQLModel, EanCodeDTO>();
                _ = cfg.CreateMap<ItemDetailGraphQLModel, ItemDetailDTO>();
                _ = cfg.CreateMap<ItemImageGraphQLModel, ItemImageDTO>();
                _ = cfg.CreateMap<CompanyGraphQLModel, CompanyDTO>();
                _ = cfg.CreateMap<CompanyLocationGraphQLModel, CompanyLocationDTO>();
                _ = cfg.CreateMap<StorageGraphQLModel, StorageDTO>();
                _ = cfg.CreateMap<CountryGraphQLModel, CountryDTO>();
                _ = cfg.CreateMap<DepartmentGraphQLModel, DepartmentDTO>();
                _ = cfg.CreateMap<CityGraphQLModel, CityDTO>();
                _ = cfg.CreateMap<CompanyLocationGraphQLModel, TreasuryMajorCashDrawerCompanyLocationMasterTreeDTO>();
                _ = cfg.CreateMap<CostCenterGraphQLModel, TreasuryMajorCashDrawerCostCenterMasterTreeDTO>();
                _ = cfg.CreateMap<CashDrawerGraphQLModel, MajorCashDrawerMasterTreeDTO>();
                _ = cfg.CreateMap<CompanyLocationGraphQLModel, TreasuryMinorCashDrawerCompanyLocationMasterTreeDTO>();
                _ = cfg.CreateMap<CostCenterGraphQLModel, TreasuryMinorCashDrawerCostCenterMasterTreeDTO>();
                _ = cfg.CreateMap<CashDrawerGraphQLModel, MinorCashDrawerMasterTreeDTO>();
                _ = cfg.CreateMap<CashDrawerGraphQLModel, TreasuryAuxiliaryCashDrawerMasterTreeDTO>();
                _ = cfg.CreateMap<BankGraphQLModel, TreasuryBankMasterTreeDTO>();
                _ = cfg.CreateMap<BankAccountGraphQLModel, TreasuryBankAccountMasterTreeDTO>();
                _ = cfg.CreateMap<FranchiseGraphQLModel, TreasuryFranchiseMasterTreeDTO>();
                _ = cfg.CreateMap<CostCenterGraphQLModel, TreasuryBankAccountCostCenterDTO>();
                _ = cfg.CreateMap<CostCenterGraphQLModel, TreasuryFranchiseCostCenterDTO>();
                _ = cfg.CreateMap<CreditLimitGraphQLModel, CreditLimitDTO>();
                _ = cfg.CreateMap<AccountingAccountGraphQLModel, AccountingAccountGroupDTO>();
                _ = cfg.CreateMap<AccountingAccountGroupDetailGraphQLModel, AccountingAccountGroupDetailDTO>();
                _ = cfg.CreateMap<PriceListDetailGraphQLModel, PriceListDetailDTO>();
                _ = cfg.CreateMap<PaymentMethodGraphQLModel, PaymentMethodPriceListDTO>();
                _ = cfg.CreateMap<ParameterGraphQLModel, DynamicControlModel>();
                _ = cfg.CreateMap<ItemGraphQLModel, PromotionCatalogItemDTO>();
                _ = cfg.CreateMap<AccountingBookGraphQLModel, AccountingBookDTO>();
                _ = cfg.CreateMap<ZoneGraphQLModel, ZoneDTO>();
            });

            _ = kernel.Bind<AutoMapper.IMapper>().ToConstant(config.CreateMapper());
        }

            // Automapper config
            //var config = new MapperConfiguration(cfg =>
            //{
            //    //// Inventory
            //    //_ = cfg.CreateMap<InventoryItemTypeModel, InventoryItemTypeDTO>();
            //    //// Biling
            //    //_ = cfg.CreateMap<BillingCustomerGraphQLModel, BillingCustomerDTO>();
            //    //_ = cfg.CreateMap<BillingSellerGraphQLModel, BillingSellerDTO>();
            //    //_ = cfg.CreateMap<BillingDocumentSequenceGraphQLModel, BillingDocumentSequenceDTO>();
            //    //// Global
            //    //_ = cfg.CreateMap<GlobalAwsSesGraphQLModel, GlobalAwsSesDTO>();
            //    //_ = cfg.CreateMap<GlobalSmtpGraphQLModel, GlobalSmtpDTO>();
            //    //_ = cfg.CreateMap<GlobalEmailGraphQLModel, GlobalEmailDTO>();
            //    //_ = cfg.CreateMap<GlobalCompanyGraphQLModel, GlobalCompanyDTO>();
            //    //_ = cfg.CreateMap<GlobalCompanyLocationGraphQLModel, GlobalCompanyLocationDTO>();
            //    //_ = cfg.CreateMap<GlobalCostCenterGraphQLModel, GlobalCostCenterDTO>();
            //    //// Books
            //    //_ = cfg.CreateMap<BooksAccountingSourceGraphQLModel, BooksAccountingSourceDTO>();
            //    //_ = cfg.CreateMap<BooksIdentificationTypeGraphQLModel, BooksIdentificationTypeDTO>();
            //    //_ = cfg.CreateMap<BooksAccountingEntityGraphQLModel, BooksAccountingEntityDTO>();
            //    //_ = cfg.CreateMap<BooksAccountingBookGraphQLModel, BooksAccountingBookDTO>();
            //    //_ = cfg.CreateMap<BooksAccountingPresentationGraphQLModel, BooksAccountingPresentationDTO>();
            //    //_ = cfg.CreateMap<BooksAccountingEntryDraftDetailGraphQLModel, BooksAccountingEntryDraftDetailDTO>();
            //    //_ = cfg.CreateMap<BooksAccountingEntryDraftMasterGraphQLModel, BooksAccountingEntryDraftMasterDTO>();
            //    //_ = cfg.CreateMap<BooksAccountingEntryMasterGraphQLModel, BooksAccountingEntryMasterDTO>();
            //    //_ = cfg.CreateMap<BooksRetentionTypeGraphQLModel, BooksRetentionTypeDTO>();
            //    //// Suppliers
            //    //_ = cfg.CreateMap<SuppliersSupplierGraphModel, SuppliersSupplierDTO>();
            //});

        //_ = kernel.Bind<IMapper>().ToConstant(config.CreateMapper());
        //    }

            protected override void OnExit(object sender, EventArgs e)
            {
                kernel.Dispose();
                base.OnExit(sender, e);
            }

            protected override object GetInstance(Type service, string key)
            {
                return service == null ? throw new ArgumentNullException("service") : kernel.Get(service);
            }

            protected override IEnumerable<object> GetAllInstances(Type service)
            {
                return kernel.GetAll(service);
            }

            protected override void BuildUp(object instance)
            {
                kernel.Inject(instance);
            }
        }
}
