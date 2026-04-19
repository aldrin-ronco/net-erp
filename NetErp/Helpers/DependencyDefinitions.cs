using NetErp.Helpers.Cache;

namespace NetErp.Helpers
{
    public static class DependencyDefinitions
    {
        public static DependencyItem Catalogs(CatalogCache cache) => new(
            "Catálogos de productos",
            "Registre al menos un catálogo de productos en Inventario > Catálogo de productos",
            cache.IsInitialized && cache.Items.Count > 0);

        public static DependencyItem CostCenters(CostCenterCache cache) => new(
            "Centros de costo",
            "Registre al menos un centro de costo en Configuración > Centros de costo",
            cache.IsInitialized && cache.Items.Count > 0);

        public static DependencyItem CompanyLocations(CompanyLocationCache cache) => new(
            "Sedes",
            "Registre al menos una sede en Configuración > Parámetros > Sedes",
            cache.IsInitialized && cache.Items.Count > 0);

        public static DependencyItem Storages(StorageCache cache) => new(
            "Bodegas",
            "Registre al menos una bodega en Tesorería > Bodegas",
            cache.IsInitialized && cache.Items.Count > 0);
    }
}
