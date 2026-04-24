namespace Models.Billing
{
    public class BillingConfigGraphQLModel
    {
        public int Id { get; set; }
        public CustomerGraphQLModel? DefaultCustomer { get; set; }
    }
}
