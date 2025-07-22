using Models.Books;
using Models.Global;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Billing
{
    public class CustomerGraphQLModel
    {
        public int Id { get; set; }
        public int CreditTerm { get; set; } = 0;
        public bool IsTaxFree { get; set; } = false;
        public bool IsActive { get; set; } = false;
        public string BlockingReason { get; set; } = string.Empty;
        public bool RetainAnyBasis { get; set; } = false;
        public AccountingEntityGraphQLModel Entity { get; set; }
        public ObservableCollection<RetentionTypeDTO> Retentions { get; set; }
        public int SellerId { get; set; } = 0;
    }

    public class CustomerDTO : CustomerGraphQLModel
    {
        private bool _isChecked;
        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                }
            }
        }
    }

    public class CustomerCreateMessage
    {
        public CustomerGraphQLModel CreatedCustomer { get; set; }
    }

    public class CustomerUpdateMessage
    {
        public CustomerDTO UpdatedCustomer { get; set; }
    }

    public class CustomerDeleteMessage
    {
        public CustomerDTO DeletedCustomer { get; set; }
    }
    public class CustomersDataContext
    {
        public ObservableCollection<IdentificationTypeGraphQLModel> IdentificationTypes { get; set; }
        public ObservableCollection<CountryGraphQLModel> Countries { get; set; }
        public ObservableCollection<CustomerGraphQLModel> Sellers { get; set; }
        public ObservableCollection<RetentionTypeGraphQLModel> RetentionTypes { get; set; }
    }
}

