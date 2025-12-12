using Models.Books;
using Models.Global;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Models.Global.GraphQLResponseTypes;

namespace Models.Billing
{
    public class CustomerGraphQLModel
    {
        public int Id { get; set; }
        public int CreditTerm { get; set; } = 0;
        public bool IsTaxFree { get; set; } = false;
        public bool IsActive { get; set; } = false;
        public string BlockingReason { get; set; } = string.Empty;
        public bool RetainsAnyBasis { get; set; } = false;
        public AccountingEntityGraphQLModel AccountingEntity { get; set; } = new();
        public ObservableCollection<WithholdingTypeGraphQLModel> WithholdingTypes { get; set; } = [];
        public ZoneGraphQLModel Zone { get; set; } = new();
        public DateTime InsertedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }

    public class CustomerCreateMessage
    {
        public UpsertResponseType<CustomerGraphQLModel> CreatedCustomer { get; set; } = new();
    }

    public class CustomerUpdateMessage
    {
        public UpsertResponseType<CustomerGraphQLModel> UpdatedCustomer { get; set; } = new();
    }

    public class CustomerDeleteMessage
    {
        public DeleteResponseType DeletedCustomer { get; set; } = new();
    }
    public class CustomersDataContext
    {
        public PageType<IdentificationTypeGraphQLModel> IdentificationTypes { get; set; } = new();
        public PageType<CountryGraphQLModel> Countries { get; set; } = new();
        public PageType<ZoneGraphQLModel> Zones { get; set; } = new();
        public PageType<WithholdingTypeGraphQLModel> WithholdingTypes { get; set; } = new();
    }
}

