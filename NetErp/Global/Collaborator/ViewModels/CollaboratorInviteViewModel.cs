using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Microsoft.VisualStudio.Threading;
using Models.Global;
using NetErp.Global.Collaborator.DTO;
using NetErp.Helpers;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Global.Collaborator.ViewModels
{
    public class CollaboratorInviteViewModel : Screen
    {
        #region Dependencies

        private readonly IAuthApiClient _authApiClient;
        private readonly IRepository<AccountGraphQLModel> _accountService;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly DebouncedAction _searchDebounce = new();

        #endregion

        #region Constructor

        public CollaboratorInviteViewModel(
            IAuthApiClient authApiClient,
            IRepository<AccountGraphQLModel> accountService,
            JoinableTaskFactory joinableTaskFactory)
        {
            _authApiClient = authApiClient;
            _accountService = accountService;
            _joinableTaskFactory = joinableTaskFactory;

        }

        #endregion

        #region Properties

        public double DialogWidth
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(DialogWidth));
                }
            }
        } = 500;

        public double DialogHeight
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(DialogHeight));
                }
            }
        } = 450;

        public bool IsBusy
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IsBusy));
                }
            }
        }

        public string SearchEmail
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SearchEmail));
                    if (!string.IsNullOrEmpty(value) && value.Contains('@'))
                        _ = _searchDebounce.RunAsync(SearchAccountAsync);
                }
            }
        } = string.Empty;

        public ObservableCollection<AccountSearchResultDTO> SearchResults
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SearchResults));
                    NotifyOfPropertyChange(nameof(AccountFound));
                }
            }
        } = [];

        public AccountSearchResultDTO? SelectedAccount
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedAccount));
                    NotifyOfPropertyChange(nameof(CanInvite));
                }
            }
        }

        public bool AccountFound => SearchResults.Count > 0;

        public bool AccountNotFound
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(AccountNotFound));
                }
            }
        }

        public bool CanInvite => SelectedAccount is not null;

        #endregion

        #region Commands

        private ICommand? _inviteCommand;
        public ICommand InviteCommand
        {
            get
            {
                _inviteCommand ??= new AsyncCommand(InviteAsync);
                return _inviteCommand;
            }
        }

        public void SelectAccount(AccountSearchResultDTO account)
        {
            foreach (AccountSearchResultDTO item in SearchResults)
                item.IsSelected = false;
            account.IsSelected = true;
            SelectedAccount = account;
        }

        private ICommand? _cancelCommand;
        public ICommand CancelCommand
        {
            get
            {
                _cancelCommand ??= new AsyncCommand(CancelAsync);
                return _cancelCommand;
            }
        }

        #endregion

        #region Operations

        private async Task SearchAccountAsync()
        {
            try
            {
                IsBusy = true;
                SearchResults = [];
                SelectedAccount = null;
                AccountNotFound = false;

                GraphQL.GraphQLResponse<AccountSearchResponse> result = await _authApiClient.SendQueryAsync<AccountSearchResponse>(
                    new GraphQL.GraphQLRequest
                    {
                        Query = _searchAccountQuery.Value,
                        Variables = new
                        {
                            filters = new { emailContains = SearchEmail.Trim() },
                            pagination = new { pageSize = 4 }
                        }
                    });

                if (result.Errors != null && result.Errors.Length > 0)
                {
                    AccountNotFound = true;
                    return;
                }

                if (result.Data.AccountsPage.Entries.Count == 0)
                {
                    AccountNotFound = true;
                    return;
                }

                SearchResults = [.. result.Data.AccountsPage.Entries.Select(account => new AccountSearchResultDTO
                {
                    Id = account.Id,
                    Email = account.Email,
                    FullName = account.FullName,
                    FirstName = account.FirstName,
                    MiddleName = account.MiddleName,
                    FirstLastName = account.FirstLastName,
                    MiddleLastName = account.MiddleLastName,
                    Profession = account.Profession,
                    PhotoUrl = account.PhotoUrl
                })];

                if (SearchResults.Count == 1)
                {
                    SearchResults[0].IsSelected = true;
                    SelectedAccount = SearchResults[0];
                }
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !", $"Error al buscar cuenta.\r\n{ex.GetErrorMessage()}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task InviteAsync()
        {
            try
            {
                IsBusy = true;

                // 1. Create collaborator in Auth API
                GraphQL.GraphQLResponse<CreateCollaboratorResponse> authResult = await _authApiClient.SendMutationAsync<CreateCollaboratorResponse>(
                    new GraphQL.GraphQLRequest
                    {
                        Query = _createCollaboratorMutation.Value,
                        Variables = new
                        {
                            input = new
                            {
                                accountId = SelectedAccount!.Id,
                                companyId = SessionInfo.LoginCompanyId,
                                invitedBy = SessionInfo.LoginAccountId
                            }
                        }
                    });

                if (authResult.Errors != null && authResult.Errors.Length > 0)
                {
                    ThemedMessageBox.Show("Atención !", $"No se pudo invitar al colaborador.\n\n{authResult.Errors[0].Message}", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 2. Create account reference in Main API
                var (_, createQuery) = _createAccountQuery.Value;
                dynamic createVariables = new ExpandoObject();
                createVariables.createResponseInput = new
                {
                    id = SelectedAccount!.Id,
                    email = SelectedAccount!.Email,
                    firstName = SelectedAccount!.FirstName,
                    middleName = SelectedAccount!.MiddleName,
                    firstLastName = SelectedAccount!.FirstLastName,
                    middleLastName = SelectedAccount!.MiddleLastName,
                    profession = SelectedAccount!.Profession,
                    photoUrl = SelectedAccount!.PhotoUrl
                };
                await _accountService.CreateAsync<UpsertResponseType<AccountGraphQLModel>>(createQuery, createVariables);

                await TryCloseAsync(true);
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención !", $"Error al invitar colaborador.\r\n{ex.GetErrorMessage()}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task CancelAsync()
        {
            await TryCloseAsync(false);
        }

        #endregion

        #region GraphQL Queries

        private static readonly Lazy<string> _searchAccountQuery = new(() => @"
            query ($filters: AccountFilters, $pagination: Pagination) {
                accountsPage(filters: $filters, pagination: $pagination) {
                    entries {
                        id
                        email
                        firstName
                        middleName
                        firstLastName
                        middleLastName
                        fullName
                        profession
                        photoUrl
                    }
                    totalEntries
                }
            }");

        private static readonly Lazy<string> _createCollaboratorMutation = new(() => @"
            mutation ($input: CreateCollaboratorInput!) {
                createCollaborator(input: $input) {
                    collaborator {
                        account { id }
                    }
                    success
                    message
                }
            }");

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _createAccountQuery = new(() =>
        {
            var fields = FieldSpec<UpsertResponseType<AccountGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "account", nested: sq => sq
                    .Field(e => e.Id))
                .Field(f => f.Success)
                .Field(f => f.Message)
                .Build();

            var fragment = new GraphQLQueryFragment("createAccount",
                [new("input", "CreateAccountInput!")],
                fields, "CreateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        #endregion

        #region Response Types

        private class AccountSearchResponse
        {
            public AccountSearchPage AccountsPage { get; set; } = new();
        }

        private class AccountSearchPage
        {
            public List<AuthAccountModel> Entries { get; set; } = [];
            public int TotalEntries { get; set; }
        }

        private class AuthAccountModel
        {
            public int Id { get; set; }
            public string Email { get; set; } = string.Empty;
            public string FirstName { get; set; } = string.Empty;
            public string MiddleName { get; set; } = string.Empty;
            public string FirstLastName { get; set; } = string.Empty;
            public string MiddleLastName { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public string Profession { get; set; } = string.Empty;
            public string PhotoUrl { get; set; } = string.Empty;
        }

        private class CreateCollaboratorResponse
        {
            public CreateCollaboratorPayload CreateCollaborator { get; set; } = new();
        }

        private class CreateCollaboratorPayload
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
        }

        #endregion
    }
}
