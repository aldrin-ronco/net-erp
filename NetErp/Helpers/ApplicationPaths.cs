namespace NetErp.Helpers
{
    /// <summary>
    /// Rutas centralizadas de la aplicación relativas al directorio de instalación.
    /// Usar con DirectoryHelper.Exists(), DirectoryHelper.GetFullPath(), etc.
    /// </summary>
    public static class ApplicationPaths
    {
        public static class Reports
        {
            public const string Billing = @"reports\billing";
            public const string Books = @"reports\books";
            public const string Inventory = @"reports\inventory";

            public static class Templates
            {
                public const string AccountingAccountReport = @"reports\books\accounting_account_report.mrt";
            }
        }
    }
}
