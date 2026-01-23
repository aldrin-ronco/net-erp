using Models.Books;
using Models.Treasury;
using System.Collections.ObjectModel;
using static Models.Global.GraphQLResponseTypes;

namespace Models.Global
{
    /// <summary>
    /// Modelo para cargar datos comunes del sistema en una sola consulta GraphQL.
    /// Utilizado para inicializar el GlobalDataCache al inicio de la aplicación.
    /// </summary>
    public class GlobalDataContextModel
    {
        public PageType<IdentificationTypeGraphQLModel> IdentificationTypes { get; set; } = new();
        public PageType<CountryGraphQLModel> Countries { get; set; } = new();

        // Treasury data
        public PageType<CostCenterGraphQLModel> CostCenters { get; set; } = new();
        public PageType<AccountingAccountGraphQLModel> AuxiliaryAccountingAccounts { get; set; } = new();
        public PageType<CashDrawerGraphQLModel> MajorCashDrawers { get; set; } = new();
        public PageType<BankAccountGraphQLModel> BankAccounts { get; set; } = new();
    }
}
