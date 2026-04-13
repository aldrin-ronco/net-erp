using NetErp.Helpers.Cache;

namespace NetErp.Helpers
{
    public static class DependencyDefinitions
    {
        public static DependencyItem CostCenters(CostCenterCache cache) => new(
            "Centros de costo",
            "Registre al menos un centro de costo en Configuración > Centros de costo",
            cache.IsInitialized && cache.Items.Count > 0);
    }
}
