using Models.Global;
using Models.Login;
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

        public static string ToUserMessage(this List<GlobalErrorGraphQLModel> errors)
        {
            if (errors == null)
                return string.Empty;

            var list = errors.Where(e => e != null).ToList();
            if (list.Count == 0)
                return string.Empty;

            var sb = new StringBuilder();

            foreach (var e in list)
            {
                var message = (e.Message ?? string.Empty).Trim();
                if (!string.IsNullOrEmpty(message))
                    sb.AppendLine(message);
            }

            return sb.ToString().TrimEnd();
        }
    }
}
