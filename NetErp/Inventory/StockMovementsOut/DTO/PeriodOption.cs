namespace NetErp.Inventory.StockMovementsOut.DTO
{
    public enum PeriodOption
    {
        Today,
        Yesterday,
        ThisWeek,
        Last7Days,
        LastWeek,
        Last14Days,
        ThisMonth,
        Last30Days,
        LastMonth,
        Custom
    }

    public class PeriodItem
    {
        public PeriodOption Value { get; init; }
        public string Label { get; init; } = string.Empty;
    }
}
