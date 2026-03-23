using Common.Helpers;
using Common.Interfaces;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Common.Services
{
    /// <summary>
    /// Singleton client for the authentication/accounts API.
    /// Centralizes URL, API key, and default headers so no other class
    /// needs to know connection details of the auth API.
    /// </summary>
    public class AuthApiClient : IAuthApiClient
    {
        private readonly GraphQLHttpClient _client;

        public AuthApiClient()
        {
            HttpClientHandler handler = new()
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            _client = new GraphQLHttpClient(
                ConnectionConfig.LoginAPIUrl,
                new NewtonsoftJsonSerializer(),
                httpClient: new HttpClient(handler));

            _client.HttpClient.DefaultRequestHeaders.Add("x-device-id", "pc12345abcde");
            _client.HttpClient.DefaultRequestHeaders.Add("x-platform", "PC");
            _client.HttpClient.DefaultRequestHeaders.Add("x-api-key", SessionInfo.ApiKey);
        }

        public async Task<GraphQLResponse<T>> SendQueryAsync<T>(GraphQLRequest request)
        {
            return await _client.SendQueryAsync<T>(request);
        }

        public async Task<GraphQLResponse<T>> SendMutationAsync<T>(GraphQLRequest request)
        {
            return await _client.SendMutationAsync<T>(request);
        }
    }
}
