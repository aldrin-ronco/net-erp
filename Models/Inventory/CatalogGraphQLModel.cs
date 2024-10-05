using Models.Books;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Inventory
{
    public class CatalogGraphQLModel
    {
        public int Id {  get; set; }
        public string Name { get; set; } = string.Empty;
        public IEnumerable<ItemTypeGraphQLModel> ItemsTypes { get; set; }
    }

    public class CatalogCreateMessage
    {
        public CatalogGraphQLModel CreatedCatalog { get; set; }
    }

    public class CatalogUpdateMessage
    {
        public CatalogGraphQLModel UpdatedCatalog { get; set; }
    }
    public class CatalogDeleteMessage 
    {
        public CatalogGraphQLModel DeletedCatalog { get; set; }
    }

    public class CatalogMasterDataContext
    {
        public List<MeasurementUnitGraphQLModel> MeasurementUnits { get; set; }
        public List<BrandGraphQLModel> Brands { get; set; }
        public List<AccountingGroupGraphQLModel> AccountingGroups { get; set; }
        public List<ItemSizeMasterGraphQLModel> Sizes { get; set; }
    }
}
