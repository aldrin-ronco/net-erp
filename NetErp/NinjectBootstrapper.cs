using AutoMapper;
using Caliburn.Micro;
using Common.Interfaces;
using DevExpress.Data.Utils;
using DevExpress.Entity.Model.Metadata;
using DevExpress.Xpf.Editors;
using DTOLibrary.Books;
using Models.Billing;
using Models.Books;
using Models.Global;
using Models.Suppliers;
using NetErp.Books.AccountingEntities.ViewModels;
using NetErp.Books.IdentificationTypes.DTO;
using NetErp.Global.MainMenu.ViewModels;
using NetErp.Global.Shell.ViewModels;
using Ninject;
using Services.Billing.DAL.PostgreSQL;
using Services.Books.DAL.PostgreSQL;
using Services.Global.DAL.PostgreSQL;
using Services.Suppliers.DAL.PostgreSQL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

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
            _ = kernel.Bind<IGenericDataAccess<IdentificationTypeGraphQLModel>>().To<IdentificationTypeService>().InSingletonScope();
            _ = kernel.Bind<IGenericDataAccess<CountryGraphQLModel>>().To<CountryService>().InSingletonScope();
            _ = kernel.Bind<IGenericDataAccess<CustomerGraphQLModel>>().To<CustomerService>().InSingletonScope();
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
                _ = cfg.CreateMap<CustomerGraphQLModel, CustomerDTO>();
                _ = cfg.CreateMap<RetentionTypeGraphQLModel, RetentionTypeDTO>();
                _ = cfg.CreateMap<SupplierGraphQLModel, SupplierDTO>();
                _ = cfg.CreateMap<CostCenterGraphQLModel, CostCenterDTO>();
                _ = cfg.CreateMap<SellerGraphQLModel, SellerDTO>();
                _ = cfg.CreateMap<AccountingSourceGraphQLModel, AccountingSourceDTO>();
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
