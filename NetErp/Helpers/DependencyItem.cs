namespace NetErp.Helpers
{
    public class DependencyItem(string name, string description, bool isMet)
    {
        public string Name { get; } = name;
        public string Description { get; } = description;
        public bool IsMet { get; } = isMet;
    }
}
