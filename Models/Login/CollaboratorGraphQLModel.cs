using Models.Global;
using System;
using System.Collections.Generic;

namespace Models.Login
{
    public class CollaboratorGraphQLModel
    {
        public SystemAccountGraphQLModel Account { get; set; } = new();
        public CollaboratorCompanyGraphQLModel? Company { get; set; }
        public SystemAccountGraphQLModel? Inviter { get; set; }
        public DateTime InsertedAt { get; set; }

        public int OwnerAccountId => Company?.Organization?.Account?.Id ?? 0;
        public bool IsOwner => Account.Id == OwnerAccountId;
    }

    public class CollaboratorCompanyGraphQLModel
    {
        public CollaboratorOrganizationGraphQLModel? Organization { get; set; }
    }

    public class CollaboratorOrganizationGraphQLModel
    {
        public CollaboratorOwnerAccountGraphQLModel? Account { get; set; }
    }

    public class CollaboratorOwnerAccountGraphQLModel
    {
        public int Id { get; set; }
    }

    public class CollaboratorPageGraphQLModel
    {
        public List<CollaboratorGraphQLModel> Entries { get; set; } = [];
        public int TotalEntries { get; set; }
    }
}
