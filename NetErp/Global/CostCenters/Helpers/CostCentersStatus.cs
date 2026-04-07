namespace NetErp.Global.CostCenters.Shared
{
    /// <summary>
    /// Constantes del estado tri-valor usado por CostCenter y Storage.
    /// Los valores corresponden al enum del backend GraphQL.
    /// </summary>
    public static class CostCentersStatus
    {
        public const string Active = "ACTIVE";
        public const string ReadOnly = "READ_ONLY";
        public const string Inactive = "INACTIVE";
    }
}
