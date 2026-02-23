namespace Models.Login
{
    public class CompanySeedEventModel
    {
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? File { get; set; }
        public int? DurationMs { get; set; }
        public int? ErpCompanyId { get; set; }
        public string? Error { get; set; }
        public string? Message { get; set; }
    }
}
