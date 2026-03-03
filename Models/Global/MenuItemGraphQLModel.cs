namespace Models.Global
{
    public class MenuItemGraphQLModel
    {
        public int Id { get; set; }
        public string ItemKey { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public bool IsLockable { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
