using Common.Helpers;
using Common.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using static Models.Global.GraphQLResponseTypes;

namespace Common.Interfaces
{
    public interface IRepository<TModel>
    {
        Task<TModel> CreateAsync(string query, object variables, CancellationToken cancellationToken = default);
        Task<TModel> UpdateAsync(string query, object variables, CancellationToken cancellationToken = default);
        Task<TModel> DeleteAsync(string query, object variables, CancellationToken cancellationToken = default);
        Task<TModel> FindByIdAsync(string query, object variables, CancellationToken cancellationToken = default);
        Task<IEnumerable<TModel>> GetListAsync(string query, object variables, CancellationToken cancellationToken = default);
        Task<IEnumerable<TModel>> CreateListAsync(string query, object variables, CancellationToken cancellationToken = default);
        Task<IEnumerable<TModel>> SendMutationListAsync(string query, object variables, CancellationToken cancellationToken = default);
        Task<PageType<TModel>> GetPageAsync(string query, object variables, CancellationToken cancellationToken = default);
        Task<CanDeleteType> CanDeleteAsync(string query, object variables, CancellationToken cancellationToken = default);
        Task<TResponse> GetDataContextAsync<TResponse>(string query, object variables, CancellationToken cancellationToken = default);
        Task<TResponse> MutationContextAsync<TResponse>(string query, object variables, CancellationToken cancellationToken = default);
    }
}