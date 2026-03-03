using Models.Global;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Models.Global.GraphQLResponseTypes;

namespace Models.Inventory
{
    public class ItemBrandGraphQLModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public CompanyGraphQLModel Company { get; set; } = new();
        public SystemAccountGraphQLModel CreatedBy { get; set; } = new();
        public DateTime InsertedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class ItemBrandDTO : ItemBrandGraphQLModel
    {

    }

    public class ItemBrandCreateMessage
    {
        public UpsertResponseType<ItemBrandGraphQLModel> CreatedItemBrand { get; set; } = new();
    }

    public class ItemBrandUpdateMessage
    {
        public UpsertResponseType<ItemBrandGraphQLModel> UpdatedItemBrand { get; set; } = new();
    }

    public class ItemBrandDeleteMessage
    {
        public DeleteResponseType DeletedItemBrand { get; set; } = new();
    }
}
