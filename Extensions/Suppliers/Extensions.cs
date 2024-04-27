using Models.Suppliers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extensions.Suppliers
{
    public static class Extensions
    {
        public static void Replace(this ObservableCollection<SupplierDTO> suppliers, SupplierDTO updatedSupplier)
        {
            SupplierDTO supplierToReplace = suppliers.FirstOrDefault(x => x.Id == updatedSupplier.Id);
            if (supplierToReplace != null)
            {
                int index = suppliers.IndexOf(supplierToReplace);
                _ = suppliers.Remove(supplierToReplace);
                suppliers.Insert(index, updatedSupplier);
            }
        }
    }
}
