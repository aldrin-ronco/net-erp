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
        public CompanyGraphQLModel Company { get; set; }
        public IEnumerable<CostCenterGraphQLModel> CostCenters { get; set; }
        public IEnumerable<StorageGraphQLModel> Storages { get; set; }
        public override string ToString()
        {
            return Name;
        }
    }

    public class CompanyLocationCreateMessage
    {
        public CompanyLocationGraphQLModel CreatedCompanyLocation { get; set; }
    }

    public class CompanyLocationUpdateMessage
    {
        public CompanyLocationGraphQLModel UpdatedCompanyLocation { get; set; }
    }

    public class CompanyLocationDeleteMessage
    {
        public CompanyLocationGraphQLModel DeletedCompanyLocation { get; set; }
    }
}
