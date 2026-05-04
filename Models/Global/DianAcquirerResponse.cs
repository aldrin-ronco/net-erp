namespace Models.Global
{
    public class DianAcquirerResponse
    {
        public bool Status { get; set; }
        public string StatusCode { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string ReceiverName { get; set; } = string.Empty;
        public string ReceiverEmail { get; set; } = string.Empty;
    }
}
