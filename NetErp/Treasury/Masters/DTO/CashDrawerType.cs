namespace NetErp.Treasury.Masters.DTO
{
    /// <summary>
    /// Discriminador para diferenciar entre Cajas Generales y Cajas Menores
    /// en la jerarquía del árbol de Treasury.
    /// </summary>
    public enum CashDrawerType
    {
        Major,  // Cajas Generales
        Minor   // Cajas Menores
    }
}
