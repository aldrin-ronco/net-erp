namespace Models.Inventory
{
    public class EanCodeByItemGraphQLModel
    {
        public string EanCode { get; set; } = string.Empty;
        public bool IsInternal { get; set; }
    }
}
