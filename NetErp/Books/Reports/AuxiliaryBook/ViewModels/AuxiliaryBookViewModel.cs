using Caliburn.Micro;
using Common.Interfaces;
using Models.Books;
using Models.Global;
using Services.Books.DAL.PostgreSQL;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace NetErp.Books.Reports.AuxiliaryBook.ViewModels
{
    public class AuxiliaryBookViewModel : Conductor<Screen>.Collection.OneActive
    {
        public readonly IGenericDataAccess<AuxiliaryBookGraphQLModel> AuxiliaryBookService = IoC.Get<IGenericDataAccess<AuxiliaryBookGraphQLModel>>();

        // Presentaciones
        private ObservableCollection<AccountingPresentationGraphQLModel> _accountingPresentations;
        public ObservableCollection<AccountingPresentationGraphQLModel> AccountingPresentations
        {
            get { return _accountingPresentations; }
            set
            {
                if (_accountingPresentations != value)
                {
                    _accountingPresentations = value;
                    NotifyOfPropertyChange(nameof(AccountingPresentations));
                }
            }
        }

        // Centros de costos
        private ObservableCollection<CostCenterGraphQLModel> _costCenters;
        public ObservableCollection<CostCenterGraphQLModel> CostCenters
        {
            get { return _costCenters; }
            set
            {
                if (_costCenters != value)
                {
                    _costCenters = value;
                    NotifyOfPropertyChange(nameof(CostCenters));
                }
            }
        }

        // Fuente Contable
        private ObservableCollection<AccountingSourceGraphQLModel> _accountingSources;
        public ObservableCollection<AccountingSourceGraphQLModel> AccountingSources
        {
            get { return _accountingSources; }
            set
            {
                if (_accountingSources != value)
                {
                    _accountingSources = value;
                    NotifyOfPropertyChange(nameof(AccountingSources));
                }
            }
        }

        // Cuentas Contables
        private ObservableCollection<AccountingAccountGraphQLModel> _accountingAccounts;
        public ObservableCollection<AccountingAccountGraphQLModel> AccountingAccounts
        {
            get { return _accountingAccounts; }
            set
            {
                if (_accountingAccounts != value)
                {
                    _accountingAccounts = value;
                    NotifyOfPropertyChange(nameof(AccountingAccounts));
                }
            }
        }

        // Cuentas Contables para filtro de cuenta final, por alguna razon no me permite usar una sola fuente al usar el combo de autocompletado
        private ObservableCollection<AccountingAccountGraphQLModel> _accountingAccountsEnd;
        public ObservableCollection<AccountingAccountGraphQLModel> AccountingAccountsEnd
        {
            get { return _accountingAccountsEnd; }
            set
            {
                if (_accountingAccountsEnd != value)
                {
                    _accountingAccountsEnd = value;
                    NotifyOfPropertyChange(nameof(AccountingAccounts));
                }
            }
        }

        public static async Task<AuxiliaryBookViewModel> Create()
        {
            var _instance = new AuxiliaryBookViewModel();
            await _instance.Initialize();
            return _instance;
        }

        private async Task Initialize()
        {
            string query = @"
            query ($accountingAccountsWhere: AccountingAccountsWhereInput, $accountingSourcesWhere:AccountingSourcesWhereInput ) {
              accountingPresentations{
                id
                name
              },
              costCenters{
                id
                name
              },
              accountingSources(where: $accountingSourcesWhere) {
                id
                reverseId
                name
              },
              accountingAccounts(where: $accountingAccountsWhere) {
                id
                code
                name
              }
            }";

            object variables = new
            {
                AccountingAccountsWhere = new
                {
                    includeOnlyAuxiliaryAccounts = true
                },
                AccountingSourcesWhere = new
                {
                    Annulment = false
                }
            };
            var dataContext = await AuxiliaryBookService.GetDataContext<AuxiliaryBookDataContext>(query, variables);
            if (dataContext != null)
            {
                this.AccountingPresentations = new ObservableCollection<AccountingPresentationGraphQLModel>(dataContext.AccountingPresentations);
                this.AccountingSources = new ObservableCollection<AccountingSourceGraphQLModel>(dataContext.AccountingSources);
                this.CostCenters = new ObservableCollection<CostCenterGraphQLModel>(dataContext.CostCenters);
                this.AccountingAccounts = new ObservableCollection<AccountingAccountGraphQLModel>(dataContext.AccountingAccounts);
                this.AccountingAccountsEnd = new ObservableCollection<AccountingAccountGraphQLModel>(dataContext.AccountingAccounts);

                // Initial Selected Values
                if (this.CostCenters != null)
                    this.AuxiliaryBookResportViewModel.SelectedCostCenters = new ObservableCollection<CostCenterGraphQLModel>(this.CostCenters);

                if (this.AccountingPresentations != null)
                    this.AuxiliaryBookResportViewModel.SelectedAccountingPresentationId = this.AccountingPresentations.FirstOrDefault().Id;

                if (this.AccountingSources != null)
                    this.AuxiliaryBookResportViewModel.SelectedAccountingSources = new ObservableCollection<AccountingSourceGraphQLModel>(this.AccountingSources);

            }
        }

        public AuxiliaryBookReportViewModel AuxiliaryBookResportViewModel { get; set; }

        private AuxiliaryBookViewModel()
        {
            try
            {
                AuxiliaryBookResportViewModel = new AuxiliaryBookReportViewModel(this);
                Task.Run(() => ActivateReportView());
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task ActivateReportView()
        {
            await ActivateItemAsync(this.AuxiliaryBookResportViewModel, new System.Threading.CancellationToken());
        }

    }
}
