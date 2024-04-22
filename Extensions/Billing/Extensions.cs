using Models.Billing;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extensions.Billing
{
    public static class Extensions
    {
        /// <summary>
        /// Replace customer in ObservableCollection<BillingCustomerDTO>
        /// </summary>
        /// <param name="customers">ObservableCollection<BillingCustomerDTO></param>
        /// <param name="updatedCustomer">BillingCustomerDTO instance</param>
        public static void Replace(this ObservableCollection<CustomerDTO> customers, CustomerDTO updatedCustomer)
        {
            CustomerDTO customerToReplace = customers.FirstOrDefault(x => x.Id == updatedCustomer.Id);
            if (customerToReplace != null)
            {
                int index = customers.IndexOf(customerToReplace);
                _ = customers.Remove(customerToReplace);
                customers.Insert(index, updatedCustomer);
            }
        }
    }
}
