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

        public static string ToUserMessage(this List<GlobalErrorGraphQLModel> errors, string title = null)
        {
            if (errors == null)
                return string.Empty;

            var list = errors.Where(e => e != null).ToList();
            if (list.Count == 0)
                return string.Empty;

            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(title))
            {
                sb.AppendLine(title);
            }

            foreach (var e in list)
            {
                var field = string.IsNullOrWhiteSpace(e.Field) ? null : e.Field.Trim();
                var message = (e.Message ?? string.Empty).Trim();

                if (!string.IsNullOrEmpty(field))
                    sb.AppendLine($"- {field}: {message}");
                else
                    sb.AppendLine($"- {message}");
            }

            return sb.ToString().TrimEnd();
        }
    }
}
