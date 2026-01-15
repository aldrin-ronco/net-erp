using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Models.Global.GraphQLResponseTypes;

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
        public UpsertResponseType<CompanyLocationGraphQLModel> CreatedCompanyLocation { get; set; } = new();
    }

    public class CompanyLocationUpdateMessage
    {
        public UpsertResponseType<CompanyLocationGraphQLModel> UpdatedCompanyLocation { get; set; } = new();
    }

    public class CompanyLocationDeleteMessage
    {
        public DeleteResponseType DeletedCompanyLocation { get; set; } = new();
    }
}
