using Models.Billing;
using Models.Books;
using Models.Global;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Models.Global.GraphQLResponseTypes;

namespace Models.Suppliers
{
    public class SupplierGraphQLModel
    {
        public int Id { get; set; }
        public bool IsTaxFree { get; set; } = false;
        public bool WithholdingAppliesOnAnyAmount { get; set; } = false;
        public decimal IcaWithholdingRate { get; set; }
        
        public AccountingAccountGraphQLModel IcaAccountingAccount { get; set; } = new();
        public AccountingEntityGraphQLModel AccountingEntity { get; set; } = new();
        public ObservableCollection<WithholdingTypeGraphQLModel> WithholdingTypes { get; set; } = [];
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
        public UpsertResponseType<SupplierGraphQLModel> CreatedSupplier { get; set; } = new();
    }

    public class SupplierUpdateMessage
    {
        public UpsertResponseType<SupplierGraphQLModel> UpdatedSupplier { get; set; } = new();
    }

    public class SupplierDeleteMessage
    {
        public DeleteResponseType DeletedSupplier { get; set; } = new();
    }
    public class SupplierDataContext
    {
        public PageType<SupplierGraphQLModel> Suppliers { get; set; }
        public PageType<AccountingAccountGraphQLModel> AccountingAccounts { get; set; }
    }
}
