using GraphQL;
using GraphQL.Client.Http;
using Newtonsoft.Json;
using System;

namespace Common.Helpers
{
    public static class ExceptionHelper
    {
        public static string GetErrorMessage(this Exception ex)
        {
            if (ex is GraphQLHttpRequestException graphQLEx)
            {
                try
                {
                    // Intentar primero el formato HTTP Error (errores 401, 403, etc.)
                    GraphQLHttpError? httpError = JsonConvert.DeserializeObject<GraphQLHttpError>(
                        graphQLEx.Content?.ToString() ?? string.Empty);

                    if (httpError != null && !string.IsNullOrEmpty(httpError.Message))
                    {
                        return httpError.Message;
                    }

                    // Si no funciona, intentar el formato GraphQL Error
                    GraphQLError? graphQLError = JsonConvert.DeserializeObject<GraphQLError>(
                        graphQLEx.Content?.ToString() ?? string.Empty);

                    if (graphQLError?.Errors != null && graphQLError.Errors.Length > 0)
                    {
                        // Priorizar el mensaje de Extensions si existe
                        if (!string.IsNullOrEmpty(graphQLError.Errors[0].Extensions?.Message))
                        {
                            return graphQLError.Errors[0].Extensions.Message;
                        }
                        // Si no, usar el mensaje del error
                        if (!string.IsNullOrEmpty(graphQLError.Errors[0].Message))
                        {
                            return graphQLError.Errors[0].Message;
                        }
                    }
                }
                catch
                {
                    // Si falla la deserialización, usar el mensaje de la excepción
                }
            }

            return ex.Message;
        }
    }

    public class GraphQLHttpError
    {
        public string Message { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
    }
}
