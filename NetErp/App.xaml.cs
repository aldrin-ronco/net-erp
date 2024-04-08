using Common.Interfaces;
using Models.Books;
using NetErp.IoContainer;
using Ninject;
using Ninject.Modules;
using System;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Reflection;
using System.Windows.Controls;
using BooksServicesPostgreSQL = Services.Books.DAL.PostgreSQL;
using BooksServicesSQLServer = Services.Books.DAL.SQLServer;
using DevExpress.Xpf.WindowsUI.Navigation;
using Services.Books.DAL.PostgreSQL;
using NetErp.Books.AccountingAccounts.ViewModels;
using DevExpress.Mvvm;

namespace NetErp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            NinjectKernel.Kernel = new StandardKernel(new MyDIContainer());
            DISource.Resolver = Resolve;
        }

        object Resolve(Type type, object key, string name)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (name != null)
                return NinjectKernel.Kernel.Get(type, name);
            return NinjectKernel.Kernel.Get(type);
        }
    }

    public class MyDIContainer : NinjectModule
    {
        string? SQLEngine = Environment.GetEnvironmentVariable("NET_ERP_SQL_ENGINE");

        public override void Load()
        {
            if (SQLEngine == null || SQLEngine.Trim() == string.Empty) throw new InvalidEnumArgumentException(nameof(SQLEngine));

            Bind(typeof(IGenericDataAccess<AccountingAccountGraphQLModel>)).To(SQLEngine == "POSTGRESQL" ? typeof(BooksServicesPostgreSQL.AccountingAccountService) : typeof(BooksServicesSQLServer.AccountingAccountService)).InSingletonScope();
            Bind(typeof(AccountPlanMasterViewModel)).To(typeof(AccountPlanMasterViewModel)).InTransientScope();
            //Bind(typeof(AccountPlanDetailViewModel)).To(typeof(AccountPlanDetailViewModel)).InTransientScope();
            Bind(typeof(INavigationService)).To(typeof(FrameNavigationService)).InTransientScope();
            //Bind(typeof(IEventAggregator)).To(typeof(EventAggregator)).InSingletonScope();
        }
    }
}
