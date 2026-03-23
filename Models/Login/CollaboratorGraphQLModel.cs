using Models.Global;
using System.Collections.Generic;

namespace Models.Login
{
    public class CollaboratorGraphQLModel
    {
        public SystemAccountGraphQLModel Account { get; set; } = new();
    }

    public class CollaboratorPageGraphQLModel
    {
        public List<CollaboratorGraphQLModel> Entries { get; set; } = [];
        public int TotalEntries { get; set; }
    }
}
