using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Models.Global.GraphQLResponseTypes;

namespace Models.Billing
{
    public class ZoneGraphQLModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
    public class ZoneCreateMessage
    {
        public UpsertResponseType<ZoneGraphQLModel> CreatedZone { get; set; } = new();
    }
    public class ZoneUpdateMessage
    {
        public UpsertResponseType<ZoneGraphQLModel> UpdatedZone { get; set; } = new ();
    }

    public class ZoneDeleteMessage
    {
        public DeleteResponseType DeletedZone { get; set; } = new();
    }
}
