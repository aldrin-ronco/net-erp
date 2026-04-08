using Models.Global;
using System.Numerics;

namespace Models.Books
{
    /// <summary>
    /// Línea de un comprobante contable publicado (read-only).
    /// Mapea al tipo <c>AccountingEntryLine</c> del schema GraphQL.
    /// </summary>
    public class AccountingEntryLineGraphQLModel
    {
        public BigInteger Id { get; set; } = 0;
        public AccountingAccountGraphQLModel AccountingAccount { get; set; }
        public AccountingEntityGraphQLModel AccountingEntity { get; set; }
        public CostCenterGraphQLModel CostCenter { get; set; }
        public string RecordDetail { get; set; } = string.Empty;
        public decimal Debit { get; set; } = 0;
        public decimal Credit { get; set; } = 0;
        public decimal Base { get; set; } = 0;
    }
}
