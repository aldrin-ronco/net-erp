using System.Collections.Generic;

namespace Models.Login
{
    public class CompanySeedResultModel
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = [];
        public int FilesExecuted { get; set; }
        public int FilesFailed { get; set; }
        public int FilesSkipped { get; set; }
    }
}
