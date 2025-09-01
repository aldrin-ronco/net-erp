using Common.Helpers;
using Common.Interfaces;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Models.Login;
using System;
using System.Threading.Tasks;

namespace Common.Services
{
    public class LoginService : ILoginService
    {
        public async Task<LoginGraphQLModel> AuthenticateAsync(string email, string password)
        {
            try
            {
                GraphQLHttpClient client = new(ConnectionConfig.LoginAPIUrl, new NewtonsoftJsonSerializer());
                
                var loginQuery = @"
                    mutation ($input: LoginAccountInput!) {
                      loginAccount(input: $input) {
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
                    Query = loginQuery,
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

        private class LoginResponseType
        {
            public LoginGraphQLModel LoginAccount { get; set; }
        }
    }
}