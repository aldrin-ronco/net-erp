using Models.Login;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Common.Interfaces
{
    public interface ICompanySeedService
    {
        Task<CompanySeedResultModel> RunSeedsAsync(
            int companyId,
            IProgress<string> progress,
            CancellationToken cancellationToken = default);
    }
}
