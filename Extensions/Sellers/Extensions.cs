using Models.Billing;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extensions.Sellers
{
    public static class Extensions
    {
        /// <summary>
        /// Replace seller in ObservableCollection<BillingSellerDTO>
        /// </summary>
        /// <param name="sellers">ObservableCollection<BillingSellerDTO></param>
        /// <param name="updatedSeller">BillingSellerDTO instance</param>
        public static void Replace(this ObservableCollection<SellerDTO> sellers, SellerDTO updatedSeller)
        {
            SellerDTO sellerToReplace = sellers.FirstOrDefault(x => x.Id == updatedSeller.Id);
            if (sellerToReplace != null)
            {
                int index = sellers.IndexOf(sellerToReplace);
                _ = sellers.Remove(sellerToReplace);
                sellers.Insert(index, updatedSeller);
            }
        }
    }
}
