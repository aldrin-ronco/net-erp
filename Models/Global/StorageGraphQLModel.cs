using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Models.Global.GraphQLResponseTypes;

namespace Models.Global
{
    public class StorageGraphQLModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public CityGraphQLModel City { get; set; } = new();
        public string Address { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public CompanyLocationGraphQLModel CompanyLocation { get; set; } = new();
        public DateTime InsertedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }

    public class StorageCreateMessage
    {
        public UpsertResponseType<StorageGraphQLModel> CreatedStorage { get; set; } = new();
    }

    public class StorageUpdateMessage
    {
        public UpsertResponseType<StorageGraphQLModel> UpdatedStorage { get; set; } = new();
    }

    public class StorageDeleteMessage
    {
        public DeleteResponseType DeletedStorage { get; set; } = new();
    }

}
