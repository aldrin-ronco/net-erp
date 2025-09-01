using Models.Global;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Threading.Tasks;

namespace Extensions.Global
{
    public static class Extensions
    {
        public static void Replace(this ObservableCollection<CostCenterGraphQLModel> centers, CostCenterGraphQLModel updatedCostCenter)
        {
            CostCenterGraphQLModel centerToReplace = centers.Where(x => x.Id == updatedCostCenter.Id).FirstOrDefault();
            int index = centers.IndexOf(centerToReplace);
                centers.Remove(centerToReplace);
                centers.Insert(index, updatedCostCenter);
        }
    }
}
