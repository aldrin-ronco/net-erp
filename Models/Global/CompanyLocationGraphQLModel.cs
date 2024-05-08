using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Global
{
    public class CompanyLocationGraphQLModel
    {
        public int Id { get; set; } = 0;
        public string Name { get; set; } = string.Empty;
        public ObservableCollection<CostCenterDTO> CostCenters { get; set; } = new ObservableCollection<CostCenterDTO>();
        public override string ToString()
        {
            return Name;
        }
    }

    public class CompanyLocationDTO : CompanyLocationGraphQLModel
    {
        public bool IsExpanded { get; set; } = false;
    }
}
