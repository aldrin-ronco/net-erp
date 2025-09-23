using Common.Helpers;
using Common.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using static Models.Global.GraphQLResponseTypes;

namespace Common.Interfaces
{
    /// <summary>
    /// Modern repository interface for data access operations using GraphQL.
    /// This is the recommended data access pattern for all new implementations.
    /// Provides comprehensive CRUD operations with cancellation token support and error handling.
    /// </summary>
    /// <typeparam name="TModel">The GraphQL model type for the entity being managed</typeparam>
    /// <example>
    /// <code>
    /// // Usage in a service or ViewModel
    /// IRepository&lt;CustomerGraphQLModel&gt; customerRepository;
    /// var customer = await customerRepository.FindByIdAsync(query, variables, cancellationToken);
    /// </code>
    /// </example>
    public interface IRepository<TModel>
    {
        /// <summary>
        /// Creates a new entity asynchronously using GraphQL mutation.
        /// </summary>
        /// <param name="query">The GraphQL mutation query string</param>
        /// <param name="variables">Variables object containing the data for the new entity</param>
        /// <param name="cancellationToken">Token to cancel the operation if needed</param>
        /// <returns>The created entity with server-generated values (like Id)</returns>
        /// <exception cref="System.Exception">Thrown when GraphQL mutation fails or validation errors occur</exception>
        Task<TModel> CreateAsync(string query, object variables, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Updates an existing entity asynchronously using GraphQL mutation.
        /// </summary>
        /// <param name="query">The GraphQL mutation query string</param>
        /// <param name="variables">Variables object containing the updated entity data</param>
        /// <param name="cancellationToken">Token to cancel the operation if needed</param>
        /// <returns>The updated entity with latest server values</returns>
        /// <exception cref="System.Exception">Thrown when GraphQL mutation fails, entity not found, or validation errors occur</exception>
        Task<TModel> UpdateAsync(string query, object variables, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Deletes an entity asynchronously using GraphQL mutation.
        /// </summary>
        /// <param name="query">The GraphQL mutation query string</param>
        /// <param name="variables">Variables object containing the entity identifier</param>
        /// <param name="cancellationToken">Token to cancel the operation if needed</param>
        /// <returns>The deleted entity information</returns>
        /// <exception cref="System.Exception">Thrown when GraphQL mutation fails or entity cannot be deleted due to foreign key constraints</exception>
        Task<TModel> DeleteAsync(string query, object variables, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Retrieves a single entity by its identifier asynchronously.
        /// </summary>
        /// <param name="query">The GraphQL query string to find the entity</param>
        /// <param name="variables">Variables object containing the entity identifier</param>
        /// <param name="cancellationToken">Token to cancel the operation if needed</param>
        /// <returns>The found entity or default(TModel) if not found</returns>
        /// <exception cref="System.Exception">Thrown when GraphQL query fails</exception>
        Task<TModel> FindByIdAsync(string query, object variables, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Retrieves a collection of entities asynchronously based on query criteria.
        /// </summary>
        /// <param name="query">The GraphQL query string</param>
        /// <param name="variables">Variables object containing filter criteria</param>
        /// <param name="cancellationToken">Token to cancel the operation if needed</param>
        /// <returns>Collection of entities matching the query criteria</returns>
        /// <exception cref="System.Exception">Thrown when GraphQL query fails</exception>
        Task<IEnumerable<TModel>> GetListAsync(string query, object variables, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Creates multiple entities in a single operation asynchronously.
        /// Useful for bulk insert operations with better performance than individual creates.
        /// </summary>
        /// <param name="query">The GraphQL mutation query string for bulk creation</param>
        /// <param name="variables">Variables object containing the collection of entities to create</param>
        /// <param name="cancellationToken">Token to cancel the operation if needed</param>
        /// <returns>Collection of created entities with server-generated values</returns>
        /// <exception cref="System.Exception">Thrown when GraphQL mutation fails or validation errors occur</exception>
        Task<IEnumerable<TModel>> CreateListAsync(string query, object variables, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Sends a GraphQL mutation that affects multiple entities and returns the results.
        /// General purpose method for complex mutations involving multiple entities.
        /// </summary>
        /// <param name="query">The GraphQL mutation query string</param>
        /// <param name="variables">Variables object containing the mutation parameters</param>
        /// <param name="cancellationToken">Token to cancel the operation if needed</param>
        /// <returns>Collection of entities affected by the mutation</returns>
        /// <exception cref="System.Exception">Thrown when GraphQL mutation fails</exception>
        Task<IEnumerable<TModel>> SendMutationListAsync(string query, object variables, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Retrieves a paginated result set asynchronously for efficient handling of large datasets.
        /// Essential for master/list views that need pagination controls.
        /// </summary>
        /// <param name="query">The GraphQL query string with pagination parameters</param>
        /// <param name="variables">Variables object containing pagination settings (page, pageSize, filters)</param>
        /// <param name="cancellationToken">Token to cancel the operation if needed</param>
        /// <returns>PageType containing the requested page of entities and total count information</returns>
        /// <exception cref="System.Exception">Thrown when GraphQL query fails</exception>
        Task<PageType<TModel>> GetPageAsync(string query, object variables, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Checks if an entity can be safely deleted without violating referential integrity.
        /// Should be called before attempting delete operations to provide user feedback.
        /// </summary>
        /// <param name="query">The GraphQL query string to check delete constraints</param>
        /// <param name="variables">Variables object containing the entity identifier</param>
        /// <param name="cancellationToken">Token to cancel the operation if needed</param>
        /// <returns>CanDeleteType indicating whether deletion is possible and any constraint messages</returns>
        /// <exception cref="System.Exception">Thrown when GraphQL query fails</exception>
        Task<CanDeleteType> CanDeleteAsync(string query, object variables, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Executes a custom GraphQL query and returns a strongly-typed response.
        /// Used for complex read operations that don't fit standard CRUD patterns.
        /// </summary>
        /// <typeparam name="TResponse">The expected response type structure</typeparam>
        /// <param name="query">The GraphQL query string</param>
        /// <param name="variables">Variables object containing query parameters</param>
        /// <param name="cancellationToken">Token to cancel the operation if needed</param>
        /// <returns>Strongly-typed response matching TResponse structure</returns>
        /// <exception cref="System.Exception">Thrown when GraphQL query fails</exception>
        Task<TResponse> GetDataContextAsync<TResponse>(string query, object variables, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Executes a custom GraphQL mutation and returns a strongly-typed response.
        /// Used for complex mutation operations that don't fit standard CRUD patterns.
        /// </summary>
        /// <typeparam name="TResponse">The expected response type structure</typeparam>
        /// <param name="query">The GraphQL mutation query string</param>
        /// <param name="variables">Variables object containing mutation parameters</param>
        /// <param name="cancellationToken">Token to cancel the operation if needed</param>
        /// <returns>Strongly-typed response matching TResponse structure</returns>
        /// <exception cref="System.Exception">Thrown when GraphQL mutation fails</exception>
        Task<TResponse> MutationContextAsync<TResponse>(string query, object variables, CancellationToken cancellationToken = default);

        Task<TResponse> CreateAsync<TResponse>(string query, object variables, CancellationToken cancellationToken = default);
        Task<TResponse> UpdateAsync<TResponse>(string query, object variables, CancellationToken cancellationToken = default);
        Task<TResponse> DeleteAsync<TResponse>(string query, object variables, CancellationToken cancellationToken = default);
    }
}