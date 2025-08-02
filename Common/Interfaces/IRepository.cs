using Common.Helpers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

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
        Task<PageResult<TModel>> GetPageAsync(string query, object variables, CancellationToken cancellationToken = default);
        Task<CanDeleteResult> CanDeleteAsync(string query, object variables, CancellationToken cancellationToken = default);
        Task<TResponse> GetDataContextAsync<TResponse>(string query, object variables, CancellationToken cancellationToken = default);
        Task<TResponse> MutationContextAsync<TResponse>(string query, object variables, CancellationToken cancellationToken = default);
    }

    public class PageResult<T>
    {
        public int Count { get; set; }
        public ObservableCollection<T> Rows { get; set; } = new();
    }

    public class CanDeleteResult
    {
        public bool CanDelete { get; set; } = false;
        public string Message { get; set; } = string.Empty;
    }
}