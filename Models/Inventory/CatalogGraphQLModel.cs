using Models.Global;
using System;
using System.Collections.Generic;
using static Models.Global.GraphQLResponseTypes;

namespace Models.Inventory
{
    public class CatalogGraphQLModel
    {
        public int Id {  get; set; }
        public string Name { get; set; } = string.Empty;
        public CompanyGraphQLModel Company { get; set; } = new();
        public SystemAccountGraphQLModel CreatedBy { get; set; } = new();
        public DateTime InsertedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public IEnumerable<ItemTypeGraphQLModel> ItemTypes { get; set; }
    }

    public class CatalogCreateMessage
    {
        public UpsertResponseType<CatalogGraphQLModel> CreatedCatalog { get; set; } = new();
    }

    public class CatalogUpdateMessage
    {
        public UpsertResponseType<CatalogGraphQLModel> UpdatedCatalog { get; set; } = new();
    }
    public class CatalogDeleteMessage
    {
        public CatalogGraphQLModel DeletedCatalog { get; set; }
    }

}
