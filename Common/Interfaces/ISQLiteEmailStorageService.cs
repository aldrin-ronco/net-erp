using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Interfaces
{
    public interface ISQLiteEmailStorageService
    {
        Task<List<string>> GetSavedEmailsAsync();
        Task SaveEmailAsync(string email);
        Task<bool> ShouldPromptToSaveEmailAsync(string email);
        Task RemoveEmailAsync(string email);
    }
}
