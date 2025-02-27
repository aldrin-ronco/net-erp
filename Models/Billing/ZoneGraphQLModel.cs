using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Billing
{
    public class ZoneGraphQLModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
    }
    public class ZoneCreateMessage
    {
        public ZoneGraphQLModel CreateZone { get; set; } = new ZoneGraphQLModel();
    }
    public class ZoneUpdateMessage
    {
        public ZoneGraphQLModel UpdateZone { get; set; } = new ZoneGraphQLModel();
    }
    public class ZoneDeleteMessage
    {
        public ZoneGraphQLModel DeleteZone { get; set; } = new ZoneGraphQLModel();
    }
}
