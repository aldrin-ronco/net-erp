using Common.Helpers;
using Common.Interfaces;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Common.Services
{
    public class GraphQLClient : IGraphQLClient
    {
        private readonly GraphQLHttpClient _client;
        private string? _lastSessionId;
        private string? _lastCompanyId;
        private string? _lastDatabaseId;
        public GraphQLClient()
        {
            //Configuración para el certificado SSL
            var handler = new HttpClientHandler()
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
            _client = new GraphQLHttpClient(ConnectionConfig.MainGraphQLAPIUrl, new NewtonsoftJsonSerializer(), httpClient: new HttpClient(handler));
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
                ApplyHeaders();
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

        private void ApplyHeaders()
        {
            var headers = _client.HttpClient.DefaultRequestHeaders;

            // Función local: actualiza el header solo si hay valor; remueve si existía antes
            static void SetHeader(System.Net.Http.Headers.HttpRequestHeaders header, string name, string? value)
            {
                if (header.Contains(name)) header.Remove(name);
                if (!string.IsNullOrWhiteSpace(value)) header.Add(name, value);
            }

            // x-session-id: disponible luego de redimir ticket
            var currentSessionId = SessionInfo.SessionId;
            if (_lastSessionId != currentSessionId)
            {
                SetHeader(headers, "x-session-id", currentSessionId);
                _lastSessionId = currentSessionId;
            }

            // database-id: viene de Organization.DatabaseId del login
            var currentDatabaseId = SessionInfo.DatabaseId;
            if (!string.IsNullOrWhiteSpace(currentDatabaseId))
            {
                if (_lastDatabaseId != currentDatabaseId)
                {
                    SetHeader(headers, "database-id", currentDatabaseId);
                    _lastDatabaseId = currentDatabaseId;
                }
            }
            else
            {
                if (headers.Contains("database-id")) headers.Remove("database-id");
                _lastDatabaseId = null;
            }

            // company-id: viene de CurrentCompany.Id (tenant company id)
            if (SessionInfo.CurrentCompany != null)
            {
                var currentCompanyId = SessionInfo.CurrentCompany.Id.ToString();
                if (_lastCompanyId != currentCompanyId)
                {
                    SetHeader(headers, "company-id", currentCompanyId);
                    _lastCompanyId = currentCompanyId;
                }
            }
            else
            {

                // Nunca enviar company-id sin CurrentCompany
                if (headers.Contains("company-id")) headers.Remove("company-id");
                _lastCompanyId = null;
            }

            // x-device-id: estático por ahora (puedes reemplazar por un provider real de device ID)
            if (!headers.Contains("x-device-id"))
            {
                headers.Add("x-device-id", "pc12345abcde"); // TODO: reemplazar con un ID de dispositivo real
            }
            if (!headers.Contains("x-platform"))
            {
                headers.Add("x-platform", "PC"); // TODO: reemplazar con un ID de dispositivo real
            }
        }
    }
}