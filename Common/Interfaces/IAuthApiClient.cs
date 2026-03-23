using GraphQL;
using System.Threading.Tasks;

namespace Common.Interfaces
{
    /// <summary>
    /// Centralized client for the authentication API (accounts.qtsolutions.com.co).
    /// Encapsulates connection configuration (URL, API key, headers) in a single place
    /// so that LoginService, CollaboratorCache, and any future auth-API consumers
    /// don't need to manage credentials independently.
    /// </summary>
    public interface IAuthApiClient
    {
        Task<GraphQLResponse<T>> SendQueryAsync<T>(GraphQLRequest request);
        Task<GraphQLResponse<T>> SendMutationAsync<T>(GraphQLRequest request);
    }
}
