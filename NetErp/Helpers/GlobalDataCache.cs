using Caliburn.Micro;
using Models.Books;
using Models.Global;
using Models.Treasury;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NetErp.Helpers
{
    /// <summary>
    /// Caché global para datos comunes utilizados en múltiples módulos.
    /// Registrado como Singleton en Ninject - carga los datos una vez y los mantiene en memoria.
    /// Se suscribe a mensajes para actualizaciones granulares.
    /// </summary>
    public class GlobalDataCache
    {
        private readonly IEventAggregator _eventAggregator;
        private bool _isInitialized = false;
        private readonly object _lock = new();

        #region Properties

        /// <summary>
        /// Tipos de identificación disponibles en el sistema
        /// </summary>
        public ObservableCollection<IdentificationTypeGraphQLModel> IdentificationTypes { get; private set; } = [];

        /// <summary>
        /// Países con sus departamentos y ciudades
        /// </summary>
        public ObservableCollection<CountryGraphQLModel> Countries { get; private set; } = [];

        /// <summary>
        /// Centros de costo
        /// </summary>
        public ObservableCollection<CostCenterGraphQLModel> CostCenters { get; private set; } = [];

        /// <summary>
        /// Cuentas contables auxiliares (código >= 8 dígitos)
        /// </summary>
        public ObservableCollection<AccountingAccountGraphQLModel> AuxiliaryAccountingAccounts { get; private set; } = [];

        /// <summary>
        /// Cuentas bancarias
        /// </summary>
        public ObservableCollection<BankAccountGraphQLModel> BankAccounts { get; private set; } = [];

        /// <summary>
        /// Cajas generales (no caja menor)
        /// </summary>
        public ObservableCollection<CashDrawerGraphQLModel> MajorCashDrawers { get; private set; } = [];

        /// <summary>
        /// Indica si el caché ya ha sido inicializado
        /// </summary>
        public bool IsInitialized
        {
            get
            {
                lock (_lock)
                {
                    return _isInitialized;
                }
            }
        }

        #endregion

        #region Constructor

        public GlobalDataCache(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
            // Suscripción a mensajes para actualizaciones granulares
            _eventAggregator.SubscribeOnUIThread(this);
        }

        #endregion

        #region Initialize / Clear / Refresh

        /// <summary>
        /// Inicializa el caché con los datos básicos (IdentificationTypes, Countries).
        /// Este método debe ser llamado al inicio de la aplicación después del login.
        /// </summary>
        public void Initialize(
            ObservableCollection<IdentificationTypeGraphQLModel> identificationTypes,
            ObservableCollection<CountryGraphQLModel> countries)
        {
            lock (_lock)
            {
                if (_isInitialized)
                {
                    throw new InvalidOperationException("GlobalDataCache ya ha sido inicializado. Use Clear() antes de reinicializar.");
                }

                IdentificationTypes = identificationTypes ?? [];
                Countries = countries ?? [];
                _isInitialized = true;
            }
        }

        /// <summary>
        /// Inicializa datos adicionales de Treasury.
        /// Puede llamarse después de Initialize() para cargar datos específicos de módulos.
        /// </summary>
        public void InitializeTreasuryData(
            ObservableCollection<CostCenterGraphQLModel>? costCenters = null,
            ObservableCollection<AccountingAccountGraphQLModel>? auxiliaryAccountingAccounts = null,
            ObservableCollection<BankAccountGraphQLModel>? bankAccounts = null,
            ObservableCollection<CashDrawerGraphQLModel>? majorCashDrawers = null)
        {
            lock (_lock)
            {
                if (costCenters != null)
                    CostCenters = costCenters;
                if (auxiliaryAccountingAccounts != null)
                    AuxiliaryAccountingAccounts = auxiliaryAccountingAccounts;
                if (bankAccounts != null)
                    BankAccounts = bankAccounts;
                if (majorCashDrawers != null)
                    MajorCashDrawers = majorCashDrawers;
            }
        }

        /// <summary>
        /// Limpia el caché y permite reinicializar.
        /// Útil para cambios de sesión o logout.
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                IdentificationTypes.Clear();
                Countries.Clear();
                CostCenters.Clear();
                AuxiliaryAccountingAccounts.Clear();
                BankAccounts.Clear();
                MajorCashDrawers.Clear();
                _isInitialized = false;
            }
        }

        #endregion

        #region Granular Updates - CostCenters

        public void AddCostCenter(CostCenterGraphQLModel item)
        {
            lock (_lock)
            {
                if (!CostCenters.Any(x => x.Id == item.Id))
                    CostCenters.Add(item);
            }
        }

        public void UpdateCostCenter(CostCenterGraphQLModel item)
        {
            lock (_lock)
            {
                var existing = CostCenters.FirstOrDefault(x => x.Id == item.Id);
                if (existing != null)
                {
                    var index = CostCenters.IndexOf(existing);
                    CostCenters[index] = item;
                }
            }
        }

        public void RemoveCostCenter(int id)
        {
            lock (_lock)
            {
                var item = CostCenters.FirstOrDefault(x => x.Id == id);
                if (item != null)
                    CostCenters.Remove(item);
            }
        }

        #endregion

        #region Granular Updates - BankAccounts

        public void AddBankAccount(BankAccountGraphQLModel item)
        {
            lock (_lock)
            {
                if (!BankAccounts.Any(x => x.Id == item.Id))
                    BankAccounts.Add(item);
            }
        }

        public void UpdateBankAccount(BankAccountGraphQLModel item)
        {
            lock (_lock)
            {
                var existing = BankAccounts.FirstOrDefault(x => x.Id == item.Id);
                if (existing != null)
                {
                    var index = BankAccounts.IndexOf(existing);
                    BankAccounts[index] = item;
                }
            }
        }

        public void RemoveBankAccount(int id)
        {
            lock (_lock)
            {
                var item = BankAccounts.FirstOrDefault(x => x.Id == id);
                if (item != null)
                    BankAccounts.Remove(item);
            }
        }

        #endregion

        #region Granular Updates - MajorCashDrawers

        public void AddMajorCashDrawer(CashDrawerGraphQLModel item)
        {
            lock (_lock)
            {
                if (!MajorCashDrawers.Any(x => x.Id == item.Id))
                    MajorCashDrawers.Add(item);
            }
        }

        public void UpdateMajorCashDrawer(CashDrawerGraphQLModel item)
        {
            lock (_lock)
            {
                var existing = MajorCashDrawers.FirstOrDefault(x => x.Id == item.Id);
                if (existing != null)
                {
                    var index = MajorCashDrawers.IndexOf(existing);
                    MajorCashDrawers[index] = item;
                }
            }
        }

        public void RemoveMajorCashDrawer(int id)
        {
            lock (_lock)
            {
                var item = MajorCashDrawers.FirstOrDefault(x => x.Id == id);
                if (item != null)
                    MajorCashDrawers.Remove(item);
            }
        }

        #endregion

        #region Granular Updates - AuxiliaryAccountingAccounts

        public void AddAuxiliaryAccountingAccount(AccountingAccountGraphQLModel item)
        {
            lock (_lock)
            {
                if (!AuxiliaryAccountingAccounts.Any(x => x.Id == item.Id))
                    AuxiliaryAccountingAccounts.Add(item);
            }
        }

        public void UpdateAuxiliaryAccountingAccount(AccountingAccountGraphQLModel item)
        {
            lock (_lock)
            {
                var existing = AuxiliaryAccountingAccounts.FirstOrDefault(x => x.Id == item.Id);
                if (existing != null)
                {
                    var index = AuxiliaryAccountingAccounts.IndexOf(existing);
                    AuxiliaryAccountingAccounts[index] = item;
                }
            }
        }

        public void RemoveAuxiliaryAccountingAccount(int id)
        {
            lock (_lock)
            {
                var item = AuxiliaryAccountingAccounts.FirstOrDefault(x => x.Id == id);
                if (item != null)
                    AuxiliaryAccountingAccounts.Remove(item);
            }
        }

        #endregion
    }
}
