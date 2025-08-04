using Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Common.Services
{
    public class GraphQLRepository<TModel> : IRepository<TModel>
    {
        private readonly IGraphQLClient _client;

        public GraphQLRepository(IGraphQLClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public async Task<TModel> CreateAsync(string query, object variables, CancellationToken cancellationToken = default)
        {
            var response = await _client.ExecuteMutationAsync<SingleItemResponseType<TModel>>(query, variables, cancellationToken);
            return response.CreateResponse;
        }

        public async Task<TModel> UpdateAsync(string query, object variables, CancellationToken cancellationToken = default)
        {
            var response = await _client.ExecuteMutationAsync<SingleItemResponseType<TModel>>(query, variables, cancellationToken);
            return response.UpdateResponse;
        }

        public async Task<TModel> DeleteAsync(string query, object variables, CancellationToken cancellationToken = default)
        {
            var response = await _client.ExecuteMutationAsync<SingleItemResponseType<TModel>>(query, variables, cancellationToken);
            return response.DeleteResponse;
        }

        public async Task<TModel> FindByIdAsync(string query, object variables, CancellationToken cancellationToken = default)
        {
            var response = await _client.ExecuteQueryAsync<SingleItemResponseType<TModel>>(query, variables, cancellationToken);
            return response.SingleItemResponse;
        }

        public async Task<IEnumerable<TModel>> GetListAsync(string query, object variables, CancellationToken cancellationToken = default)
        {
            var response = await _client.ExecuteQueryAsync<ListItemResponseType<TModel>>(query, variables, cancellationToken);
            return response.ListResponse;
        }

        public async Task<IEnumerable<TModel>> CreateListAsync(string query, object variables, CancellationToken cancellationToken = default)
        {
            var response = await _client.ExecuteMutationAsync<ListItemResponseType<TModel>>(query, variables, cancellationToken);
            return response.ListResponse;
        }

        public async Task<IEnumerable<TModel>> SendMutationListAsync(string query, object variables, CancellationToken cancellationToken = default)
        {
            var response = await _client.ExecuteMutationAsync<ListItemResponseType<TModel>>(query, variables, cancellationToken);
            return response.ListResponse;
        }

        public async Task<PageResult<TModel>> GetPageAsync(string query, object variables, CancellationToken cancellationToken = default)
        {
            var response = await _client.ExecuteQueryAsync<PageResponseType<TModel>>(query, variables, cancellationToken);
            return new PageResult<TModel>
            {
                Count = response.PageResponse.Count,
                Rows = response.PageResponse.Rows
            };
        }

        public async Task<CanDeleteResult> CanDeleteAsync(string query, object variables, CancellationToken cancellationToken = default)
        {
            var response = await _client.ExecuteQueryAsync<CanDeleteResponseType>(query, variables, cancellationToken);
            return new CanDeleteResult
            {
                CanDelete = response.CanDeleteModel.CanDelete,
                Message = response.CanDeleteModel.Message
            };
        }

        public async Task<TResponse> GetDataContextAsync<TResponse>(string query, object variables, CancellationToken cancellationToken = default)
        {
            return await _client.ExecuteQueryAsync<TResponse>(query, variables, cancellationToken);
        }

        public async Task<TResponse> MutationContextAsync<TResponse>(string query, object variables, CancellationToken cancellationToken = default)
        {
            return await _client.ExecuteMutationAsync<TResponse>(query, variables, cancellationToken);
        }
    }
}