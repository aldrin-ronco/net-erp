using System;
using System.Threading;
using System.Threading.Tasks;

namespace Common.Interfaces
{
    public interface IGraphQLClient
    {
        Task<TResponse> ExecuteQueryAsync<TResponse>(string query, object variables, CancellationToken cancellationToken = default);
        Task<TResponse> ExecuteMutationAsync<TResponse>(string query, object variables, CancellationToken cancellationToken = default);
    }
}