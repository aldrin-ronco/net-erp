using Common.Helpers;
using Common.Interfaces;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace Common.Services
{
    public class GraphQLClient : IGraphQLClient
    {
        private readonly GraphQLHttpClient _client;

        public GraphQLClient()
        {
            _client = new GraphQLHttpClient(ConnectionConfig.GraphQLAPIUrl, new NewtonsoftJsonSerializer());
            _client.HttpClient.DefaultRequestHeaders.Add("DatabaseId", ConnectionConfig.DatabaseId);
        }

        public async Task<TResponse> ExecuteQueryAsync<TResponse>(string query, object variables, CancellationToken cancellationToken = default)
        {
            return await ExecuteGraphQLOperation<TResponse>(query, variables, false, cancellationToken);
        }

        public async Task<TResponse> ExecuteMutationAsync<TResponse>(string query, object variables, CancellationToken cancellationToken = default)
        {
            return await ExecuteGraphQLOperation<TResponse>(query, variables, true, cancellationToken);
        }

        private async Task<TResponse> ExecuteGraphQLOperation<TResponse>(string query, object variables, bool isMutation, CancellationToken cancellationToken)
        {
            try
            {
                var request = new GraphQLRequest
                {
                    Query = query,
                    Variables = variables
                };

                GraphQLResponse<TResponse> result = isMutation
                    ? await _client.SendMutationAsync<TResponse>(request, cancellationToken)
                    : await _client.SendQueryAsync<TResponse>(request, cancellationToken);

                if (result.Errors != null && result.Errors.Length > 0)
                {
                    GraphQL.GraphQLError error = result.Errors[0];
                    Map? extensions = error.Extensions;
                    if (extensions != null && extensions.TryGetValue("message", out object? value))
                    {
                        throw new Exception(value.ToString());
                    }
                    throw new Exception(error.Message);
                }

                return result.Data;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }

    // Response wrapper types (compatible with existing queries)
    public class SingleItemResponseType<T>
    {
        public T CreateResponse { get; set; } = default!;
        public T UpdateResponse { get; set; } = default!;
        public T DeleteResponse { get; set; } = default!;
        public T SingleItemResponse { get; set; } = default!;
    }

    public class ListItemResponseType<T>
    {
        public ObservableCollection<T> ListResponse { get; set; } = new();
    }

    public class PageResponseType<T>
    {
        public PageType<T> PageResponse { get; set; } = new();
    }

    public class CanDeleteResponseType
    {
        public CanDeleteModel CanDeleteModel { get; set; } = new();
    }

    public class PageType<T>
    {
        public int Count { get; set; }
        public ObservableCollection<T> Rows { get; set; } = new();
    }

    public class CanDeleteModel
    {
        public bool CanDelete { get; set; } = false;
        public string Message { get; set; } = string.Empty;
    }
}