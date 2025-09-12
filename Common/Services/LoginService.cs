using Amazon.Runtime.Internal;
using Common.Helpers;
using Common.Interfaces;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Models.Login;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Common.Services
{
    public class LoginService : ILoginService
    {
        public async Task<LoginGraphQLModel> AuthenticateAsync(string email, string password)
        {
            try
            {
                //Configuración momentanea para evitar la verificación del certificado SSL (en desarrollo)
                var handler = new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                };
                GraphQLHttpClient client = new(ConnectionConfig.LoginAPIUrl, new NewtonsoftJsonSerializer(), httpClient: new HttpClient(handler));
                
                var query = @"
                mutation ($input: LoginAccountInput!) {
                  loginAccount(input: $input) {
                    accessTicket {
                      expiresAt
                      ticket
                    }
                    account {
                      id
                      email
                      firstName
                      middleName
                      firstLastName
                      middleLastName
                      insertedAt
                      updatedAt
                    }
                    companies {
                      company {
                        id
                        name
                        reference
                        license {
                          id
                          organization {
                            id
                            name
                          }
                        }
                      }
                      role
                    }
                    success
                    message
                    errors {
                      field
                      message
                    }
                  }
                }";

                var variables = new
                {
                    input = new
                    {
                        email,
                        password
                    }
                };

                GraphQLResponse<LoginResponseType> result = await client.SendMutationAsync<LoginResponseType>(new GraphQLRequest()
                {
                    Query = query,
                    Variables = variables
                });

                if (result.Errors != null)
                {
                    GraphQL.GraphQLError error = result.Errors[0];
                    Map? extensions = error.Extensions;
                    if (extensions != null && extensions.TryGetValue("message", out object? value))
                    {
                        throw new Exception(value.ToString());
                    }
                    throw new Exception(error.Message);
                }

                return result.Data.LoginAccount;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<LoginValidateTicketGraphQLModel> RedeemTicketAsync(string accessTicket)
        {
            try
            {
                //Configuración momentanea para evitar la verificación del certificado SSL (en desarrollo)
                var handler = new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                };
                GraphQLHttpClient client = new(ConnectionConfig.LoginAPIUrl, new NewtonsoftJsonSerializer(), httpClient: new HttpClient(handler));

                var query = @"
                    mutation ($input: ValidateTicketInput!) {
                      validateTicket(input: $input) {
                        account {
                          id
                          email
                        }
                        sessionId
                        success
                        message
                        errors {
                          field
                          message
                        }
                      }
                    }";

                var variables = new
                {
                    input = new
                    {
                        ticket = accessTicket
                    }
                };

                GraphQLResponse<RedeemTicketResponseType> result = await client.SendMutationAsync<RedeemTicketResponseType>(new GraphQLRequest()
                {
                    Query = query,
                    Variables = variables
                });

                if (result.Errors != null)
                {
                    GraphQL.GraphQLError error = result.Errors[0];
                    Map? extensions = error.Extensions;
                    if (extensions != null && extensions.TryGetValue("message", out object? value))
                    {
                        throw new Exception(value.ToString());
                    }
                    throw new Exception(error.Message);
                }

                return result.Data.ValidateTicket;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private class LoginResponseType
        {
            public LoginGraphQLModel LoginAccount { get; set; } = new();
        }

        private class RedeemTicketResponseType
        {
            public LoginValidateTicketGraphQLModel ValidateTicket { get; set; } = new();
        }
    }
}