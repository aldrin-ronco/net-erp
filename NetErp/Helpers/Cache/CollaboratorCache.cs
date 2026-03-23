using Common.Helpers;
using Common.Interfaces;
using GraphQL;
using Models.Login;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace NetErp.Helpers.Cache
{
    /// <summary>
    /// Cache for collaborators (users) of the current company.
    /// Queries the authentication API (not the tenant API) using IAuthApiClient.
    /// Loads collaborators filtered by LoginCompanyId.
    /// </summary>
    public class CollaboratorCache : IEntityCache
    {
        private readonly IAuthApiClient _authApiClient;
        private readonly Lock _lock = new();
        private readonly ObservableCollection<CollaboratorGraphQLModel> _items = [];
        private bool _isLoaded;

        public ReadOnlyObservableCollection<CollaboratorGraphQLModel> Items { get; }
        public bool IsInitialized => _isLoaded;

        public CollaboratorCache(IAuthApiClient authApiClient)
        {
            _authApiClient = authApiClient ?? throw new ArgumentNullException(nameof(authApiClient));
            Items = new ReadOnlyObservableCollection<CollaboratorGraphQLModel>(_items);
        }

        public async Task EnsureLoadedAsync()
        {
            lock (_lock) { if (_isLoaded) return; }

            int companyId = SessionInfo.LoginCompanyId;
            if (companyId == 0) throw new InvalidOperationException("LoginCompanyId no está establecido. Seleccione una empresa antes de cargar colaboradores.");

            string query = _loadQuery.Value;

            GraphQLResponse<CollaboratorsResponseType> result = await _authApiClient.SendQueryAsync<CollaboratorsResponseType>(new GraphQLRequest
            {
                Query = query,
                Variables = new { filters = new { companyId }, pagination = new { pageSize = -1 } }
            });

            if (result.Errors != null)
            {
                GraphQL.GraphQLError error = result.Errors[0];
                throw new Exception($"Error al cargar colaboradores: {error.Message}");
            }

            lock (_lock)
            {
                _items.Clear();
                foreach (CollaboratorGraphQLModel collaborator in result.Data.CollaboratorsPage.Entries)
                {
                    _items.Add(collaborator);
                }
                _isLoaded = true;
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _items.Clear();
                _isLoaded = false;
            }
        }

        private static readonly Lazy<string> _loadQuery = new(() =>
            @"query ($filters: CollaboratorFilters, $pagination: Pagination) {
                collaboratorsPage(filters: $filters, pagination: $pagination) {
                    entries {
                        account {
                            id
                            email
                            firstName
                            middleName
                            firstLastName
                            middleLastName
                            fullName
                        }
                    }
                    totalEntries
                }
            }");

        private class CollaboratorsResponseType
        {
            public CollaboratorPageGraphQLModel CollaboratorsPage { get; set; } = new();
        }
    }
}
