using Models.Books;
using Models.Global;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Suppliers
{
    public class SupplierGraphQLModel
    {
        public int Id { get; set; }
        public bool IsTaxFree { get; set; } = false;
        public decimal IcaRetentionMargin { get; set; }
        public int IcaRetentionMarginBasis { get; set; }
        public bool RetainsAnyBasis { get; set; }
        public AccountingAccountGraphQLModel IcaAccountingAccount { get; set; }
        public AccountingEntityGraphQLModel Entity { get; set; }
        public ObservableCollection<RetentionTypeGraphQLModel> Retentions { get; set; }
    }

    public class SupplierDTO : SupplierGraphQLModel
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
    public class SupplierCreateMessage
    {
        public SupplierDTO CreatedSupplier { get; set; }

        public ObservableCollection<SupplierGraphQLModel> Suppliers { get; set; }
    }

    public class SupplierUpdateMessage
    {
        public SupplierDTO UpdatedSupplier { get; set; }
        public ObservableCollection<SupplierGraphQLModel> Suppliers { get; set; }
    }

    public class SupplierDeleteMessage
    {
        public SupplierDTO DeletedSupplier { get; set; }
    }
    public class SupplierDataContext
    {
        public ObservableCollection<IdentificationTypeGraphQLModel> IdentificationTypes { get; set; }
        public ObservableCollection<CountryGraphQLModel> Countries { get; set; }
        public ObservableCollection<RetentionTypeGraphQLModel> RetentionTypes { get; set; }
    }
}
