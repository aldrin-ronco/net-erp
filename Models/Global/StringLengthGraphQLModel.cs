namespace Models.Global
{
    public class StringFieldLengthGraphQLModel
    {
        public string Column { get; set; } = string.Empty;
        public int MaxLength { get; set; }
    }

    public class EntityStringLengthsGraphQLModel
    {
        public string Entity { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Schema { get; set; } = string.Empty;
        public string Table { get; set; } = string.Empty;
        public List<StringFieldLengthGraphQLModel> Fields { get; set; } = [];
    }
}
