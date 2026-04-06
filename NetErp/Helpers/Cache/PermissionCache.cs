using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using Models.Global;
using NetErp.Helpers.GraphQLQueryBuilder;
using QueryBuilder = NetErp.Helpers.GraphQLQueryBuilder.GraphQLQueryBuilder;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Helpers.Cache
{
    /// <summary>
    /// Cache that loads and resolves permission values for the current user.
    /// Precomputes the cascade: UserPermission → CompanyPermissionDefault → SystemDefault.
    /// Provides synchronous O(1) lookups by permission code.
    /// Cleared on logout/company switch via IEntityCache.
    /// </summary>
    public class PermissionCache : IEntityCache,
        IHandle<CompanyPermissionDefaultChangedMessage>,
        IHandle<UserPermissionChangedMessage>
    {
        private readonly IRepository<PermissionDefinitionGraphQLModel> _permDefService;
        private readonly IRepository<CompanyPermissionDefaultGraphQLModel> _companyPermDefService;
        private readonly IRepository<UserPermissionGraphQLModel> _userPermService;
        private readonly IEventAggregator _eventAggregator;
        private readonly Lock _lock = new();
        private readonly Dictionary<string, ResolvedPermission> _permissions = [];
        private bool _isLoaded;

        public bool IsInitialized => _isLoaded;

        public PermissionCache(
            IRepository<PermissionDefinitionGraphQLModel> permDefService,
            IRepository<CompanyPermissionDefaultGraphQLModel> companyPermDefService,
            IRepository<UserPermissionGraphQLModel> userPermService,
            IEventAggregator eventAggregator)
        {
            _permDefService = permDefService;
            _companyPermDefService = companyPermDefService;
            _userPermService = userPermService;
            _eventAggregator = eventAggregator;
            _eventAggregator.SubscribeOnUIThread(this);
        }

        public async Task EnsureLoadedAsync()
        {
            lock (_lock) { if (_isLoaded) return; }
            await LoadAndResolveAsync();
        }

        private async Task LoadAndResolveAsync()
        {
            // Single HTTP request with 3 fragments
            var (_, query) = _combinedQuery.Value;

            dynamic filters = new ExpandoObject();
            filters.accountId = SessionInfo.LoginAccountId;

            ExpandoObject variables = new GraphQLVariables()
                .For(_combinedPermDefsFragment.Value, "pagination", new { pageSize = -1 })
                .For(_combinedCompanyDefsFragment.Value, "pagination", new { pageSize = -1 })
                .For(_combinedUserPermsFragment.Value, "filters", filters)
                .For(_combinedUserPermsFragment.Value, "pagination", new { pageSize = -1 })
                .Build();

            PermissionDataContext result = await _permDefService.GetDataContextAsync<PermissionDataContext>(query, variables);

            PageType<PermissionDefinitionGraphQLModel> permDefs = result.PermDefs;
            PageType<CompanyPermissionDefaultGraphQLModel> companyDefaults = result.CompanyDefs;
            PageType<UserPermissionGraphQLModel> userPerms = result.UserPerms;

            // Index company defaults and user permissions by permissionDefinition.Id
            Dictionary<int, string> companyDefaultsByPermId = companyDefaults.Entries
                .Where(cd => cd.PermissionDefinition != null)
                .ToDictionary(cd => cd.PermissionDefinition!.Id, cd => cd.DefaultValue);

            Dictionary<int, UserPermissionGraphQLModel> userPermsByPermId = userPerms.Entries
                .Where(up => up.PermissionDefinition != null)
                .ToDictionary(up => up.PermissionDefinition!.Id);

            // Resolve cascade for each permission definition
            lock (_lock)
            {
                _permissions.Clear();

                foreach (PermissionDefinitionGraphQLModel permDef in permDefs.Entries)
                {
                    string effectiveValue = permDef.SystemDefault;

                    // Level 2: Company default overrides system default
                    if (companyDefaultsByPermId.TryGetValue(permDef.Id, out string? companyValue))
                        effectiveValue = companyValue;

                    // Level 1: User permission overrides everything (if not expired)
                    if (userPermsByPermId.TryGetValue(permDef.Id, out UserPermissionGraphQLModel? userPerm))
                    {
                        bool isExpired = false;
                        DateTime? expiresAt = null;

                        if (!string.IsNullOrEmpty(userPerm.ExpiresAt) &&
                            DateTime.TryParse(userPerm.ExpiresAt, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsed))
                        {
                            expiresAt = parsed;
                            isExpired = parsed < DateTime.UtcNow;
                        }

                        if (!isExpired)
                            effectiveValue = userPerm.Value;
                    }

                    _permissions[permDef.Code] = new ResolvedPermission(
                        permDef.Code,
                        permDef.PermissionType,
                        effectiveValue);
                }

                _isLoaded = true;
            }
        }

        /// <summary>
        /// Returns true if the ACTION permission is allowed.
        /// SystemAdmin always returns true. Unknown codes return true (permissive by default).
        /// </summary>
        public bool IsAllowed(string code)
        {
            if (SessionInfo.IsSystemAdmin) return true;

            lock (_lock)
            {
                if (_permissions.TryGetValue(code, out ResolvedPermission? perm))
                    return perm.EffectiveValue == "ALLOWED";
            }

            return true;
        }

        /// <summary>
        /// Returns true if the ACTION permission is denied.
        /// </summary>
        public bool IsDenied(string code) => !IsAllowed(code);

        /// <summary>
        /// Returns true if the FIELD permission is required.
        /// SystemAdmin always returns false. Unknown codes return false (optional by default).
        /// </summary>
        public bool IsRequired(string code)
        {
            if (SessionInfo.IsSystemAdmin) return false;

            lock (_lock)
            {
                if (_permissions.TryGetValue(code, out ResolvedPermission? perm))
                    return perm.EffectiveValue == "REQUIRED";
            }

            return false;
        }

        /// <summary>
        /// Returns true if the FIELD permission is optional.
        /// </summary>
        public bool IsOptional(string code) => !IsRequired(code);

        /// <summary>
        /// Returns true if a permission with the given code exists in the cache.
        /// </summary>
        public bool HasPermission(string code)
        {
            lock (_lock)
            {
                return _permissions.ContainsKey(code);
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _permissions.Clear();
                _isLoaded = false;
            }
        }

        /// <summary>
        /// Forces a full reload of permissions and notifies all listeners.
        /// </summary>
        public async Task ReloadAsync()
        {
            lock (_lock)
            {
                _permissions.Clear();
                _isLoaded = false;
            }
            await LoadAndResolveAsync();
            await _eventAggregator.PublishOnCurrentThreadAsync(new PermissionsCacheRefreshedMessage());
        }

        public async Task HandleAsync(CompanyPermissionDefaultChangedMessage message, CancellationToken cancellationToken)
        {
            await ReloadAsync();
        }

        public async Task HandleAsync(UserPermissionChangedMessage message, CancellationToken cancellationToken)
        {
            await ReloadAsync();
        }

        #region GraphQL Queries

        private static readonly Lazy<GraphQLQueryFragment> _combinedPermDefsFragment = new(() =>
        {
            Dictionary<string, object> fields = FieldSpec<PageType<PermissionDefinitionGraphQLModel>>
                .Create()
                .SelectList(it => it.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Code)
                    .Field(e => e.PermissionType)
                    .Field(e => e.SystemDefault))
                .Build();

            return new GraphQLQueryFragment("permissionDefinitionsPage", [new("pagination", "Pagination")], fields, "PermDefs");
        });

        private static readonly Lazy<GraphQLQueryFragment> _combinedCompanyDefsFragment = new(() =>
        {
            Dictionary<string, object> fields = FieldSpec<PageType<CompanyPermissionDefaultGraphQLModel>>
                .Create()
                .SelectList(it => it.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.DefaultValue)
                    .Select(e => e.PermissionDefinition, pd => pd
                        .Field(p => p!.Id)))
                .Build();

            return new GraphQLQueryFragment("companyPermissionDefaultsPage", [new("pagination", "Pagination")], fields, "CompanyDefs");
        });

        private static readonly Lazy<GraphQLQueryFragment> _combinedUserPermsFragment = new(() =>
        {
            Dictionary<string, object> fields = FieldSpec<PageType<UserPermissionGraphQLModel>>
                .Create()
                .SelectList(it => it.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Value)
                    .Field(e => e.ExpiresAt)
                    .Select(e => e.PermissionDefinition, pd => pd
                        .Field(p => p!.Id)))
                .Build();

            return new GraphQLQueryFragment("userPermissionsPage",
                [new("filters", "UserPermissionFilters"), new("pagination", "Pagination")],
                fields, "UserPerms");
        });

        private static readonly Lazy<(List<GraphQLQueryFragment> Fragments, string Query)> _combinedQuery = new(() =>
        {
            List<GraphQLQueryFragment> fragments =
            [
                _combinedPermDefsFragment.Value,
                _combinedCompanyDefsFragment.Value,
                _combinedUserPermsFragment.Value
            ];
            string query = new QueryBuilder(fragments).GetQuery();
            return (fragments, query);
        });

        #endregion

        #region Internal Types

        private record ResolvedPermission(string Code, string PermissionType, string EffectiveValue);

        private class PermissionDataContext
        {
            public PageType<PermissionDefinitionGraphQLModel> PermDefs { get; set; } = new();
            public PageType<CompanyPermissionDefaultGraphQLModel> CompanyDefs { get; set; } = new();
            public PageType<UserPermissionGraphQLModel> UserPerms { get; set; } = new();
        }

        #endregion
    }
}
