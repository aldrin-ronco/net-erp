using Models.Books;
using Models.Global;

namespace Models.Inventory
{
    public class InventoryConfigGraphQLModel
    {
        public int Id { get; set; }
        public StorageGraphQLModel? TransitStorage { get; set; }
        public AccountingSourceGraphQLModel? TransferDestinationInAccountingSource { get; set; }
        public AccountingSourceGraphQLModel? TransferDocAccountingSource { get; set; }
        public AccountingSourceGraphQLModel? TransferSourceOutAccountingSource { get; set; }
        public AccountingSourceGraphQLModel? TransferTransitInAccountingSource { get; set; }
        public AccountingSourceGraphQLModel? TransferTransitOutAccountingSource { get; set; }
    }
}
