using Amazon.Runtime.Internal;
using Common.Interfaces;
using GraphQL;
using Models.Login;
using System;
using System.Threading.Tasks;

namespace Common.Services
{
    public class LoginService(IAuthApiClient authApiClient) : ILoginService
    {
        private readonly IAuthApiClient _authApiClient = authApiClient;

        public async Task<LoginGraphQLModel> AuthenticateAsync(string email, string password)
        {
            string query = @"
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
                    accessTicket {
                      expiresAt
                      ticket
                    }
                    companies {
                      company {
                        id
                        reference
                        status
                        address
                        businessName
                        captureType
                        tenantCompanyId
                        seedStatus
                        defaultCurrency{
                            code
                        }
                        country {
                          code
                        }
                        department {
                          code
                        }
                        city {
                          code
                        }
                        firstLastName
                        firstName
                        fullName
                        identificationNumber
                        identificationType {
                          code
                        }
                        middleLastName
                        middleName
                        primaryCellPhone
                        secondaryCellPhone
                        primaryPhone
                        secondaryPhone
                        regime
                        searchName
                        tradeName
                        verificationDigit
                        telephonicInformation
                        updatedAt
                        insertedAt
                        organization {
                          id
                          name
                          databaseId
                        }
                        updatedAt
                        insertedAt
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

            GraphQLResponse<LoginResponseType> result = await _authApiClient.SendMutationAsync<LoginResponseType>(new GraphQLRequest()
            {
                Query = query,
                Variables = new { input = new { email, password } }
            });

            if (result.Errors != null)
            {
                GraphQLError error = result.Errors[0];
                Map? extensions = error.Extensions;
                if (extensions != null && extensions.TryGetValue("message", out object? value))
                {
                    throw new Exception(value.ToString());
                }
                throw new Exception(error.Message);
            }

            return result.Data.LoginAccount;
        }

        public async Task<LoginValidateTicketGraphQLModel> RedeemTicketAsync(string accessTicket)
        {
            string query = @"
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

            GraphQLResponse<RedeemTicketResponseType> result = await _authApiClient.SendMutationAsync<RedeemTicketResponseType>(new GraphQLRequest()
            {
                Query = query,
                Variables = new { input = new { ticket = accessTicket } }
            });

            if (result.Errors != null)
            {
                GraphQLError error = result.Errors[0];
                Map? extensions = error.Extensions;
                if (extensions != null && extensions.TryGetValue("message", out object? value))
                {
                    throw new Exception(value.ToString());
                }
                throw new Exception(error.Message);
            }

            return result.Data.ValidateTicket;
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
