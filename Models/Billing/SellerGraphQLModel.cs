﻿using Common.Interfaces;
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
    public class SellerGraphQLModel
    {
        public int Id { get; set; }
        public bool IsActive { get; set; } = true;
        public  AccountingEntityGraphQLModel Entity { get; set; }
        public ObservableCollection<CostCenterGraphQLModel> CostCenters { get; set; }
    }

    public class SellerDTO : SellerGraphQLModel
    {
        public bool IsChecked { get; set; }
    }

    public class SellerCreateMessage
    {
        public SellerGraphQLModel CreatedSeller { get; set; }

        public ObservableCollection<SellerGraphQLModel> Sellers { get; set;}
    }

    public class SellerUpdateMessage
    {
        public SellerGraphQLModel UpdatedSeller { get; set; }

        public ObservableCollection<SellerGraphQLModel> Sellers { get; set; }
    }

    public class SellerDeleteMessage
    {
        public SellerGraphQLModel DeletedSeller { get; set; }
    }

    public class SellersDataContext
    {
        public ObservableCollection<IdentificationTypeGraphQLModel> IdentificationTypes { get; set; }
        public ObservableCollection<CountryGraphQLModel> Countries { get; set; }
        public ObservableCollection<CostCenterGraphQLModel> CostCenters { get; set; }
        public IGenericDataAccess<SellerGraphQLModel>.PageType SellerPage { get; set; }
    }
}
