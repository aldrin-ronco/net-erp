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
    public class SellerGraphQLModel
    {
        public int Id { get; set; }
        public bool IsActive { get; set; } = true;
        public AccountingEntityGraphQLModel? AccountingEntity { get; set; }
        public ObservableCollection<CostCenterGraphQLModel> CostCenters { get; set; } = [];
        public ZoneGraphQLModel? Zone { get; set; }        
    }

    public class SellerDTO : SellerGraphQLModel
    {
        public bool IsChecked { get; set; }
    }

    public class SellerCreateMessage
    {
        public required UpsertResponseType<SellerGraphQLModel> CreatedSeller { get; set; }

    }

    public class SellerUpdateMessage
    {
        public required UpsertResponseType<SellerGraphQLModel> UpdatedSeller { get; set; }

    }

    public class SellerDeleteMessage
    {
        public required DeleteResponseType DeletedSeller { get; set; }
    }
    public class SellersByIdDataContext
    {
        public required SellerGraphQLModel Seller { get; set; }
    }
      
}
