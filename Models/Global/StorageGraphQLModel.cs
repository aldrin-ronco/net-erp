using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Global
{
    public class StorageGraphQLModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public CityGraphQLModel City { get; set; }
        public string Address { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public CompanyLocationGraphQLModel Location { get; set; }
    }

    public class  StorageCreateMessage
    {
        public StorageGraphQLModel CreatedStorage { get; set; }
    }

    public class StorageUpdateMessage
    {
        public StorageGraphQLModel UpdatedStorage { get; set; }
    }

    public class StorageDeleteMessage
    {
        public StorageGraphQLModel DeletedStorage { get; set; }
    }

}
